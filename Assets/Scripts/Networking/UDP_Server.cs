using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UDPServer_Select : MonoBehaviour
{
    public int port = 6000;
    public string serverName = "UnityServerRoom_UDP";

    private Socket udpSocket;
    private byte[] buffer = new byte[1024];
    private Dictionary<IPEndPoint, string> playerNames = new Dictionary<IPEndPoint, string>();
    private List<IPEndPoint> clients = new List<IPEndPoint>();

    [Header("UI (TMP)")]
    public TMP_Text logDisplay;
    private bool gameStarted = false;
    private const int MAX_PLAYERS = 2;

    void Start()
    {
        udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udpSocket.Blocking = false;
        udpSocket.Bind(new IPEndPoint(IPAddress.Any, port));
        Log($"[UDP Server] Listening on port {port}");
    }

    void Update()
    {
        ReceiveMessages();
    }

    void CheckStartGame()
    {
        if (!gameStarted && clients.Count >= MAX_PLAYERS)
        {
            gameStarted = true;
            Log("[Server] Starting game with 2 players!");
            
           
            BroadcastGameStart();
            

            SceneManager.LoadScene("Game");
        }
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
        if (!playerNames.ContainsKey(sender))
        {
            playerNames[sender] = msg;
            clients.Add(sender);
            SendTo(sender, $"Welcome to {serverName}!");
            Broadcast($"{msg} joined the room.", sender);
            Log($"[UDP Server] {msg} joined from {sender}");

            CheckStartGame();
        }        else
        {
            if (msg.StartsWith("PLAYER_DATA:"))
            {
                BroadcastPlayerData(msg, sender);
            }
            else if (msg.StartsWith("BARRICADA_DAMAGE:"))
            {
                // Formato: BARRICADA_DAMAGE:{id}:{damage}
                ProcessBarricadaDamage(msg);
            }
            else
            {
                string name = playerNames[sender];
                string formatted = $"[{name}]: {msg}";
                Broadcast(formatted, sender);
                Log(formatted);
            }
        }
    }

    void SendTo(IPEndPoint target, string msg)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg);
        try { udpSocket.SendTo(data, target); }
        catch { }
    }

    void Broadcast(string msg, IPEndPoint exclude = null)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg);
        foreach (var client in clients)
        {
            if (exclude != null && client.Equals(exclude)) continue;
            try { udpSocket.SendTo(data, client); } catch { }
        }
    }

    void Log(string msg)
    {
        Debug.Log(msg);
        if (logDisplay)
        {
            logDisplay.text += msg + "\n";
            if (logDisplay.text.Length > 5000)
                logDisplay.text = logDisplay.text[^5000..];
        }
    }


    void BroadcastGameStart()
    {
        string startMsg = "GAME_START";
        Broadcast(startMsg);
    }


    void BroadcastPlayerData(string data, IPEndPoint sender)
    {
        Broadcast(data, sender);
    }

    void ProcessBarricadaDamage(string msg)
    {
        // Formato: BARRICADA_DAMAGE:{id}:{damage}
        try
        {
            string[] parts = msg.Split(':');
            if (parts.Length >= 3)
            {
                int barricadaId = int.Parse(parts[1]);
                int damage = int.Parse(parts[2]);
                
                // Aplicar daño a la barricada en el servidor
                if (BarricadaManager.Instance != null)
                {
                    BarricadaManager.Instance.ApplyDamageToBarricada(barricadaId, damage);
                }
                
                Log($"[Server] Barricada {barricadaId} took {damage} damage");
            }
        }
        catch (System.Exception ex)
        {
            Log($"[Server] Error processing barricada damage: {ex.Message}");
        }
    }
}
