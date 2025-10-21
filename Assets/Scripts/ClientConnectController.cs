using System.Net;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using BrickOps.Networking;

public class ClientConnectController : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField nameInput;
    public TMP_InputField ipInput;
    public TMP_InputField portInput;
    public Button connectButton;
    public Button backButton;
    public TMP_Text errorText;

    void Start()
    {
        connectButton.onClick.AddListener(OnConnect);
        backButton.onClick.AddListener(BackToMenu);
        errorText.text = "";
    }

    void OnConnect()
    {
        string playerName = nameInput.text.Trim();
        string serverIP = ipInput.text.Trim();
        string portStr = portInput.text.Trim();

        // Validaciones
        if (string.IsNullOrEmpty(playerName))
        {
            errorText.text = "Please enter a name!";
            return;
        }

        if (string.IsNullOrEmpty(serverIP))
            serverIP = "127.0.0.1";

        if (string.IsNullOrEmpty(portStr))
            portStr = "6000";

        if (!int.TryParse(portStr, out int port))
        {
            errorText.text = "Invalid port number!";
            return;
        }

        if (!IPAddress.TryParse(serverIP, out IPAddress ip))
        {
            errorText.text = $"Invalid IP: {serverIP}";
            return;
        }

        // Configurar NetworkManager
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.playerName = playerName;
            NetworkManager.Instance.serverIP = serverIP;
            NetworkManager.Instance.port = port;
            NetworkManager.Instance.isServer = false;
            
            // Ir a la sala de espera
            SceneManager.LoadScene("WaitingRoom");
        }
        else
        {
            errorText.text = "NetworkManager not found!";
        }
    }

    void BackToMenu()
    {
        if (NetworkManager.Instance != null)
        {
            Destroy(NetworkManager.Instance.gameObject);
        }
        SceneManager.LoadScene("MainMenu");
    }
}