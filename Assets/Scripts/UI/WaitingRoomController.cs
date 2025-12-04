using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using BrickOps.Networking;

public class WaitingRoomController : MonoBehaviour
{
    [Header("Panels (Asignar en Inspector)")]
    public GameObject connectionPanel; // Solo visible para Cliente
    public GameObject chatPanel;       // Visible para ambos al conectar
    public GameObject hostPanel;       // Solo visible para Host (contiene botón Start)

    [Header("UI Elements")]
    public TMP_InputField nameInput;
    public TMP_InputField ipInput;
    public TMP_InputField chatInput;
    public TMP_Text chatText;
    public TMP_Text playerCountText; // Opcional: Para mostrar "Players: X"
    
    [Header("Buttons")]
    public Button connectButton;     // Cliente
    public Button sendButton;        // Ambos
    public Button startGameButton;   // Host
    public Button backButton;        // Ambos

    // Estado
    private UdpTransport transport;
    private bool isHost;
    private List<IPEndPoint> connectedClients = new List<IPEndPoint>();

    void Start()
    {
        // Detectar modo según lo configurado en MainMenu
        isHost = NetworkManager.Instance.isServer;

        SetupUI();
        SetupListeners();

        if (isHost)
        {
            StartHostLogic();
        }
        else
        {
            // El cliente espera a que el usuario pulse "Conectar"
            Debug.Log("[WaitingRoom] Client Mode: Waiting for user input...");
        }
    }

    void SetupUI()
    {
        // Limpiar chat
        if (chatText != null) chatText.text = "";

        if (isHost)
        {
            // Configuración visual HOST
            if (connectionPanel) connectionPanel.SetActive(false);
            if (chatPanel) chatPanel.SetActive(true);
            if (hostPanel) hostPanel.SetActive(true);
            if (startGameButton) startGameButton.interactable = true;
            if (nameInput) nameInput.interactable = true;
            UpdatePlayerCount();
        }
        else
        {
            // Configuración visual CLIENTE
            if (connectionPanel) connectionPanel.SetActive(true);
            if (chatPanel) chatPanel.SetActive(false);
            if (hostPanel) hostPanel.SetActive(false);
        }
    }

    void SetupListeners()
    {
        if (connectButton) connectButton.onClick.AddListener(ClientConnect);
        if (sendButton) sendButton.onClick.AddListener(SendChatMessage);
        if (startGameButton) startGameButton.onClick.AddListener(HostStartGame);
        if (backButton) backButton.onClick.AddListener(GoBack);
        if (chatInput) chatInput.onSubmit.AddListener((s) => SendChatMessage());
        if (nameInput)
        {
            // 1. Poner el nombre actual por defecto
            nameInput.text = NetworkManager.Instance.playerName;

            // 2. Detectar cuando escribes para actualizar el NetworkManager al vuelo
            nameInput.onValueChanged.AddListener((newName) => 
            {
                NetworkManager.Instance.playerName = newName;
            });
        }
    }

    // ================== HOST LOGIC ==================
    void StartHostLogic()
    {
        transport = new UdpTransport();
        
        // Intentar abrir puerto 6000
        if (transport.InitializeServer(6000))
        {
            // Guardar socket en NetworkManager para que GameController lo use luego
            NetworkManager.Instance.udpSocket = transport.Socket;
            NetworkManager.Instance.serverEndPoint = null; // Soy servidor
            
            AddChatMsg("<color=green>Server started on Port 6000.</color>");
            AddChatMsg("Waiting for players...");
        }
        else
        {
            AddChatMsg("<color=red>Error: Could not bind port 6000.</color>");
        }
    }

    void HostStartGame()
    {
        if (!isHost) return;

        AddChatMsg("<color=yellow>Starting Game...</color>");
        NetworkManager.Instance.isGameStarted = true;

        // Avisar a todos los clientes
        HostBroadcast("GAME_START");

        // Cargar juego (usará el socket guardado en NetworkManager)
        SceneManager.LoadScene("Game");
    }

    // ================== CLIENT LOGIC ==================
    void ClientConnect()
    {
        string ipStr = ipInput.text.Trim();
        string name = nameInput.text.Trim();

        if (string.IsNullOrEmpty(ipStr)) ipStr = "127.0.0.1";
        if (string.IsNullOrEmpty(name)) name = "Client";

        // Guardar nombre
        NetworkManager.Instance.playerName = name;
        NetworkManager.Instance.serverIP = ipStr;

        // Iniciar Socket
        transport = new UdpTransport();
        if (!IPAddress.TryParse(ipStr, out IPAddress ip))
        {
            Debug.LogError("Invalid IP");
            return;
        }

        if (transport.InitializeClient(ip, 6000))
        {
            NetworkManager.Instance.udpSocket = transport.Socket;
            NetworkManager.Instance.serverEndPoint = transport.RemoteEndPoint;

            // UI Feedback
            connectionPanel.SetActive(false);
            chatPanel.SetActive(true);
            
            // Enviar saludo al servidor para registrarnos
            transport.Send($"JOIN:{name}", transport.RemoteEndPoint);
            
            AddChatMsg($"<color=green>Connected to {ipStr} as {name}</color>");
        }
    }

    // ================== COMMON LOGIC ==================
    void Update()
    {
        if (transport != null && transport.Socket != null)
        {
            // Recibir mensajes (Non-blocking loop)
            while (transport.TryReceive(out string msg, out EndPoint sender))
            {
                if (string.IsNullOrEmpty(msg)) continue;

                if (isHost)
                    HandleMessageAsHost(msg, (IPEndPoint)sender);
                else
                    HandleMessageAsClient(msg);
            }
        }
    }

    void SendChatMessage()
    {
        string txt = chatInput.text.Trim();
        if (string.IsNullOrEmpty(txt)) return;

        string name = NetworkManager.Instance.playerName;
        string fullMsg = $"CHAT:[{name}]: {txt}";

        if (isHost)
        {
            // Host: Muestra local y retransmite a todos
            AddChatMsg($"[{name}]: {txt}");
            HostBroadcast(fullMsg);
        }
        else
        {
            // Cliente: Manda al host (el host lo rebotará para que lo veamos)
            // Opcional: Mostrar localmente para feedback instantáneo
            if (transport != null)
                transport.Send(fullMsg, transport.RemoteEndPoint);
        }

        chatInput.text = "";
        chatInput.ActivateInputField();
    }

    // --- Message Handlers ---

    void HandleMessageAsHost(string msg, IPEndPoint sender)
    {
        // Registrar cliente si es nuevo
        if (!connectedClients.Contains(sender))
        {
            connectedClients.Add(sender);
            
            // Asignar ID (Host=1, Clientes=2+)
            int newId = connectedClients.Count + 1;
            
            // Enviar ID de vuelta
            transport.Send($"PLAYER_ID:{newId}", sender);
            
            string joinMsg = $"Player {newId} joined!";
            HostBroadcast($"CHAT:<color=yellow>{joinMsg}</color>");
            AddChatMsg($"<color=yellow>{joinMsg} ({sender})</color>");
            UpdatePlayerCount();
        }

        // Procesar contenido
        if (msg.StartsWith("CHAT:"))
        {
            string content = msg.Substring(5); // Quitar "CHAT:"
            AddChatMsg(content); // Mostrar en Host
            HostBroadcast(msg);  // Reenviar a TODOS los clientes
        }
        else if (msg.StartsWith("JOIN:"))
        {
            // Ya registrado arriba, solo log visual
            string clientName = msg.Substring(5);
            // Podrías guardar el nombre asociado al ID aquí
        }
    }

    void HandleMessageAsClient(string msg)
    {
        if (msg.StartsWith("PLAYER_ID:"))
        {
            int id = int.Parse(msg.Split(':')[1]);
            NetworkManager.Instance.myPlayerId = id;
            AddChatMsg($"<color=blue>Assigned Player ID: {id}</color>");
        }
        else if (msg.StartsWith("CHAT:"))
        {
            AddChatMsg(msg.Substring(5));
        }
        else if (msg == "GAME_START")
        {
            AddChatMsg("<color=green>Host started game! Loading...</color>");
            SceneManager.LoadScene("Game");
        }
    }

    // --- Helpers ---

    void HostBroadcast(string msg)
    {
        foreach (var client in connectedClients)
        {
            transport.Send(msg, client);
        }
    }

    void AddChatMsg(string txt)
    {
        if (chatText != null)
            chatText.text += txt + "\n";
    }
    
    void UpdatePlayerCount()
    {
        if(playerCountText != null)
            playerCountText.text = $"Players: {connectedClients.Count + 1}"; // +1 Host
    }

    void GoBack()
    {
        if (transport != null) transport.Close();
        Destroy(NetworkManager.Instance.gameObject);
        SceneManager.LoadScene("MainMenu");
    }
    
    void OnApplicationQuit()
    {
        if (transport != null) transport.Close();
    }
}