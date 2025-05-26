using Mirror;

public class DataManager : NetworkBehaviour
{
    public static DataManager Instance;
    
    [SyncVar] public CellState CurrentPlayer = CellState.Player1;

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

}