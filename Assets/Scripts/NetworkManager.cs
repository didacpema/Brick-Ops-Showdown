using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace BrickOps.Networking
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }
        
        [Header("Network Settings")]
        public string serverIP = "127.0.0.1";
        public int port = 6000;
        public string playerName = "Player";
        public bool isServer = false;
        
        private Socket udpSocket;
        private EndPoint serverEndPoint;
        private byte[] buffer = new byte[2048];
        
        // Eventos para notificar a otras escenas
        public delegate void MessageReceivedHandler(string message);
        public event MessageReceivedHandler OnMessageReceived;
        
        void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public void InitializeAsClient(string ip, int portNum, string name)
        {
            serverIP = ip;
            port = portNum;
            playerName = name;
            isServer = false;
            
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Blocking = false;
            
            IPAddress ipAddress = IPAddress.Parse(serverIP);
            serverEndPoint = new IPEndPoint(ipAddress, port);
            
            // Enviar nombre inicial
            SendNetworkMessage(playerName);
            
            Debug.Log($"Client initialized: {serverIP}:{port}");
        }
        
        public void InitializeAsServer(int portNum)
        {
            port = portNum;
            isServer = true;
            
            // El servidor se inicializa en su propia escena
            Debug.Log($"Server mode set on port {port}");
        }
        
        void Update()
        {
            if (udpSocket != null && !isServer)
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
                        
                        // Notificar a los suscriptores
                        OnMessageReceived?.Invoke(msg);
                        
                        Debug.Log($"Received: {msg}");
                    }
                }
            }
            catch (SocketException) { }
        }
        
        public void SendNetworkMessage(string message)
        {
            if (udpSocket == null || serverEndPoint == null || isServer) return;
            
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                udpSocket.SendTo(data, serverEndPoint);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Send failed: {ex.Message}");
            }
        }
        
        public void SendPlayerData(PlayerData playerData)
        {
            string json = JsonUtility.ToJson(playerData);
            SendNetworkMessage("PLAYER_DATA:" + json);
        }
        
        void OnApplicationQuit()
        {
            try { udpSocket?.Close(); } catch { }
        }
    }
}