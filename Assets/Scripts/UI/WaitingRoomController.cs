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
    public GameObject connectionPanel; 
    public GameObject chatPanel;      
    public GameObject hostPanel;       

    [Header("UI Elements")]
    public TMP_InputField nameInput;
    public TMP_InputField ipInput;
    public TMP_InputField chatInput;
    public TMP_Text chatText;
    public TMP_Text playerCountText; 
    
    [Header("Buttons")]
    public Button connectButton;     
    public Button sendButton;        
    public Button startGameButton;   
    public Button backButton;        

    // Estado
    private UdpTransport transport;
    private bool isHost;
    private List<IPEndPoint> connectedClients = new List<IPEndPoint>();
    private ushort localSequenceNumber = 0;
    private Dictionary<EndPoint, ushort> remoteSequences = new Dictionary<EndPoint, ushort>();

    void Start()
    {
        isHost = NetworkManager.Instance.isServer;

        SetupUI();
        SetupListeners();

        if (isHost) StartHostLogic();
        
        else Debug.Log("[WaitingRoom] Client Mode: Waiting for user input...");
    }

    void SetupUI()
    {
        if (chatText != null) chatText.text = "";

        if (isHost)
        {
            if (connectionPanel) connectionPanel.SetActive(false);
            if (chatPanel) chatPanel.SetActive(true);
            if (hostPanel) hostPanel.SetActive(true);
            if (startGameButton) startGameButton.interactable = true;
            if (nameInput) nameInput.interactable = true;
            UpdatePlayerCount();
        }
        else
        {
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
            nameInput.text = NetworkManager.Instance.playerName;

            nameInput.onValueChanged.AddListener((newName) => 
            {
                NetworkManager.Instance.playerName = newName;
            });
        }
    }
    void StartHostLogic()
    {
        transport = new UdpTransport();
        
        if (transport.InitializeServer(6000))
        {
            NetworkManager.Instance.udpSocket = transport.Socket;
            AddChatMsg("<color=green>Server started on Port 6000.</color>");
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

        HostBroadcast("GAME_START");

        SceneManager.LoadScene("Game");
    }
    void ClientConnect()
    {
        string ipStr = ipInput.text.Trim();
        string name = nameInput.text.Trim();

        if (string.IsNullOrEmpty(ipStr)) ipStr = "127.0.0.1";
        if (string.IsNullOrEmpty(name)) name = "Client";

        NetworkManager.Instance.playerName = name;
        NetworkManager.Instance.serverIP = ipStr;

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

            connectionPanel.SetActive(false);
            chatPanel.SetActive(true);
            
            SendPacket($"JOIN:{name}", transport.RemoteEndPoint);
            AddChatMsg($"<color=green>Connecting to {ipStr}...</color>");
        }
    }
    void Update()
    {
        if (transport != null && transport.Socket != null)
        {
            while (transport.TryReceivePacket(out ushort seq, out string msg, out EndPoint sender))
            {
                // Filtre de paquets desordenats
                if (!remoteSequences.ContainsKey(sender)) remoteSequences[sender] = 0;
                
                remoteSequences[sender] = seq;

                if (isHost)
                    HandleMessageAsHost(msg, (IPEndPoint)sender);
                else
                    HandleMessageAsClient(msg);
            }
        }
    }
    void SendPacket(string message, EndPoint target)
    {
        if (transport == null || target == null) return;
        
        // EMPAQUETEM ABANS D'ENVIAR
        localSequenceNumber++;
        byte[] data = NetworkProtocol.NetworkPacketManager.WrapMessage(message, localSequenceNumber);
        
        transport.SendBytes(data, target);
    }

    void SendChatMessage()
    {
        string txt = chatInput.text.Trim();
        if (string.IsNullOrEmpty(txt)) return;

        string name = NetworkManager.Instance.playerName;
        string fullMsg = $"CHAT:[{name}]: {txt}";

        if (isHost)
        {
            AddChatMsg($"[{name}]: {txt}");
            HostBroadcast(fullMsg);
        }
        else
        {
            SendPacket(fullMsg, transport.RemoteEndPoint);
        }

        chatInput.text = "";
        chatInput.ActivateInputField();
    }

    void HandleMessageAsHost(string msg, IPEndPoint sender)
    {
        if (!connectedClients.Contains(sender))
        {
            connectedClients.Add(sender);
            int newId = connectedClients.Count + 1;
            
            SendPacket($"PLAYER_ID:{newId}", sender); // Ara usa SendPacket
            
            string joinMsg = $"Player {newId} joined!";
            HostBroadcast($"CHAT:<color=yellow>{joinMsg}</color>");
            AddChatMsg($"<color=yellow>{joinMsg}</color>");
            UpdatePlayerCount();
        }
        
        if (msg.StartsWith("CHAT:")) {
            HostBroadcast(msg);
            AddChatMsg(msg.Substring(5));
        }
    }

    void HandleMessageAsClient(string msg)
    {
        if (msg.StartsWith("PLAYER_ID:")) {
            int id = int.Parse(msg.Split(':')[1]);
            NetworkManager.Instance.myPlayerId = id;
            AddChatMsg($"<color=blue>Assigned Player ID: {id}</color>");
        }
        else if (msg.StartsWith("CHAT:")) {
            AddChatMsg(msg.Substring(5));
        }
        else if (msg == "GAME_START") {
            SceneManager.LoadScene("Game");
        }
    }

    void HostBroadcast(string msg)
    {
        localSequenceNumber++;
        byte[] data = NetworkProtocol.NetworkPacketManager.WrapMessage(msg, localSequenceNumber);
        foreach (var client in connectedClients)
        {
            transport.SendBytes(data, client);
        }
    }

    void AddChatMsg(string txt)
    {
        if (chatText != null)
            chatText.text += txt + "\n";
    }
    
    void UpdatePlayerCount() { if(playerCountText) playerCountText.text = $"Players: {connectedClients.Count + 1}"; }
    void GoBack() { transport?.Close(); Destroy(NetworkManager.Instance.gameObject); SceneManager.LoadScene("MainMenu"); }
    void OnApplicationQuit() { transport?.Close(); }
}