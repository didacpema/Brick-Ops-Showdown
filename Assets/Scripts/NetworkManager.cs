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

        [Header("Network Config")]
        public string serverIP = "127.0.0.1";
        public int port = 6000;
        public string playerName = "Player";
        public bool isServer = false;
        public int myPlayerId = -1;

        [HideInInspector] public Socket udpSocket;
        [HideInInspector] public EndPoint serverEndPoint;

        // Evento para notificar mensajes
        public delegate void MessageReceivedHandler(string message);
        public event MessageReceivedHandler OnMessageReceived;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log($"[NetworkManager] Created singleton instance. IsServer: {isServer}");
            }
            else
            {
                Debug.LogWarning($"[NetworkManager] Duplicate instance destroyed. Keeping original.");
                Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Debug.Log("[NetworkManager] Singleton instance destroyed");
                
                // Cerrar socket si existe
                if (udpSocket != null)
                {
                    try
                    {
                        udpSocket.Close();
                        Debug.Log("[NetworkManager] UDP Socket closed");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[NetworkManager] Error closing socket: {ex.Message}");
                    }
                }
                
                Instance = null;
            }
        }

        // Método para verificar el estado de la conexión
        public bool IsConnected()
        {
            return udpSocket != null && serverEndPoint != null;
        }

        // Método para obtener información de debug
        public string GetDebugInfo()
        {
            return $"PlayerID: {myPlayerId} | PlayerName: {playerName} | IsServer: {isServer} | Connected: {IsConnected()}";
        }
    }
}