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
        // 1. Configurar NetworkManager como HOST
        SetupNetworkManager(true);

        // 2. Cargar escena ESPECÍFICA del Host
        SceneManager.LoadScene("ServerWaitingRoom");
    }

    void OnJoinClient()
    {
        // 1. Configurar NetworkManager como CLIENTE
        SetupNetworkManager(false);
        
        // 2. Cargar escena de Clientes
        SceneManager.LoadScene("WaitingRoom");
    }

    void SetupNetworkManager(bool serverMode)
    {
        if (NetworkManager.Instance == null)
        {
            GameObject nmObj = new GameObject("NetworkManager");
            nmObj.AddComponent<NetworkManager>();
        }
        
        NetworkManager.Instance.isServer = serverMode;
        NetworkManager.Instance.myPlayerId = serverMode ? 1 : -1;
        NetworkManager.Instance.playerName = serverMode ? "Host" : "Player";
        NetworkManager.Instance.isGameStarted = false;
        
        // Limpiar socket viejo si hubiera
        if (NetworkManager.Instance.udpSocket != null)
        {
            NetworkManager.Instance.udpSocket.Close();
            NetworkManager.Instance.udpSocket = null;
        }
    }
}