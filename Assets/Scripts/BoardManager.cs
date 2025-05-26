using System.Collections;
using System.Collections.Generic;
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

public class BoardManager : MonoBehaviour
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

        column.cells[rowIndex].State = DataManager.Instance.CurrentTurn ;

        GameObject prefab = DataManager.Instance.CurrentTurn  == CellState.Player1 ? redPiecePrefab : yellowPiecePrefab;
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

        if (CheckForWin(columnIndex, rowIndex, DataManager.Instance.CurrentTurn ))
        {
            isGameOver = true;
            Debug.Log($"{DataManager.Instance.CurrentTurn } wins!");
            // TODO: trigger win UI here
        }
        else
        {
            GameManager.Instance.SwitchTurn();
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
        return CheckDirection(lastCol, lastRow, 1, 0, player)    // Horizontal
               || CheckDirection(lastCol, lastRow, 0, 1, player)    // Vertical
               || CheckDirection(lastCol, lastRow, 1, 1, player)    // Diagonal up-right / down-left
               || CheckDirection(lastCol, lastRow, 1, -1, player);  // Diagonal down-right / up-left
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
        DataManager.Instance.CurrentTurn = CellState.Player1;
    }
}
