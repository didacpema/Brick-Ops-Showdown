using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using TMPro;

public class TCPClient_Select : MonoBehaviour
{
    public string serverIP = "127.0.0.1";
    public int port = 5000;
    public string playerName = "Player";

    private Socket clientSocket;
    private byte[] buffer = new byte[1024];
    private bool connected = false;

    [Header("UI (TMP)")]
    public TMP_InputField ipInput;
    public TMP_InputField nameInput;
    public TMP_InputField chatInput;
    public TMP_Text chatDisplay;
    public UnityEngine.UI.Button connectButton;
    public UnityEngine.UI.Button sendButton;
    
    // Add these UI elements
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
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        clientSocket.Blocking = false;
        try { clientSocket.Connect(new IPEndPoint(ip, port)); }
        catch { }

        AppendChat($"Connecting to {ip}:{port}...");
    }

    void Update()
    {
        if (clientSocket == null) return;

        if (!connected)
        {
            if (clientSocket.Poll(0, SelectMode.SelectWrite) && clientSocket.Connected)
            {
                connected = true;
                AppendChat("Connected!");
                SendToServer(playerName);
                
                loginPanel.SetActive(false);
                chatPanel.SetActive(true);
            }
            else if (clientSocket.Poll(0, SelectMode.SelectError))
            {
                AppendChat("Connection failed.");
                clientSocket.Close();
                clientSocket = null;
                return;
            }
        }

        if (connected && clientSocket.Available > 0)
        {
            try
            {
                int bytes = clientSocket.Receive(buffer);
                if (bytes > 0)
                {
                    string msg = Encoding.UTF8.GetString(buffer, 0, bytes);
                    AppendChat(msg);
                }
            }
            catch (SocketException) { }
        }
    }

    void OnSendClicked()
    {
        if (!connected) return;
        string msg = chatInput.text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        SendToServer(msg);

        AppendChat($"[{playerName}]: {msg}");

        chatInput.text = string.Empty;
        chatInput.ActivateInputField();
        chatInput.Select();
    }


    void SendToServer(string msg)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            clientSocket.Send(data);
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
        }
    }

    void OnApplicationQuit()
    {
        try { clientSocket?.Shutdown(SocketShutdown.Both); } catch { }
        clientSocket?.Close();
    }
}
