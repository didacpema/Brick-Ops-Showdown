using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using BrickOps.Networking;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button createServerButton;
    [SerializeField] private Button joinClientButton;

    void Start()
    {
        createServerButton.onClick.AddListener(OnCreateServer);
        joinClientButton.onClick.AddListener(OnJoinClient);
    }

    void OnCreateServer()
    {
        if (NetworkManager.Instance == null)
        {
            GameObject nmObj = new GameObject("NetworkManager");
            NetworkManager nm = nmObj.AddComponent<NetworkManager>();
            nm.isServer = true;
            nm.port = 6000;
            nm.myPlayerId = 1; // El servidor es siempre Player 1
            nm.playerName = "Host"; // Nombre por defecto del servidor
        }
        else
        {
            NetworkManager.Instance.isServer = true;
            NetworkManager.Instance.myPlayerId = 1;
            NetworkManager.Instance.playerName = "Host";
        }
        
        // Ir directamente al juego, no a ServerScene separado
        SceneManager.LoadScene("Game");
    }

    void OnJoinClient()
    {
        if (NetworkManager.Instance == null)
        {
            GameObject nmObj = new GameObject("NetworkManager");
            nmObj.AddComponent<NetworkManager>();
        }
        
        SceneManager.LoadScene("WaitingRoom");
    }
}