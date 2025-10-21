using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Player Objects")]
    public GameObject player1Object;
    public GameObject player2Object;
    
    [Header("Network Settings")]
    public string serverIP = "127.0.0.1";
    public int port = 6000;
    private Socket udpSocket;
    private EndPoint serverEndPoint;
    private byte[] buffer = new byte[2048];
    
    [Header("Player Data")]
    private PlayerData myPlayerData;
    private PlayerData otherPlayerData;
    public bool isPlayer1 = true; // Determinar qué jugador eres
    
    private float updateInterval = 0.05f; // Enviar actualizaciones cada 50ms
    private float nextUpdateTime = 0f;
    
    void Start()
    {
        // Inicializar conexión UDP
        ConnectToServer();
        
        // Inicializar datos del jugador local
        InitializePlayerData();
        
        // Posicionar jugadores
        if (isPlayer1)
        {
            player1Object.transform.position = new Vector3(-5, 0, 0);
            player2Object.transform.position = new Vector3(5, 0, 0);
        }
        else
        {
            player1Object.transform.position = new Vector3(5, 0, 0);
            player2Object.transform.position = new Vector3(-5, 0, 0);
        }
    }
    
    void ConnectToServer()
    {
        udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udpSocket.Blocking = false;
        
        IPAddress ip = IPAddress.Parse(serverIP);
        serverEndPoint = new IPEndPoint(ip, port);
        
        Debug.Log($"Game connected to {serverIP}:{port}");
    }
    
    void InitializePlayerData()
    {
        myPlayerData = new PlayerData(
            "Player" + (isPlayer1 ? "1" : "2"),
            isPlayer1 ? new Vector3(-5, 0, 0) : new Vector3(5, 0, 0),
            100,
            0
        );
    }
    
    void Update()
    {
        // Recibir datos
        ReceivePlayerData();
        
        // Enviar datos periódicamente
        if (Time.time >= nextUpdateTime)
        {
            UpdateMyPlayerData();
            SendPlayerData();
            nextUpdateTime = Time.time + updateInterval;
        }
        
        // Actualizar visual de los jugadores
        UpdatePlayerVisuals();
    }
    
    void UpdateMyPlayerData()
    {
        // Actualizar posición del jugador local (ejemplo con WASD)
        GameObject myPlayer = isPlayer1 ? player1Object : player2Object;
        
        float moveSpeed = 5f;
        Vector3 movement = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W)) movement.z += 1;
        if (Input.GetKey(KeyCode.S)) movement.z -= 1;
        if (Input.GetKey(KeyCode.A)) movement.x -= 1;
        if (Input.GetKey(KeyCode.D)) movement.x += 1;
        
        if (movement != Vector3.zero)
        {
            myPlayer.transform.position += movement.normalized * moveSpeed * Time.deltaTime;
        }
        
        // Actualizar datos serializables
        Vector3 pos = myPlayer.transform.position;
        myPlayerData.posX = pos.x;
        myPlayerData.posY = pos.y;
        myPlayerData.posZ = pos.z;
    }
    
    void SendPlayerData()
    {
        if (udpSocket == null || serverEndPoint == null) return;
        
        try
        {
            // SERIALIZACIÓN con JsonUtility
            string json = JsonUtility.ToJson(myPlayerData);
            string message = "PLAYER_DATA:" + json;
            
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpSocket.SendTo(data, serverEndPoint);
            
            Debug.Log($"Sent: {message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send failed: {ex.Message}");
        }
    }
    
    void ReceivePlayerData()
    {
        if (udpSocket == null) return;
        
        EndPoint from = new IPEndPoint(IPAddress.Any, 0);
        
        try
        {
            while (udpSocket.Available > 0)
            {
                int bytes = udpSocket.ReceiveFrom(buffer, ref from);
                if (bytes > 0)
                {
                    string msg = Encoding.UTF8.GetString(buffer, 0, bytes);
                    
                    if (msg.StartsWith("PLAYER_DATA:"))
                    {
                        string json = msg.Substring("PLAYER_DATA:".Length);
                        
                        // DESERIALIZACIÓN
                        otherPlayerData = JsonUtility.FromJson<PlayerData>(json);
                        
                        Debug.Log($"Received player data: {otherPlayerData.playerName}");
                    }
                }
            }
        }
        catch (SocketException) { }
    }
    
    void UpdatePlayerVisuals()
    {
        // Actualizar posición del otro jugador basándose en datos recibidos
        if (otherPlayerData != null)
        {
            GameObject otherPlayer = isPlayer1 ? player2Object : player1Object;
            Vector3 targetPos = new Vector3(otherPlayerData.posX, otherPlayerData.posY, otherPlayerData.posZ);
            
            // Interpolación suave
            otherPlayer.transform.position = Vector3.Lerp(
                otherPlayer.transform.position, 
                targetPos, 
                Time.deltaTime * 10f
            );
        }
    }
    
    void OnApplicationQuit()
    {
        try { udpSocket?.Close(); } catch { }
    }
}