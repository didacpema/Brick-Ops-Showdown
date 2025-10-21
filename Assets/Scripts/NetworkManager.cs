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
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}