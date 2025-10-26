using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

    private Socket udpSocket;
    private EndPoint serverEndPoint;
    private byte[] buffer = new byte[2048];
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
        try
        {
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Blocking = false;

            serverEndPoint = new IPEndPoint(ip, 6000);


            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.serverIP = ip.ToString();
                NetworkManager.Instance.playerName = playerName;
                NetworkManager.Instance.udpSocket = udpSocket;
                NetworkManager.Instance.serverEndPoint = serverEndPoint;
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
        catch (Exception ex)
        {
            AppendChat($"ERROR: Connection failed - {ex.Message}");
        }
    }

    void Update()
    {
        if (connected && udpSocket != null)
        {
            ReceiveMessages();
        }
    }

    void ReceiveMessages()
    {
        EndPoint from = new IPEndPoint(IPAddress.Any, 0);

        try
        {
            while (udpSocket.Available > 0)
            {
                int bytes = udpSocket.ReceiveFrom(buffer, ref from);
                if (bytes > 0)
                {
                    string msg = Encoding.UTF8.GetString(buffer, 0, bytes);
                    HandleMessage(msg);
                }
            }
        }
        catch (SocketException) { }
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

            AppendChat("<color=cyan>Starting game...</color>");
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
        if (udpSocket == null || serverEndPoint == null) return;

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            udpSocket.SendTo(data, serverEndPoint);
        }
        catch (Exception ex)
        {
            AppendChat($"ERROR: Send failed - {ex.Message}");
        }
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
        udpSocket?.Close();

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
        udpSocket?.Close();
    }
}