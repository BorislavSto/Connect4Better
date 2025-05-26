using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public enum CellState { Empty, Player1, Player2 }
public class Cell
{
    public CellState State = CellState.Empty;
}

[System.Serializable]
public class Column
{
    public Transform columnTransform;
    public List<GameObject> pieces = new List<GameObject>();
    public List<Cell> cells = new List<Cell>();

    public void InitializeCells(int maxRows)
    {
        cells.Clear();
        for (int i = 0; i < maxRows; i++)
            cells.Add(new Cell());
    }
}

public class BoardManager : NetworkBehaviour
{
    public Column[] columns = new Column[7];
    public int maxRows = 6;
    public float pieceSpacing = 1.5f;

    public float dropYOffset = 4.1f;

    public GameObject redPiecePrefab;
    public GameObject yellowPiecePrefab;

    public bool isDropping { get; private set; }
    private bool isGameOver;

    void Start()
    {
        foreach (var col in columns)
        {
            col.InitializeCells(maxRows);
        }
    }

    public void TryDropPiece(int columnIndex)
    {
        switch (GameManager.Instance.CurrentGameMode)
        {
            case GameMode.VsAI:
                if (!GameManager.Instance.IsMyTurn(DataManager.Instance.CurrentPlayer))
                {
                    Debug.Log("Wait for AI turn!");
                    return;
                }

                DropPiece(columnIndex);
                StartCoroutine(AIPlayTurnAfterDelay());
                break;
            case GameMode.Local:
            default:
                DropPiece(columnIndex);
                break;
            case GameMode.Multiplayer:
                if (GameManager.Instance.IsMyTurn(DataManager.Instance.CurrentPlayer))
                {
                    // In multiplayer, call TryDropPiece so the command goes to server
                    CmdRequestDropPiece(columnIndex);
                }
                else
                {
                    Debug.Log("Not your turn!");
                }

                break;
        }
    }

    [Command]
    void CmdRequestDropPiece(int columnIndex)
    {
        // Validate and drop piece on the server
        DropPiece(columnIndex);

        // Then update clients with RPCs or via SyncVars
    }

    public void DropPiece(int columnIndex)
    {
        if (isDropping || isGameOver) return;
        if (columnIndex < 0 || columnIndex >= columns.Length) return;

        Column column = columns[columnIndex];
        if (column.pieces.Count >= maxRows) return;

        int rowIndex = column.pieces.Count;

        if (column.cells[rowIndex].State != CellState.Empty)
        {
            Debug.LogWarning("Trying to drop into non-empty cell, should not happen.");
            return;
        }

        column.cells[rowIndex].State = DataManager.Instance.CurrentPlayer;

        GameObject prefab = DataManager.Instance.CurrentPlayer == CellState.Player1
            ? redPiecePrefab
            : yellowPiecePrefab;
        GameObject piece = Instantiate(prefab);

        Vector3 dropTarget = column.columnTransform.position + Vector3.up * (rowIndex * pieceSpacing);
        column.pieces.Add(piece);

        Vector3 adjustedTarget = dropTarget + new Vector3(0, dropYOffset, 0);

        StartCoroutine(AnimateDrop(piece, adjustedTarget, columnIndex));
    }

    private IEnumerator AnimateDrop(GameObject piece, Vector3 target, int columnIndex)
    {
        isDropping = true;

        Vector3 start = target + Vector3.up * 10f;
        piece.transform.position = start;

        float t = 0f;
        float duration = 0.5f;
        while (t < duration)
        {
            piece.transform.position = Vector3.Lerp(start, target, t / duration);
            t += Time.deltaTime;
            yield return null;
        }

        piece.transform.position = target;

        int rowIndex = columns[columnIndex].pieces.Count - 1;

        if (CheckForWin(columnIndex, rowIndex, DataManager.Instance.CurrentPlayer))
        {
            isGameOver = true;
            Debug.Log($"{DataManager.Instance.CurrentPlayer} wins!");
            // TODO: trigger win UI here
        }
        else
        {
            if (NetworkServer.active || NetworkClient.active)
            {
                GameManager.Instance.SwitchTurn();
            }
            else
            {
                GameManager.Instance.SwitchTurnSingleplayer();
            }
        }

        isDropping = false;
    }

    private CellState GetCellState(int col, int row)
    {
        if (col < 0 || col >= columns.Length)
            return CellState.Empty;

        if (row < 0 || row >= maxRows)
            return CellState.Empty;

        return columns[col].cells[row].State;
    }

    private bool CheckForWin(int lastCol, int lastRow, CellState player)
    {
        return CheckDirection(lastCol, lastRow, 1, 0, player) // Horizontal
               || CheckDirection(lastCol, lastRow, 0, 1, player) // Vertical
               || CheckDirection(lastCol, lastRow, 1, 1, player) // Diagonal up-right / down-left
               || CheckDirection(lastCol, lastRow, 1, -1, player); // Diagonal down-right / up-left
    }

    private bool CheckDirection(int col, int row, int deltaCol, int deltaRow, CellState player)
    {
        int count = 1;

        count += CountPieces(col, row, deltaCol, deltaRow, player);
        count += CountPieces(col, row, -deltaCol, -deltaRow, player);

        return count >= 4;
    }

    private int CountPieces(int col, int row, int deltaCol, int deltaRow, CellState player)
    {
        int count = 0;
        int currentCol = col + deltaCol;
        int currentRow = row + deltaRow;

        while (true)
        {
            if (GetCellState(currentCol, currentRow) != player)
                break;

            count++;
            currentCol += deltaCol;
            currentRow += deltaRow;
        }

        return count;
    }

    private IEnumerator AIPlayTurnAfterDelay()
    {
        yield return new WaitForSeconds(1.0f); // delay for AI "thinking"

        if (GameManager.Instance.CurrentGameMode != GameMode.VsAI) yield break;

        if (DataManager.Instance.CurrentPlayer == CellState.Player2)
        {
            int aiMove = GetAIMove();
            DropPiece(aiMove);
        }
    }

    private int GetAIMove()
    {
        int bestScore = int.MinValue;
        int bestColumn = -1;
        int[] columnOrder = GetShuffledCenterBiasedColumnOrder();

        foreach (int col in columnOrder)
        {
            if (columns[col].pieces.Count >= maxRows)
                continue;

            int row = columns[col].pieces.Count;
            columns[col].cells[row].State = CellState.Player2;

            int score = Minimax(3, false, int.MinValue, int.MaxValue);

            columns[col].cells[row].State = CellState.Empty;

            if (score > bestScore)
            {
                bestScore = score;
                bestColumn = col;
            }
        }

        return bestColumn != -1 ? bestColumn : 0;
    }

    private int Minimax(int depth, bool isMaximizing, int alpha, int beta)
    {
        if (CheckForWinState(CellState.Player2)) return 1000;
        if (CheckForWinState(CellState.Player1)) return -1000;
        if (depth == 0 || IsBoardFull()) return EvaluateBoard();

        int[] columnOrder = GetShuffledCenterBiasedColumnOrder();

        if (isMaximizing)
        {
            int maxEval = int.MinValue;
            foreach (int col in columnOrder)
            {
                if (columns[col].pieces.Count >= maxRows) continue;

                int row = columns[col].pieces.Count;
                columns[col].cells[row].State = CellState.Player2;

                int eval = Minimax(depth - 1, false, alpha, beta);
                columns[col].cells[row].State = CellState.Empty;

                maxEval = Mathf.Max(maxEval, eval);
                alpha = Mathf.Max(alpha, eval);
                if (beta <= alpha) break;
            }

            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (int col in columnOrder)
            {
                if (columns[col].pieces.Count >= maxRows) continue;

                int row = columns[col].pieces.Count;
                columns[col].cells[row].State = CellState.Player1;

                int eval = Minimax(depth - 1, true, alpha, beta);
                columns[col].cells[row].State = CellState.Empty;

                minEval = Mathf.Min(minEval, eval);
                beta = Mathf.Min(beta, eval);
                if (beta <= alpha) break;
            }

            return minEval;
        }
    }

    private bool CheckForWinState(CellState player)
    {
        for (int col = 0; col < columns.Length; col++)
        {
            for (int row = 0; row < maxRows; row++)
            {
                if (GetCellState(col, row) == player)
                {
                    if (CheckDirection(col, row, 1, 0, player) ||
                        CheckDirection(col, row, 0, 1, player) ||
                        CheckDirection(col, row, 1, 1, player) ||
                        CheckDirection(col, row, 1, -1, player))
                        return true;
                }
            }
        }

        return false;
    }

    private bool IsBoardFull()
    {
        foreach (var column in columns)
        {
            if (column.pieces.Count < maxRows)
                return false;
        }

        return true;
    }

    private int EvaluateBoard()
    {
        int score = 0;

        for (int col = 0; col < columns.Length; col++)
        {
            for (int row = 0; row < maxRows; row++)
            {
                CellState state = GetCellState(col, row);
                if (state == CellState.Empty) continue;

                score += EvaluateDirection(col, row, 1, 0);  // Horizontal
                score += EvaluateDirection(col, row, 0, 1);  // Vertical
                score += EvaluateDirection(col, row, 1, 1);  // Diagonal /
                score += EvaluateDirection(col, row, 1, -1); // Diagonal \
            }
        }

        return score;
    }

    private int EvaluateDirection(int col, int row, int dCol, int dRow)
    {
        int maxInLine = 4;
        int aiCount = 0;
        int playerCount = 0;

        for (int i = 0; i < maxInLine; i++)
        {
            int c = col + dCol * i;
            int r = row + dRow * i;

            if (c < 0 || c >= columns.Length || r < 0 || r >= maxRows)
                return 0;

            CellState state = GetCellState(c, r);
            if (state == CellState.Player2)
                aiCount++;
            else if (state == CellState.Player1)
                playerCount++;
        }

        if (aiCount > 0 && playerCount > 0)
            return 0;

        if (aiCount == 4) return 100;
        if (aiCount == 3) return 10;
        if (aiCount == 2) return 5;

        if (playerCount == 4) return -100;
        if (playerCount == 3) return -80; 
        if (playerCount == 2) return -10;

        return 0;
    }
    
    private int[] GetShuffledCenterBiasedColumnOrder()
    {
        int center = columns.Length / 2;
        List<int> ordered = new List<int>();

        for (int offset = 0; offset <= center; offset++)
        {
            if (center - offset >= 0)
                ordered.Add(center - offset);
            if (center + offset < columns.Length && offset != 0)
                ordered.Add(center + offset);
        }

        System.Random rng = new System.Random();
        for (int i = 1; i < ordered.Count; i++)
        {
            int j = rng.Next(i, ordered.Count);
            (ordered[i], ordered[j]) = (ordered[j], ordered[i]);
        }

        return ordered.ToArray();
    }
    
    public void ResetBoard()
    {
        foreach (Column col in columns)
        {
            foreach (var piece in col.pieces)
                Destroy(piece);

            col.pieces.Clear();
        }

        isGameOver = false;
        isDropping = false;
        DataManager.Instance.CurrentPlayer = CellState.Player1;
    }
}

public class ConnectFourAI
{
    
}
