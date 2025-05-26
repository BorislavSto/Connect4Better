using Mirror;
using UnityEngine.SceneManagement;

public enum GameMode
{
    Local,
    VsAI,
    Multiplayer,
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

    public void StartMultiplayer()
    {
        CurrentGameMode = GameMode.Multiplayer;
        StartHost();
    }

    public void StartLocalGame()
    {
        CurrentGameMode = GameMode.Local;
        DataManager.Instance.CurrentPlayer = CellState.Player1;
        SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
    
    public void StartVsAIGame()
    {
        CurrentGameMode = GameMode.VsAI;
        DataManager.Instance.CurrentPlayer = CellState.Player1;
        SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        // aiPlayerEnabled = true;
        // initialize AI if needed
    }
    
    public bool IsMyTurn(CellState mySide)
    {
        if (CurrentGameMode == GameMode.Local)
            return true;

        if (CurrentGameMode == GameMode.VsAI)
            return (mySide == CellState.Player1);

        if (CurrentGameMode == GameMode.Multiplayer)
        {
            var localPlayer = NetworkClient.connection.identity.GetComponent<PlayerController>();
            return (mySide == DataManager.Instance.CurrentPlayer  && localPlayer.myPlayerSide == DataManager.Instance.CurrentPlayer );
        }

        return false;
    }

    [Server]
    public void SwitchTurn()
    {
        DataManager.Instance.CurrentPlayer = (DataManager.Instance.CurrentPlayer  == CellState.Player1) ? CellState.Player2 : CellState.Player1;
    }    
    
    public void SwitchTurnSingleplayer()
    {
        DataManager.Instance.CurrentPlayer = (DataManager.Instance.CurrentPlayer  == CellState.Player1) ? CellState.Player2 : CellState.Player1;
    }
}