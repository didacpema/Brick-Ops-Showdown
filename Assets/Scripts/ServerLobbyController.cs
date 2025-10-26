using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using BrickOps.Networking;

public class ServerSceneController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text serverIPText;
    public TMP_Text playerCountText;
    public TMP_Text logText;
    public Button stopServerButton;

    [Header("Settings")]
    public int port = 6000;
    private const int MAX_PLAYERS = 2;

    private Socket udpSocket;
    private byte[] buffer = new byte[2048];

    private Dictionary<IPEndPoint, PlayerInfo> players = new Dictionary<IPEndPoint, PlayerInfo>();
    private List<IPEndPoint> clients = new List<IPEndPoint>();
    private bool gameStarted = false;

    private class PlayerInfo
    {
        public string name;
        public int playerId;
    }

    void Start()
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.isServer)
        {
            Debug.LogError("Not in server mode!");
            SceneManager.LoadScene("MainMenu");
            return;
        }

        stopServerButton.onClick.AddListener(StopServer);
        StartServer();
    }

    void StartServer()
    {
        udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udpSocket.Blocking = false;
        udpSocket.Bind(new IPEndPoint(IPAddress.Any, port));

        string localIP = GetLocalIPAddress();
        serverIPText.text = $"Server IP: {localIP}\nPort: {port}\n\nClients should connect to this IP";
        
        UpdatePlayerCount();
        Log("Server started successfully!");
        Log($"Waiting for players to connect...");
    }

    void Update()
    {
        ReceiveMessages();
    }

    void ReceiveMessages()
    {
        EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);

        try
        {
            while (udpSocket.Available > 0)
            {
                int bytes = udpSocket.ReceiveFrom(buffer, ref senderEndPoint);
                string msg = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();
                ProcessMessage((IPEndPoint)senderEndPoint, msg);
            }
        }
        catch (SocketException) { }
    }

    void ProcessMessage(IPEndPoint sender, string msg)
    {
     
        if (!players.ContainsKey(sender))
        {

            int playerId = clients.Count + 1;
            
            PlayerInfo playerInfo = new PlayerInfo
            {
                name = msg,
                playerId = playerId
            };
            
            players[sender] = playerInfo;
            clients.Add(sender);
            
  
            SendTo(sender, $"PLAYER_ID:{playerId}");
            SendTo(sender, $"Welcome {msg}! You are Player {playerId}");
            
     
            Broadcast($"{msg} joined as Player {playerId}", sender);
            
            Log($"Player {playerId} ({msg}) connected from {sender}");
            UpdatePlayerCount();
            
         
            CheckPlayersReady();
        }

        else
        {
            if (msg.StartsWith("PLAYER_DATA:"))
            {
             
                Broadcast(msg, sender);
                
     
                if (Time.frameCount % 60 == 0)
                {
                    Log($"Relaying game data from Player {players[sender].playerId} to other player");
                }
            }
            else if (msg == "START_GAME")
            {
          
                if (clients.Count >= MAX_PLAYERS && !gameStarted)
                {
                    StartGame();
                }
            }
            else
            {
       
                string formatted = $"[{players[sender].name}]: {msg}";
                Broadcast(formatted, sender);
                Log($"Chat - {formatted}");
            }
        }
    }

    void CheckPlayersReady()
    {
        if (clients.Count >= MAX_PLAYERS)
        {
       
            Broadcast("READY_TO_START");
            Log("2 players connected! Clients can now start the game.");
        }
    }

    void StartGame()
    {
        gameStarted = true;
        Log("Game starting!");
        

        Broadcast("GAME_START");
        
        Log("Game session started. Server continues relaying data...");
 
    }

    void SendTo(IPEndPoint target, string msg)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg);
        try { udpSocket.SendTo(data, target); }
        catch (Exception ex) { Log($"Error sending to {target}: {ex.Message}"); }
    }


    void Broadcast(string msg, IPEndPoint exclude = null)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg);
        foreach (var client in clients)
        {
            if (exclude != null && client.Equals(exclude)) continue;
            try { udpSocket.SendTo(data, client); }
            catch { }
        }
    }

    void UpdatePlayerCount()
    {
        playerCountText.text = $"Players Connected: {clients.Count}/{MAX_PLAYERS}";
        
        if (clients.Count >= MAX_PLAYERS)
        {
            playerCountText.color = Color.green;
        }
    }

    void Log(string msg)
    {
        Debug.Log($"[Server] {msg}");
        if (logText != null)
        {
            logText.text += $"[{System.DateTime.Now:HH:mm:ss}] {msg}\n";
            
      
            if (logText.text.Length > 5000)
                logText.text = logText.text.Substring(logText.text.Length - 5000);
            
            Canvas.ForceUpdateCanvases();
        }
    }

    string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch { }
        return "127.0.0.1";
    }

    void StopServer()
    {
        Broadcast("SERVER_CLOSED");
        udpSocket?.Close();
        
        if (NetworkManager.Instance != null)
        {
            Destroy(NetworkManager.Instance.gameObject);
        }
        
        SceneManager.LoadScene("MainMenu");
    }

    void OnApplicationQuit()
    {
        udpSocket?.Close();
    }
}