using System;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using BrickOps.Networking;

public class WaitingRoomController : MonoBehaviour
{
    [Header("Connection UI")]
    public GameObject connectionPanel;
    public TMP_InputField nameInput;
    public TMP_InputField ipInput;
    public Button connectButton;

    [Header("Chat UI")]
    public GameObject chatPanel;
    public TMP_Text statusText;
    public TMP_Text playerCountText;
    public TMP_Text chatText;
    public TMP_InputField chatInput;
    public Button sendButton;
    public ScrollRect chatScrollRect;

    [Header("Game UI")]
    public Button playButton;
    public Button disconnectButton;

    private UdpTransport transport;
    private bool connected = false;
    private int myPlayerId = -1;
    private bool canStartGame = false;

    void Start()
    {

        connectionPanel.SetActive(true);
        chatPanel.SetActive(false);
        playButton.interactable = false;

        connectButton.onClick.AddListener(OnConnect);
        sendButton.onClick.AddListener(SendChatMessage);
        playButton.onClick.AddListener(OnPlayGame);
        disconnectButton.onClick.AddListener(OnDisconnect);


        chatInput.onSubmit.AddListener((text) => SendChatMessage());
    }

    void OnConnect()
    {
        string playerName = nameInput.text.Trim();
        string serverIP = ipInput.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            AppendChat("ERROR: Please enter a name!");
            return;
        }

        if (string.IsNullOrEmpty(serverIP))
            serverIP = "127.0.0.1";

        if (!IPAddress.TryParse(serverIP, out IPAddress ip))
        {
            AppendChat($"ERROR: Invalid IP: {serverIP}");
            return;
        }

        ConnectToServer(ip, playerName);
    }

    void ConnectToServer(IPAddress ip, string playerName)
    {
        transport?.Close();
        transport = new UdpTransport();

        if (!transport.InitializeClient(ip, 6000))
        {
            AppendChat("ERROR: Unable to create UDP socket.");
            return;
        }

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.serverIP = ip.ToString();
            NetworkManager.Instance.playerName = playerName;
            NetworkManager.Instance.udpSocket = transport.Socket;
            NetworkManager.Instance.serverEndPoint = transport.RemoteEndPoint;
            NetworkManager.Instance.isServer = false; // ⭐ IMPORTANTE: marcar como cliente
        }

        SendMess(playerName);

        connected = true;
        statusText.text = $"Connected to {ip}:6000";
        statusText.color = Color.green;

        connectionPanel.SetActive(false);
        chatPanel.SetActive(true);

        AppendChat($"Connected as {playerName}");
        AppendChat("Waiting for players...");
    }

    void Update()
    {
        if (connected && transport != null)
        {
            ReceiveMessages();
        }
    }

    void ReceiveMessages()
    {
        if (transport == null)
            return;

        while (transport.TryReceive(out string msg, out EndPoint sender))
        {
            if (!string.IsNullOrEmpty(msg))
                HandleMessage(msg);
        }
    }

    void HandleMessage(string msg)
    {
        Debug.Log($"[Client] Received: {msg}");

        if (msg.StartsWith("PLAYER_ID:"))
        {

            string idStr = msg.Substring("PLAYER_ID:".Length);
            myPlayerId = int.Parse(idStr);
            
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.myPlayerId = myPlayerId;
            }
            
            AppendChat($"<color=yellow>You are Player {myPlayerId}</color>");
        }
        else if (msg == "READY_TO_START")
        {

            canStartGame = true;
            playButton.interactable = true;
            playerCountText.text = "Players: 2/2 - Ready!";
            playerCountText.color = Color.green;
            AppendChat("<color=green>2 players connected! Press PLAY to start!</color>");
        }
        else if (msg == "GAME_START")
        {

            AppendChat("<color=blue>Starting game...</color>");
            Invoke("LoadGameScene", 1f);
        }
        else if (msg == "SERVER_CLOSED")
        {
            AppendChat("<color=red>Server closed connection</color>");
            OnDisconnect();
        }
        else
        {

            AppendChat(msg);
        }
    }

    void SendMess(string msg)
    {
        if (transport == null || transport.RemoteEndPoint == null) return;

        transport.Send(msg, transport.RemoteEndPoint);
    }

    void SendChatMessage()
    {
        if (!connected) return;

        string msg = chatInput.text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        SendMess(msg);
        

        string myName = NetworkManager.Instance != null ? NetworkManager.Instance.playerName : "Me";
        AppendChat($"<color=blue>[{myName}]: {msg}</color>");
        
        chatInput.text = "";
        chatInput.ActivateInputField();
    }

    void OnPlayGame()
    {
        if (!canStartGame)
        {
            AppendChat("Waiting for more players...");
            return;
        }

        SendMess("START_GAME");
        playButton.interactable = false;
        AppendChat("Starting game...");
    }

    void LoadGameScene()
    {
        connected = false;
        
        Debug.Log("[WaitingRoom] Transferring control to GameController...");
        
        SceneManager.LoadScene("Game");
    }

    void OnDisconnect()
    {
        connected = false;
        transport?.Close();

        if (NetworkManager.Instance != null)
        {
            Destroy(NetworkManager.Instance.gameObject);
        }

        SceneManager.LoadScene("MainMenu");
    }

    void AppendChat(string msg)
    {
        if (chatText != null)
        {
            chatText.text += msg + "\n";
            
  
            if (chatText.text.Length > 5000)
                chatText.text = chatText.text.Substring(chatText.text.Length - 5000);


            Canvas.ForceUpdateCanvases();
            if (chatScrollRect != null)
            {
                chatScrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }

    void OnDestroy()
    {

        connected = false;
        Debug.Log("[WaitingRoom] WaitingRoomController destroyed, stopped listening");
    }

    void OnApplicationQuit()
    {
        connected = false;
        transport?.Close();
    }
}