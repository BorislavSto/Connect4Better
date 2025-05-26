using Mirror;
using UnityEngine;

public enum GameMode
{
    Local,
    VsAI,
    Online,
}

public enum PlayerType
{
    Human,
    AI,
    Remote,
}

public class GameManager : NetworkManager
{
    public static GameManager Instance;
    
    public GameMode CurrentGameMode = GameMode.Local;

    public PlayerType Player1Type = PlayerType.Human;
    public PlayerType Player2Type = PlayerType.Human;

    private int playerCount = 0;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        PlayerController pc = conn.identity.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.myPlayerSide = playerCount == 0 ? CellState.Player1 : CellState.Player2;
            playerCount++;
        }
    }
    
    public override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public bool IsMyTurn(CellState mySide)
    {
        if (CurrentGameMode == GameMode.Local)
            return true;

        if (CurrentGameMode == GameMode.VsAI)
            return (mySide == CellState.Player1);

        if (CurrentGameMode == GameMode.Online)
        {
            var localPlayer = NetworkClient.connection.identity.GetComponent<PlayerController>();
            return (mySide == DataManager.Instance.CurrentTurn  && localPlayer.myPlayerSide == DataManager.Instance.CurrentTurn );
        }

        return false;
    }

    [Server]
    public void SwitchTurn()
    {
        DataManager.Instance.CurrentTurn = (DataManager.Instance.CurrentTurn  == CellState.Player1) ? CellState.Player2 : CellState.Player1;
    }
}