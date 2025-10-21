using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using TMPro;

public class TCPServer_Select : MonoBehaviour
{
    public int port = 5000;
    public string serverName = "UnityServerRoom";

    private Socket listenSocket;
    private List<Socket> clientSockets = new List<Socket>();
    private Dictionary<Socket, string> playerNames = new Dictionary<Socket, string>();
    private byte[] buffer = new byte[1024];

    [Header("UI (TMP)")]
    public TMP_Text logDisplay;

    void Start()
    {
        listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Blocking = false;
        listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listenSocket.Bind(new IPEndPoint(IPAddress.Any, port));
        listenSocket.Listen(10);
        Log($"[Server] Listening on port {port}");
    }

    void Update()
    {
        CheckSockets();
    }

    void CheckSockets()
    {
        List<Socket> checkRead = new List<Socket> { listenSocket };
        checkRead.AddRange(clientSockets);
        List<Socket> checkError = new List<Socket>(checkRead);

        Socket.Select(checkRead, null, checkError, 0);

        foreach (Socket err in checkError)
        {
            if (clientSockets.Contains(err))
            {
                RemoveClient(err, "error");
            }
        }

        if (checkRead.Contains(listenSocket))
        {
            try
            {
                Socket newClient = listenSocket.Accept();
                newClient.Blocking = false;
                clientSockets.Add(newClient);
                Log($"[Server] Client connected: {newClient.RemoteEndPoint}");
            }
            catch { }
            checkRead.Remove(listenSocket);
        }

        foreach (Socket client in new List<Socket>(checkRead))
        {
            try
            {
                if (client.Available == 0) continue;

                int bytes = client.Receive(buffer);
                if (bytes == 0)
                {
                    RemoveClient(client, "disconnected");
                    continue;
                }

                string msg = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();
                ProcessMessage(client, msg);
            }
            catch
            {
                RemoveClient(client, "exception");
            }
        }
    }

    void ProcessMessage(Socket client, string msg)
    {
        if (!playerNames.ContainsKey(client))
        {
            playerNames[client] = msg;
            SendTo(client, $"Welcome to {serverName}!");
            Broadcast($"{msg} joined the room.", null); // Changed: removed exclude parameter
            Log($"[Server] {msg} joined the room.");
        }
        else
        {
            string sender = playerNames[client];
            string formatted = $"[{sender}]: {msg}";
            Broadcast(formatted, null); // Changed: removed exclude parameter
            Log(formatted);
        }
    }

    void SendTo(Socket client, string msg)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            client.Send(data);
        }
        catch { }
    }

    void Broadcast(string msg, Socket exclude = null)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg);
        foreach (Socket c in clientSockets)
        {
            if (c == exclude) continue;
            try { c.Send(data); } catch { }
        }
    }

    void RemoveClient(Socket client, string reason)
    {
        if (playerNames.TryGetValue(client, out string name))
        {
            Log($"[Server] {name} left the room ({reason}).");
            Broadcast($"{name} left the room.");
        }
        playerNames.Remove(client);
        clientSockets.Remove(client);
        try { client.Close(); } catch { }
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

    void OnApplicationQuit()
    {
        foreach (Socket c in clientSockets)
        {
            try { c.Shutdown(SocketShutdown.Both); } catch { }
            c.Close();
        }
        listenSocket.Close();
    }
}
