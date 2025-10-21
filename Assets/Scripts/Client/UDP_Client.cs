using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using TMPro;

public class UDPClient_Select : MonoBehaviour
{
    public string serverIP = "127.0.0.1";
    public int port = 6000;
    public string playerName = "Player";

    private Socket udpSocket;
    private EndPoint serverEndPoint;
    private byte[] buffer = new byte[1024];
    private bool connected = false;

    [Header("UI (TMP)")]
    public TMP_InputField ipInput;
    public TMP_InputField nameInput;
    public TMP_InputField chatInput;
    public TMP_Text chatDisplay;
    public UnityEngine.UI.Button connectButton;
    public UnityEngine.UI.Button sendButton;
    public GameObject loginPanel;
    public GameObject chatPanel;

    void Start()
    {
        loginPanel.SetActive(true);
        chatPanel.SetActive(false);
        connectButton.onClick.AddListener(OnConnectClicked);
        sendButton.onClick.AddListener(OnSendClicked);
    }

    void OnConnectClicked()
    {
        serverIP = ipInput.text.Trim();
        playerName = nameInput.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            AppendChat("Please enter a name!");
            return;
        }

        if (string.IsNullOrEmpty(serverIP))
            serverIP = "127.0.0.1";

        if (!IPAddress.TryParse(serverIP, out IPAddress ip))
        {
            AppendChat($"Invalid IP: {serverIP}");
            return;
        }

        Connect(ip);
    }

    void Connect(IPAddress ip)
    {
        udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udpSocket.Blocking = false;

        serverEndPoint = new IPEndPoint(ip, port);

        // Send initial message (name)
        SendToServer(playerName);
        connected = true;
        AppendChat($"Connected to {ip}:{port}");

        loginPanel.SetActive(false);
        chatPanel.SetActive(true);
    }

    void Update()
    {
        if (!connected || udpSocket == null) return;

        EndPoint from = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            while (udpSocket.Available > 0)
            {
                int bytes = udpSocket.ReceiveFrom(buffer, ref from);
                if (bytes > 0)
                {
                    string msg = Encoding.UTF8.GetString(buffer, 0, bytes);
                    AppendChat(msg);
                }
            }
        }
        catch (SocketException) { } // Non-blocking read
    }

    void OnSendClicked()
    {
        if (!connected) return;
        string msg = chatInput.text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        SendToServer(msg);
        // Display our own message locally
        AppendChat($"[{playerName}]: {msg}");
        
        chatInput.text = string.Empty;
        
        // Return focus to input field
        chatInput.ActivateInputField();
        chatInput.Select();
    }

    void SendToServer(string msg)
    {
        if (udpSocket == null || serverEndPoint == null) return;

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            udpSocket.SendTo(data, serverEndPoint);
        }
        catch (SocketException ex)
        {
            AppendChat($"Send failed: {ex.Message}");
        }
    }

    void AppendChat(string msg)
    {
        if (chatDisplay)
        {
            chatDisplay.text += msg + "\n";
            if (chatDisplay.text.Length > 5000)
                chatDisplay.text = chatDisplay.text[^5000..];
            
            // Force the UI to refresh
            Canvas.ForceUpdateCanvases();
            chatDisplay.ForceMeshUpdate(true);
        }
    }

    void OnApplicationQuit()
    {
        try { udpSocket?.Close(); } catch { }
    }
}
