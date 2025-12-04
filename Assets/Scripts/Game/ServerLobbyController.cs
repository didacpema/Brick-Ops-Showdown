using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
    private const int MAX_PLAYERS = 10;

    private UdpTransport transport;

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
        transport?.Close();
        transport = new UdpTransport();

        if (!transport.InitializeServer(port))
        {
            Log("Failed to bind UDP socket.");
            return;
        }

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.udpSocket = transport.Socket;
            NetworkManager.Instance.serverEndPoint = transport.Socket?.LocalEndPoint;
        }

        string localIP = GetLocalIPAddress();
        serverIPText.text = $"Server IP: {localIP}\nPort: {port}\n\nClients should connect to this IP";
        
        UpdatePlayerCount();
        Log("Server started successfully!");
        Log($"Waiting for players to connect...");
    }

    void Update()
    {
        if (transport != null)
            ReceiveMessages();
    }

    void ReceiveMessages()
    {
        if (transport == null)
            return;

        while (transport.TryReceive(out string msg, out EndPoint sender))
        {
            string trimmed = msg?.Trim();
            if (trimmed == null)
                continue;

            ProcessMessage(sender as IPEndPoint, trimmed);
        }
    }

    void ProcessMessage(IPEndPoint sender, string msg)
    {
        // Nuevo jugador conectándose
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
            // Mensajes de jugadores ya conectados
            if (msg.StartsWith("PLAYER_DATA:"))
            {
                // Retransmitir posición del jugador
                Broadcast(msg, sender);
            }
            else if (msg.StartsWith("SHOOT_DATA:"))
            {
                // Retransmitir datos de disparo
                Broadcast(msg, sender);
                
                // Log reducido para no saturar
                if (Time.frameCount % 60 == 0)
                {
                    Log($"Relaying shoot data from Player {players[sender].playerId}");
                }
            }
            else if (msg.StartsWith("DEATH_DATA:"))
            {
                // Retransmitir datos de muerte
                Broadcast(msg, null); // Enviar a TODOS (incluyendo el que murió)
                Log($"Player death relayed from Player {players[sender].playerId}");
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
                // Chat genérico
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
        if (transport == null || target == null)
            return;

        transport.Send(msg, target);
    }

    void Broadcast(string msg, IPEndPoint exclude = null)
    {
        transport?.Broadcast(msg, clients, exclude);
    }

    public void BroadcastToClients(string msg, IPEndPoint exclude = null)
    {
        Broadcast(msg, exclude);
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
        transport?.Close();
        
        if (NetworkManager.Instance != null)
        {
            Destroy(NetworkManager.Instance.gameObject);
        }
        
        SceneManager.LoadScene("MainMenu");
    }

    void OnApplicationQuit()
    {
        transport?.Close();
    }
}