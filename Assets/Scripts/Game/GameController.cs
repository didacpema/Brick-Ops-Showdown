using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using BrickOps.Networking;
using BrickOps.Core;
using BrickOps.Players;

/// <summary>
/// Controlador principal del juego
/// Orquesta la comunicación entre sistemas y gestiona el ciclo de vida del juego
/// </summary>
public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    #region Inspector Variables
    [Header("Camera Settings")]
    [Tooltip("Offset de la cámara respecto al jugador")]
    public Vector3 cameraOffset = new Vector3(0, 2, -3);
    
    [Tooltip("Suavizado del seguimiento de cámara")]
    public float cameraFollowSpeed = 5f;

    [Header("Network Settings")]
    [Tooltip("Tasa de envío de paquetes por segundo")]
    public float sendRate = 30f; // 30 paquetes/seg = 33.33ms

    [Tooltip("Timeout para detectar desconexiones")]
    public float connectionTimeout = 10f;
    #endregion

    #region Private Variables - Network
    private Socket udpSocket;
    private EndPoint serverEndPoint;
    private byte[] receiveBuffer = new byte[2048];
    private float nextSendTime = 0f;
    private float lastPacketTime = 0f;
    #endregion

    #region Private Variables - Game State
    private int myPlayerId = -1;
    //public Camera mainCamera;
    private bool isInitialized = false;
    #endregion

    #region Private Variables - Stats
    private int packetsSent = 0;
    private int packetsReceived = 0;
    private float sessionStartTime = 0f;
    #endregion

    #region Private Variables - Server
    private bool isServerHost = false;
    private Dictionary<IPEndPoint, PlayerInfo> serverPlayers = new Dictionary<IPEndPoint, PlayerInfo>();
    private List<IPEndPoint> serverClients = new List<IPEndPoint>();
    
    private class PlayerInfo
    {
        public string name;
        public int playerId;
    }
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("=== GameController Start ===");
        
        // Verificar si somos servidor
        if (NetworkManager.Instance != null && NetworkManager.Instance.isServer)
        {
            isServerHost = true;
            InitializeAsServerHost();
            return;
        }
        
        if (!ValidateNetworkManager())
            return;

        if (!InitializeGame())
            return;

        sessionStartTime = Time.time;
        isInitialized = true;

        Debug.Log($"<color=lime>Game initialized successfully! Player {myPlayerId} ready.</color>");
    }

    void InitializeAsServerHost()
    {
        Debug.Log("[GameController] Initializing as SERVER HOST");
        
        myPlayerId = 1; // Servidor siempre es Player 1
        
        // Configurar socket como servidor
        try
        {
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Blocking = false;
            udpSocket.Bind(new IPEndPoint(IPAddress.Any, NetworkManager.Instance.port));
            
            NetworkManager.Instance.udpSocket = udpSocket;
            Debug.Log($"[GameController] Server listening on port {NetworkManager.Instance.port}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameController] Failed to start server: {ex.Message}");
            ReturnToMenu();
            return;
        }
        
        // Inicializar componentes de juego
        if (!SetupPlayerManager())
        {
            ReturnToMenu();
            return;
        }
        
        SetupEventListeners();
        SetupInput();
        //SetupCamera();
        
        sessionStartTime = Time.time;
        lastPacketTime = Time.time;
        isInitialized = true;
        
        Debug.Log("<color=lime>Server host initialized! Playing as Player 1</color>");
    }

    void Update()
    {
        if (!isInitialized)
            return;

        // Red - servidor maneja recepción diferente
        if (isServerHost)
        {
            ReceiveAsServer();
        }
        else
        {
            ReceiveNetworkData();
            CheckConnection();
        }
        
        // Enviar actualización solo si hay otros jugadores conectados
        if (!isServerHost || serverClients.Count > 0)
        {
            SendPeriodicUpdate();
        }

        // Jugadores
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.UpdateRemotePlayers();
        }

        // Cámara
        UpdateCamera();

        // Input de sistema
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMenu();
        }
    }

    void OnApplicationQuit()
    {
        Cleanup();
    }

    void OnDestroy()
    {
        Cleanup();
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region Initialization
    bool ValidateNetworkManager()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("[GameController] NetworkManager not found!");
            SceneManager.LoadScene("MainMenu");
            return false;
        }

        myPlayerId = NetworkManager.Instance.myPlayerId;
        
        if (myPlayerId == -1)
        {
            Debug.LogError("[GameController] Invalid Player ID!");
            SceneManager.LoadScene("WaitingRoom");
            return false;
        }

        Debug.Log($"[GameController] Network validated - Player ID: {myPlayerId}");
        return true;
    }

    bool InitializeGame()
    {
        // Network
        if (!SetupNetworking())
            return false;

        // Player Manager
        if (!SetupPlayerManager())
            return false;

        // Event Manager
        SetupEventListeners();

        // Input
        SetupInput();

        // Camera
        //SetupCamera();

        return true;
    }

    bool SetupNetworking()
    {
        udpSocket = NetworkManager.Instance.udpSocket;
        serverEndPoint = NetworkManager.Instance.serverEndPoint;

        if (udpSocket == null || serverEndPoint == null)
        {
            Debug.LogError("[GameController] UDP Socket or Server EndPoint is null!");
            return false;
        }

        lastPacketTime = Time.time;
        Debug.Log("[GameController] ✓ Network configured");
        return true;
    }

    bool SetupPlayerManager()
    {
        if (PlayerManager.Instance == null)
        {
            Debug.LogError("[GameController] PlayerManager not found!");
            return false;
        }

        GameObject localPlayer = PlayerManager.Instance.SpawnLocalPlayer(myPlayerId);
        
        if (localPlayer == null)
        {
            Debug.LogError("[GameController] Failed to spawn local player!");
            return false;
        }

        Debug.Log("[GameController] ✓ Player Manager configured");
        return true;
    }

    void SetupEventListeners()
    {
        if (EventManager.Instance == null)
        {
            Debug.LogWarning("[GameController] EventManager not found!");
            return;
        }

        // Suscribirse a eventos relevantes
        EventManager.Instance.OnPlayerHit += HandlePlayerHit;
        EventManager.Instance.OnPlayerDied += HandlePlayerDeath;
        EventManager.Instance.OnPlayerRespawned += HandlePlayerRespawn;

        Debug.Log("[GameController] ✓ Event listeners configured");
    }

    void SetupInput()
    {
        if (PlayerManager.Instance?.LocalPlayer == null)
            return;

        InputManager inputManager = gameObject.AddComponent<InputManager>();
        inputManager.Initialize(PlayerManager.Instance.LocalPlayer);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[GameController] ✓ Input configured");
    }

    // void SetupCamera()
    // {
    //     if (PlayerManager.Instance?.LocalPlayer != null)
    //     {
    //         mainCamera = PlayerManager.Instance.LocalPlayer.GetComponentInChildren<Camera>();
            
    //         if (mainCamera == null)
    //         {
    //             mainCamera = Camera.main;
    //         }
    //     }

    //     Debug.Log("[GameController] ✓ Camera configured");
    // }
    #endregion

    #region Server Hosting
    void ReceiveAsServer()
    {
        EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);

        try
        {
            while (udpSocket.Available > 0)
            {
                int bytes = udpSocket.ReceiveFrom(receiveBuffer, ref senderEndPoint);
                if (bytes > 0)
                {
                    string message = NetworkProtocol.BytesToMessage(receiveBuffer, bytes);
                    ProcessServerMessage((IPEndPoint)senderEndPoint, message);
                    
                    lastPacketTime = Time.time;
                    packetsReceived++;
                }
            }
        }
        catch (SocketException) { }
    }

    void ProcessServerMessage(IPEndPoint sender, string message)
    {
        // Nuevo cliente conectándose
        if (!serverPlayers.ContainsKey(sender))
        {
            string playerName = message.Trim();
            int newPlayerId = serverClients.Count + 2; // +2 porque el servidor es Player 1
            
            PlayerInfo playerInfo = new PlayerInfo
            {
                name = playerName,
                playerId = newPlayerId
            };
            
            serverPlayers[sender] = playerInfo;
            serverClients.Add(sender);
            
            // Enviar ID al cliente
            SendToClient(sender, NetworkProtocol.BuildMessage(NetworkProtocol.PLAYER_ID, newPlayerId.ToString()));
            SendToClient(sender, $"Welcome {playerName}! You are Player {newPlayerId}");
            
            Debug.Log($"[Server] Player {newPlayerId} ({playerName}) connected from {sender}");
            
            // Si hay 2 jugadores (servidor + 1 cliente), permitir inicio
            if (serverClients.Count >= 1)
            {
                BroadcastToClients(NetworkProtocol.READY_TO_START);
                Debug.Log("[Server] Ready to start with 2 players");
            }
            
            return;
        }
        
        // Mensajes de jugadores conectados
        if (!NetworkProtocol.TryParseMessage(message, out string messageType, out string data))
        {
            return;
        }

        switch (messageType)
        {
            case NetworkProtocol.PLAYER_DATA:
                // Retransmitir posición a otros clientes
                BroadcastToClients(message, sender);
                
                // Procesar también localmente para actualizar jugadores remotos
                ProcessPlayerData(data);
                break;

            case NetworkProtocol.SHOOT_DATA:
                BroadcastToClients(message, sender);
                ProcessShootData(data);
                break;

            case NetworkProtocol.DEATH_DATA:
                BroadcastToClients(message, null); // Enviar a todos
                ProcessDeathData(data);
                break;

            case NetworkProtocol.START_GAME:
                if (serverClients.Count >= 1)
                {
                    BroadcastToClients(NetworkProtocol.GAME_START);
                    Debug.Log("[Server] Game started!");
                }
                break;

            default:
                // Chat u otros mensajes
                BroadcastToClients(message, sender);
                break;
        }
    }

    void SendToClient(IPEndPoint target, string msg)
    {
        if (udpSocket == null) return;
        
        try
        {
            byte[] data = NetworkProtocol.MessageToBytes(msg);
            udpSocket.SendTo(data, target);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Server] Error sending to {target}: {ex.Message}");
        }
    }

    void BroadcastToClients(string msg, IPEndPoint exclude = null)
    {
        byte[] data = NetworkProtocol.MessageToBytes(msg);
        
        foreach (var client in serverClients)
        {
            if (exclude != null && client.Equals(exclude)) 
                continue;
                
            try
            {
                udpSocket.SendTo(data, client);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Server] Error broadcasting to {client}: {ex.Message}");
            }
        }
    }
    #endregion

    #region Network - Sending
    void SendPeriodicUpdate()
    {
        if (Time.time < nextSendTime)
            return;

        SendPlayerData();
        nextSendTime = Time.time + (1f / sendRate);
    }    void SendPlayerData()
    {
        if (udpSocket == null)
            return;

        GameObject localPlayer = PlayerManager.Instance?.LocalPlayer;
        if (localPlayer == null)
            return;        try
        {
            // Obtener InputManager para estados de animación
            InputManager inputManager = FindFirstObjectByType<InputManager>();
            PlayerState state;

            if (inputManager != null)
            {
                // Usar el nuevo método que incluye estados de animación
                state = inputManager.GetCurrentPlayerState(myPlayerId);
            }
            else
            {
                // Fallback: crear estado solo con posición
                state = new PlayerState(
                    myPlayerId,
                    localPlayer.transform.position,
                    localPlayer.transform.eulerAngles.y
                );
            }

            string message = NetworkProtocol.BuildMessage(NetworkProtocol.PLAYER_DATA, state);
            byte[] data = NetworkProtocol.MessageToBytes(message);

            if (isServerHost)
            {
                // Como servidor, broadcast a todos los clientes
                BroadcastToClients(message);
            }
            else
            {
                // Como cliente, enviar al servidor
                udpSocket.SendTo(data, serverEndPoint);
            }
            
            packetsSent++;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameController] Send failed: {ex.Message}");
        }
    }

    public void SendShootData(int shooterId, int targetId, float damage, Vector3 hitPoint, bool didHit)
    {
        if (udpSocket == null)
            return;

        try
        {
            ShootData shootData = new ShootData(shooterId, targetId, damage, hitPoint, didHit);
            string message = NetworkProtocol.BuildMessage(NetworkProtocol.SHOOT_DATA, shootData);
            byte[] data = NetworkProtocol.MessageToBytes(message);

            if (isServerHost)
            {
                BroadcastToClients(message);
            }
            else
            {
                udpSocket.SendTo(data, serverEndPoint);
            }
            
            Debug.Log($"<color=orange>[Net] Sent shoot: {shooterId} -> {targetId}</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameController] Send shoot failed: {ex.Message}");
        }
    }

    public void SendDeathData(int victimId, int killerId)
    {
        if (udpSocket == null)
            return;

        try
        {
            DeathData deathData = new DeathData(victimId, killerId);
            string message = NetworkProtocol.BuildMessage(NetworkProtocol.DEATH_DATA, deathData);
            byte[] data = NetworkProtocol.MessageToBytes(message);

            if (isServerHost)
            {
                BroadcastToClients(message);
            }
            else
            {
                udpSocket.SendTo(data, serverEndPoint);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameController] Send death failed: {ex.Message}");
        }
    }
    #endregion

    #region Network - Receiving
    void ReceiveNetworkData()
    {
        if (udpSocket == null)
            return;

        EndPoint from = new IPEndPoint(IPAddress.Any, 0);

        try
        {
            while (udpSocket.Available > 0)
            {
                int bytes = udpSocket.ReceiveFrom(receiveBuffer, ref from);
                
                if (bytes > 0)
                {
                    string message = NetworkProtocol.BytesToMessage(receiveBuffer, bytes);
                    ProcessNetworkMessage(message);
                    
                    lastPacketTime = Time.time;
                    packetsReceived++;
                }
            }
        }
        catch (SocketException) { }
    }

    void ProcessNetworkMessage(string message)
    {
        if (!NetworkProtocol.IsValidMessage(message))
        {
            Debug.LogWarning($"[GameController] Invalid message received");
            return;
        }

        if (!NetworkProtocol.TryParseMessage(message, out string messageType, out string data))
        {
            Debug.LogWarning($"[GameController] Failed to parse message: {message}");
            return;
        }

        switch (messageType)
        {
            case NetworkProtocol.PLAYER_DATA:
                ProcessPlayerData(data);
                break;

            case NetworkProtocol.SHOOT_DATA:
                ProcessShootData(data);
                break;

            case NetworkProtocol.DEATH_DATA:
                ProcessDeathData(data);
                break;

            case NetworkProtocol.SERVER_CLOSED:
                HandleServerClosed();
                break;

            default:
                Debug.LogWarning($"[GameController] Unknown message type: {messageType}");
                break;
        }
    }

    void ProcessPlayerData(string jsonData)
    {
        PlayerState state = NetworkProtocol.DeserializeFromJson<PlayerState>(jsonData);
        
        if (state == null || state.playerId == myPlayerId)
            return;

        if (!NetworkProtocol.IsValidPlayerId(state.playerId))
        {
            Debug.LogWarning($"[GameController] Invalid player ID in state: {state.playerId}");
            return;
        }

        // Actualizar en PlayerManager
        PlayerManager.Instance?.UpdatePlayerState(state.playerId, state);
    }

    void ProcessShootData(string jsonData)
    {
        ShootData shootData = NetworkProtocol.DeserializeFromJson<ShootData>(jsonData);
        
        if (shootData == null)
            return;

        Debug.Log($"<color=cyan>[Net] Received shoot: {shootData.shooterId} -> {shootData.targetId}</color>");

        // Si YO soy el objetivo, aplicar daño
        if (shootData.targetId == myPlayerId)
        {
            GameObject localPlayer = PlayerManager.Instance?.LocalPlayer;
            if (localPlayer != null)
            {
                PlayerHealth health = localPlayer.GetComponent<PlayerHealth>();
                health?.TakeDamage(shootData.damage, shootData.shooterId);
            }
        }

        // Si el disparo viene de OTRO jugador, reproducir efectos
        if (shootData.shooterId != myPlayerId)
        {
            GameObject shooter = PlayerManager.Instance?.GetPlayer(shootData.shooterId);
            if (shooter != null)
            {
                WeaponController weapon = shooter.GetComponent<WeaponController>();
                weapon?.PlayShootEffect(shootData.GetHitPoint(), shootData.didHit);
            }
        }
    }

    void ProcessDeathData(string jsonData)
    {
        DeathData deathData = NetworkProtocol.DeserializeFromJson<DeathData>(jsonData);
        
        if (deathData == null)
            return;

        // Notificar evento
        EventManager.Instance?.InvokeKillFeedMessage(
            $"Player {deathData.killerId} eliminated Player {deathData.victimId}"
        );

        // Si otro jugador murió, desactivar temporalmente
        if (deathData.victimId != myPlayerId)
        {
            GameObject victim = PlayerManager.Instance?.GetPlayer(deathData.victimId);
            if (victim != null)
            {
                victim.SetActive(false);
            }
        }
    }

    void HandleServerClosed()
    {
        Debug.Log("[GameController] Server closed connection");
        ReturnToMenu();
    }
    #endregion

    #region Network - Connection
    void CheckConnection()
    {
        float timeSinceLastPacket = Time.time - lastPacketTime;
        
        // if (timeSinceLastPacket > connectionTimeout)
        // {
        //     Debug.LogWarning($"[GameController] Connection timeout ({timeSinceLastPacket:F1}s)");
        //     ReturnToMenu();
        // }
    }
    #endregion

    #region Event Handlers
    void HandlePlayerHit(int shooterId, int targetId, float damage, Vector3 hitPoint)
    {
        // Enviar disparo por red
        SendShootData(shooterId, targetId, damage, hitPoint, targetId != -1);
    }

    void HandlePlayerDeath(int victimId, int killerId)
    {
        // Enviar muerte por red
        SendDeathData(victimId, killerId);
        
        // Mensaje en kill feed
        EventManager.Instance?.InvokeKillFeedMessage(
            $"Player {killerId} eliminated Player {victimId}"
        );
    }

    void HandlePlayerRespawn(int playerId, Vector3 position)
    {
        if (playerId == myPlayerId)
        {
            GameObject localPlayer = PlayerManager.Instance?.LocalPlayer;
            if (localPlayer != null)
            {
                localPlayer.transform.position = position;
                localPlayer.transform.rotation = Quaternion.identity;
            }
        }
        
        Debug.Log($"[GameController] Player {playerId} respawned at {position}");
    }
    #endregion

    #region Camera
    void UpdateCamera()
    {
        // if (mainCamera == null || PlayerManager.Instance?.LocalPlayer == null)
        //     return;

        GameObject localPlayer = PlayerManager.Instance.LocalPlayer;
        Quaternion rotation = localPlayer.transform.rotation;
        Vector3 targetPos = localPlayer.transform.position + rotation * cameraOffset;

        // mainCamera.transform.position = Vector3.Lerp(
        //     mainCamera.transform.position,
        //     targetPos,
        //     Time.deltaTime * cameraFollowSpeed
        // );

        // mainCamera.transform.LookAt(localPlayer.transform.position + Vector3.up);
    }
    #endregion

    #region Scene Management
    public void ReturnToMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Cleanup();

        if (NetworkManager.Instance != null)
        {
            Destroy(NetworkManager.Instance.gameObject);
        }

        SceneManager.LoadScene("MainMenu");
    }

    void Cleanup()
    {
        isInitialized = false;

        // Notificar a clientes si somos servidor
        if (isServerHost && serverClients.Count > 0)
        {
            BroadcastToClients(NetworkProtocol.SERVER_CLOSED);
        }

        // Cerrar socket
        if (udpSocket != null)
        {
            try
            {
                udpSocket.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameController] Error closing socket: {ex.Message}");
            }
        }

        // Limpiar eventos
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnPlayerHit -= HandlePlayerHit;
            EventManager.Instance.OnPlayerDied -= HandlePlayerDeath;
            EventManager.Instance.OnPlayerRespawned -= HandlePlayerRespawn;
        }

        // Limpiar jugadores
        PlayerManager.Instance?.ClearAllPlayers();

        Debug.Log("[GameController] Cleanup completed");
    }
    #endregion

    #region Debug & Stats
    public string GetDebugInfo()
    {
        float sessionTime = Time.time - sessionStartTime;
        float packetsPerSecond = packetsReceived / Mathf.Max(1f, sessionTime);
        
        return $"Session: {sessionTime:F0}s | " +
               $"Sent: {packetsSent} | Received: {packetsReceived} | " +
               $"Rate: {packetsPerSecond:F1} pps | " +
               $"Players: {PlayerManager.Instance?.RemotePlayerCount ?? 0}";
    }
    #endregion
}