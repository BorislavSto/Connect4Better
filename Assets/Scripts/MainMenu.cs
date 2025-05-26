using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
    
    // starts server + local client, auto scene load
    public void OnMultiplayerClicked()
    {
        GameManager.Instance.StartMultiplayer();
    }
    
    public void OnLocalClicked()
    {
        // Stop any networking if running (just in case)
        if (NetworkClient.isConnected || NetworkServer.active)
        {
            GameManager.Instance.StopHost();
        }

        // Initialize local game mode — your GameManager should handle this
        GameManager.Instance.StartLocalGame();
    }
    
    public void OnVsAIClicked()
    {
        if (NetworkClient.isConnected || NetworkServer.active)
        {
            GameManager.Instance.StopHost();
        }

        GameManager.Instance.StartVsAIGame();
    }
}