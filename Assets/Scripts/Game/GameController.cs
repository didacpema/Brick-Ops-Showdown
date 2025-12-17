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
    public float sendRate = 30f; 

    [Tooltip("Timeout para detectar desconexiones")]
    public float connectionTimeout = 10f;
    #endregion

    #region Private Variables - Network
    private Socket udpSocket;
    private EndPoint serverEndPoint;
    private byte[] receiveBuffer = new byte[2048];
    private float nextSendTime = 0f;
    private float lastPacketTime = 0f;
    private ushort localSequenceNumber = 0;
    private Dictionary<string, ushort> remoteSequences = new Dictionary<string, ushort>();
    #endregion

    #region Private Variables - Game State
    private int myPlayerId = -1;
    private bool isInitialized = false;
    private InputManager cachedInputManager; 
    #endregion

    #region Private Variables - Stats
    private int packetsSent = 0;
    private int packetsReceived = 0;
    private float sessionStartTime = 0f;
    private float lastKeepAliveTime = 0f;
    private Vector3 lastSentPosition;
    private float lastSentRotation;
    private int lastSentShootCount;    
    private bool lastSentAiming;       
    private bool lastSentCrouching;    
    private bool lastSentGrounded;     
    private const float MOV_THRESHOLD = 0.01f;
    private const float ROT_THRESHOLD = 0.5f;
    #endregion

    #region Private Variables - Server
    private bool isServerHost = false;
    private Dictionary<string, PlayerInfo> serverPlayers = new Dictionary<string, PlayerInfo>();
    private List<IPEndPoint> serverClients = new List<IPEndPoint>();
    private Dictionary<int, string> nameCache = new Dictionary<int, string>();
    
    private class PlayerInfo
    {
        public string name;
        public int playerId;
    }
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
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
        
        myPlayerId = 1; 
        
        if (NetworkManager.Instance.udpSocket != null)
        {
            udpSocket = NetworkManager.Instance.udpSocket;
            Debug.Log("[GameController] Socket reutilizado de la Sala de Espera");
        }
        else
        {
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
        }
        
        if (!SetupPlayerManager())
        {
            ReturnToMenu();
            return;
        }
        
        SetupEventListeners();
        SetupInput();
        
        sessionStartTime = Time.time;
        lastPacketTime = Time.time;
        isInitialized = true;
        
        Debug.Log("<color=lime>Server host initialized! Playing as Player 1</color>");
    }

    void Update()
    {
        if (!isInitialized)
            return;

        if (isServerHost)
        {
            ReceiveAsServer();
        }
        else
        {
            ReceiveNetworkData();
            CheckConnection();
        }
        
        // SIEMPRE enviar datos periódicamente si estamos inicializados
        SendPeriodicUpdate();

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.UpdateRemotePlayers();
        }

        UpdateCamera();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMenu();
        }
        
        if (Time.time - lastKeepAliveTime > 3f && isInitialized)
        {
            SendMyName();
            lastKeepAliveTime = Time.time;
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
        if (!SetupNetworking())
            return false;

        if (!SetupPlayerManager())
            return false;

        SetupEventListeners();

        SetupInput();

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
        Debug.Log("[GameController] Network configured");
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

        EventManager.Instance.OnPlayerHit += HandlePlayerHit;
        EventManager.Instance.OnPlayerDied += HandlePlayerDeath;
        EventManager.Instance.OnPlayerRespawned += HandlePlayerRespawn;
        EventManager.Instance.OnPlayerSpawned += HandlePlayerSpawnedName;

        Debug.Log("[GameController] Event listeners configured");
    }

    void SetupInput()
    {
        if (PlayerManager.Instance?.LocalPlayer == null)
            return;

        cachedInputManager = PlayerManager.Instance.LocalPlayer.GetComponent<InputManager>();
        
        if (cachedInputManager == null)
        {
            Debug.LogWarning("[GameController] InputManager not found in prefab, adding one...");
            cachedInputManager = PlayerManager.Instance.LocalPlayer.AddComponent<InputManager>();
            cachedInputManager.Initialize(PlayerManager.Instance.LocalPlayer);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[GameController] Input configured");
    }
    #endregion

    #region Server Hosting
    void ReceiveAsServer()
    {
        EndPoint reusableEndPoint = new IPEndPoint(IPAddress.Any, 0);

        try
        {
            while (udpSocket.Available > 0)
            {
                int bytes = udpSocket.ReceiveFrom(receiveBuffer, ref reusableEndPoint);
                if (bytes > 0)
                {
                    IPEndPoint senderCopy = new IPEndPoint(((IPEndPoint)reusableEndPoint).Address, ((IPEndPoint)reusableEndPoint).Port);
                    string clientKey = senderCopy.ToString();

                    if (NetworkProtocol.NetworkPacketManager.UnwrapMessage(receiveBuffer, bytes, out ushort seq, out string message))
                    {
                        if (!remoteSequences.ContainsKey(clientKey)) 
                            remoteSequences[clientKey] = 0;

                        ushort lastSeq = remoteSequences[clientKey];

                        if (IsNewer(seq, lastSeq))
                        {
                            remoteSequences[clientKey] = seq; 
                            ProcessServerMessage(senderCopy, message); 
                            
                            lastPacketTime = Time.time;
                            packetsReceived++;
                        }
                    }
                }
            }
        }
        catch (SocketException) { }
    }

    void ProcessServerMessage(IPEndPoint sender, string message)
    {
        string clientKey = sender.ToString();
        
        // Si no está registrado, registrarlo primero
        if (!serverPlayers.ContainsKey(clientKey))
        {
            string playerName = "Player_" + (serverClients.Count + 2);
            
            // Intentar extraer el nombre si viene en el mensaje
            if (!message.StartsWith("PLAYER_DATA") && !message.StartsWith("SHOOT_DATA"))
            {
                playerName = message.Trim();
            }

            int newPlayerId = serverClients.Count + 2; // +2 porque el servidor es Player 1
            
            PlayerInfo playerInfo = new PlayerInfo
            {
                name = playerName,
                playerId = newPlayerId
            };
            
            serverPlayers[clientKey] = playerInfo;
            serverClients.Add(sender);
            
            SendToClient(sender, NetworkProtocol.BuildMessage(NetworkProtocol.PLAYER_ID, newPlayerId.ToString()));
            SendToClient(sender, $"Welcome {playerName}! You are Player {newPlayerId}");
            
            Debug.Log($"[Server] Player {newPlayerId} ({playerName}) connected from {sender}");
            
            if (serverClients.Count >= 1)
            {
                BroadcastToClients(NetworkProtocol.READY_TO_START);
                Debug.Log("[Server] Ready to start with multiple players");
            }
        }
        
        // Procesar el mensaje normalmente
        if (!NetworkProtocol.TryParseMessage(message, out string messageType, out string data))
            return;

        switch (messageType)
        {
            case NetworkProtocol.PLAYER_DATA:
                // NO reenviar los datos del cliente de vuelta a todos
                // Solo procesarlos localmente y enviar a OTROS clientes
                BroadcastToClients(message, sender); // exclude sender
                ProcessPlayerData(data);
                break;

            case NetworkProtocol.SHOOT_DATA:
                BroadcastToClients(message, sender);
                ProcessShootData(data);
                break;

            case NetworkProtocol.DEATH_DATA:
                BroadcastToClients(message, null); 
                ProcessDeathData(data);
                break;

            case NetworkProtocol.PLAYER_RESPAWN:
                BroadcastToClients(message, sender);
                ProcessRespawnData(data);
                break;
                
            case NetworkProtocol.BARRICADA_HIT:
                BarricadaHitData hitData = NetworkProtocol.DeserializeFromJson<BarricadaHitData>(data);
                if (hitData != null)
                {
                    BarricadaManager.Instance?.ApplyDamageToBarricada(hitData.barricadaId, hitData.damage);
                }
                // Broadcast a TODOS los clientes (sin excluir al sender ya que todos aplican localmente)
                BroadcastToClients(message);
                break;
                
            case NetworkProtocol.PLAYER_NAME:
                PlayerNameData nameData = NetworkProtocol.DeserializeFromJson<PlayerNameData>(data);
                if (nameData != null)
                {
                    RegisterAndApplyName(nameData.playerId, nameData.playerName);
                    BroadcastToClients(message, sender);
                }
                break;
            
            case NetworkProtocol.HEALTH_PACK_PICKUP:
                BroadcastToClients(message, sender);
                ProcessHealthPackPickup(data);
                break;

            case NetworkProtocol.START_GAME:
                if (serverClients.Count >= 1)
                {
                    BroadcastToClients(NetworkProtocol.GAME_START);
                    Debug.Log("[Server] Game started!");
                }
                break;

            case NetworkProtocol.OBJECT_TRANSFORM:
                BroadcastToClients(message, sender);
                ProcessObjectTransform(data);
                break;

            default:
                BroadcastToClients(message, sender);
                break;
        }
    }

    void SendToClient(IPEndPoint target, string msg)
    {
        if (udpSocket == null) return;
        
        try
        {
            localSequenceNumber++;
            byte[] data = NetworkProtocol.NetworkPacketManager.WrapMessage(msg, localSequenceNumber);
            udpSocket.SendTo(data, target);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Server] Error sending to {target}: {ex.Message}");
        }
    }

    public void BroadcastToClients(string msg, IPEndPoint exclude = null)
    {
        if (udpSocket == null) return;

        localSequenceNumber++;
        byte[] data = NetworkProtocol.NetworkPacketManager.WrapMessage(msg, localSequenceNumber);
        
        foreach (var client in serverClients)
        {
            if (exclude != null && client.Equals(exclude)) continue;
                
            try
            {
                udpSocket.SendTo(data, client);
            }
            catch (ObjectDisposedException)
            {
                Debug.LogWarning("[Server] Socket already disposed, cannot broadcast");
                break;
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
    }
    
   void SendPlayerData()
    {
        if (udpSocket == null) return;

        GameObject localPlayer = PlayerManager.Instance?.LocalPlayer;
        if (localPlayer == null) return;

        // 1. Obtenim l'estat actual complet
        PlayerState currentState;
        if (cachedInputManager != null)
        {
            currentState = cachedInputManager.GetCurrentPlayerState(myPlayerId);
        }
        else
        {
            currentState = new PlayerState(
                myPlayerId,
                localPlayer.transform.position,
                localPlayer.transform.eulerAngles.y
            );
        }

        // 2. Comprovem canvis de MOVIMENT
        bool positionChanged = Vector3.Distance(currentState.GetPosition(), lastSentPosition) > MOV_THRESHOLD;
        bool rotationChanged = Mathf.Abs(currentState.rotY - lastSentRotation) > ROT_THRESHOLD;

        // 3. Comprovem canvis d'ESTAT (Animacions)
        // Si qualsevol d'aquests canvia, hem d'enviar paquet encara que estiguem quiets
        bool stateChanged = (currentState.isAiming != lastSentAiming) ||
                            (currentState.isCrouching != lastSentCrouching) ||
                            (currentState.isGrounded != lastSentGrounded) ||
                            (currentState.shootCount != lastSentShootCount);

        // 4. Decidim si enviar o no
        // Si no hi ha cap canvi I fa menys d'1 segon de l'últim paquet -> NO ENVIEM
        if (!positionChanged && !rotationChanged && !stateChanged && Time.time - lastPacketTime < 1.0f)
        {
            return;
        }

        // 5. Actualitzem l'últim estat conegut
        lastSentPosition = currentState.GetPosition();
        lastSentRotation = currentState.rotY;
        lastSentAiming = currentState.isAiming;
        lastSentCrouching = currentState.isCrouching;
        lastSentGrounded = currentState.isGrounded;
        lastSentShootCount = currentState.shootCount;

        // 6. Enviem el paquet
        try
        {
            string jsonMessage = NetworkProtocol.BuildMessage(NetworkProtocol.PLAYER_DATA, currentState);
            localSequenceNumber++;
            byte[] packetBytes = NetworkProtocol.NetworkPacketManager.WrapMessage(jsonMessage, localSequenceNumber);

            if (isServerHost)
            {
                foreach (var client in serverClients)
                {
                    udpSocket.SendTo(packetBytes, client);
                    packetsSent++;
                }
            }
            else
            {
                udpSocket.SendTo(packetBytes, serverEndPoint);
                packetsSent++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameController] Send failed: {ex.Message}");
        }
    }

    public void SendShootData(int shooterId, int targetId, float damage, Vector3 hitPoint, bool didHit)
    {
        if (udpSocket == null) return;

        try
        {
            ShootData shootData = new ShootData(shooterId, targetId, damage, hitPoint, didHit);
            string message = NetworkProtocol.BuildMessage(NetworkProtocol.SHOOT_DATA, shootData);

            localSequenceNumber++;
            byte[] data = NetworkProtocol.NetworkPacketManager.WrapMessage(message, localSequenceNumber);
            
            if (didHit && targetId != -1 && targetId != myPlayerId)
            {
                GameObject enemy = PlayerManager.Instance?.GetPlayer(targetId);
                if (enemy != null)
                {
                    enemy.GetComponent<PlayerHealth>()?.ApplyRemoteDamage(damage);
                }
            }
            
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
            localSequenceNumber++;
            byte[] data = NetworkProtocol.NetworkPacketManager.WrapMessage(message, localSequenceNumber);

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

    public void SendPlayerRespawn(int playerId, Vector3 position, float rotation)
    {
        if (udpSocket == null)
            return;

        try
        {
            RespawnData respawnData = new RespawnData(playerId, position, rotation);
            string message = NetworkProtocol.BuildMessage(NetworkProtocol.PLAYER_RESPAWN, respawnData);
            localSequenceNumber++;
            byte[] data = NetworkProtocol.NetworkPacketManager.WrapMessage(message, localSequenceNumber);

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
            Debug.LogError($"[GameController] Send respawn failed: {ex.Message}");
        }
    }
    
    public void SendBarricadeHit(int barricadaId, int damage)
    {
        if (udpSocket == null) return;

        try
        {
            BarricadaHitData BarricadaData = new BarricadaHitData(barricadaId, damage);
            string message = NetworkProtocol.BuildMessage(NetworkProtocol.BARRICADA_HIT, BarricadaData);
            localSequenceNumber++;
            byte[] data = NetworkProtocol.NetworkPacketManager.WrapMessage(message, localSequenceNumber);

            if (isServerHost)
            {
                // El host aplica el daño localmente y hace broadcast a los clientes
                BarricadaManager.Instance?.ApplyDamageToBarricada(barricadaId, damage);
                BroadcastToClients(message);
            }
            else
            {
                // Cliente: enviar al servidor (el cliente ya aplicó el daño localmente)
                udpSocket.SendTo(data, serverEndPoint);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameController] Error sending barricade hit: {ex.Message}");
        }
    }
    
    void SendMyName()
    {
        if (udpSocket == null) return;
        
        string myName = NetworkManager.Instance != null ? NetworkManager.Instance.playerName : "Unknown";

        PlayerNameData data = new PlayerNameData(myPlayerId, myName);
        string msg = NetworkProtocol.BuildMessage(NetworkProtocol.PLAYER_NAME, data);
        
        localSequenceNumber++;
        byte[] packetBytes = NetworkProtocol.NetworkPacketManager.WrapMessage(msg, localSequenceNumber);
        
        if (isServerHost)
            BroadcastToClients(msg);
        else
            udpSocket.SendTo(packetBytes, serverEndPoint);
    }
    
    public void SendMessageToNetwork(string message)
    {
        if (udpSocket == null) 
        {
            Debug.LogWarning("[GameController] Cannot send message, socket is null");
            return;
        }
        
        try
        {
            localSequenceNumber++;
            byte[] data = NetworkProtocol.NetworkPacketManager.WrapMessage(message, localSequenceNumber);
            
            if (isServerHost)
            {
                BroadcastToClients(message);
            }
            else
            {
                udpSocket.SendTo(data, serverEndPoint);
            }
            
            packetsSent++;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameController] Error sending message: {ex.Message}");
        }
    }
    #endregion

    #region Network - Receiving
    void ReceiveNetworkData()
    {
        if (udpSocket == null) return;
        EndPoint reusableEndPoint = new IPEndPoint(IPAddress.Any, 0);

        try
        {
            while (udpSocket.Available > 0)
            {
                int bytes = udpSocket.ReceiveFrom(receiveBuffer, ref reusableEndPoint);
                if (bytes > 0)
                {
                    IPEndPoint senderCopy = new IPEndPoint(((IPEndPoint)reusableEndPoint).Address, ((IPEndPoint)reusableEndPoint).Port);
                    string senderKey = senderCopy.ToString();

                    if (NetworkProtocol.NetworkPacketManager.UnwrapMessage(receiveBuffer, bytes, out ushort seq, out string message))
                    {
                        lastPacketTime = Time.time; 
                        packetsReceived++;

                        if (!remoteSequences.ContainsKey(senderKey)) 
                            remoteSequences[senderKey] = 0;

                        if (IsNewer(seq, remoteSequences[senderKey]))
                        {
                            remoteSequences[senderKey] = seq; 
                            ProcessNetworkMessage(message); 
                        }
                    }
                }
            }
        }
        catch (SocketException) { }
    }
    
    bool IsNewer(ushort incoming, ushort current)
    {
        if (incoming == current) return false;

        return ((incoming > current) && (incoming - current <= 32768)) || 
               ((incoming < current) && (current - incoming > 32768));
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

            case NetworkProtocol.PLAYER_RESPAWN:
                ProcessRespawnData(data);
                break;

            case NetworkProtocol.SERVER_CLOSED:
                HandleServerClosed();
                break;
                
            case NetworkProtocol.PLAYER_NAME:
                PlayerNameData nameData = NetworkProtocol.DeserializeFromJson<PlayerNameData>(data);
                if (nameData != null)
                {
                    RegisterAndApplyName(nameData.playerId, nameData.playerName);
                    
                    if (isServerHost) BroadcastToClients(message);
                }
                break;
            
            case NetworkProtocol.HEALTH_PACK_PICKUP:
                ProcessHealthPackPickup(data);
                break;

            case NetworkProtocol.OBJECT_TRANSFORM:
                ProcessObjectTransform(data);
                break;
                
            case NetworkProtocol.BARRICADA_HIT:
                BarricadaHitData barricadaHitData = NetworkProtocol.DeserializeFromJson<BarricadaHitData>(data);
                if (barricadaHitData != null)
                {
                    BarricadaManager.Instance?.ApplyDamageToBarricada(barricadaHitData.barricadaId, barricadaHitData.damage);
                }
                break;
                
            case NetworkProtocol.PLAYER_ID:
                Debug.Log($"[Client] Confirmed ID from Game Server: {data}");
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

        PlayerManager.Instance?.UpdatePlayerState(state.playerId, state);
    }

    void ProcessShootData(string jsonData)
    {
        ShootData shootData = NetworkProtocol.DeserializeFromJson<ShootData>(jsonData);
        
        if (shootData == null)
            return;

        Debug.Log($"<color=blue>[Net] Received shoot: {shootData.shooterId} -> {shootData.targetId}</color>");

        if (shootData.targetId == myPlayerId)
        {
            GameObject localPlayer = PlayerManager.Instance?.LocalPlayer;
            if (localPlayer != null)
            {
                PlayerHealth health = localPlayer.GetComponent<PlayerHealth>();
                health?.TakeDamage(shootData.damage, shootData.shooterId);
            }
        }

        if (shootData.didHit && shootData.targetId >= 0 && shootData.targetId != myPlayerId)
        {
            GameObject remoteTarget = PlayerManager.Instance?.GetPlayer(shootData.targetId);
            remoteTarget?.GetComponent<PlayerHealth>()?.ApplyRemoteDamage(shootData.damage);
        }

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

        EventManager.Instance?.InvokeKillFeedMessage(
            $"Player {deathData.killerId} eliminated Player {deathData.victimId}"
        );

        if (deathData.victimId != myPlayerId)
        {
            GameObject victim = PlayerManager.Instance?.GetPlayer(deathData.victimId);
            if (victim != null)
            {
                PlayerHealth victimHealth = victim.GetComponent<PlayerHealth>();
                victimHealth?.MarkDeadLocally();
                victim.SetActive(false);
            }
        }
    }

    void ProcessRespawnData(string jsonData)
    {
        RespawnData respawnData = NetworkProtocol.DeserializeFromJson<RespawnData>(jsonData);

        if (respawnData == null)
            return;

        if (!NetworkProtocol.IsValidPlayerId(respawnData.playerId))
            return;

        Vector3 position = respawnData.GetPosition();
        float rotation = respawnData.rotY;

        GameObject player = PlayerManager.Instance?.GetPlayer(respawnData.playerId);
        if (player != null)
        {
            player.SetActive(true);
            player.transform.position = position;
            player.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            health?.ResetHealthState();
        }

        PlayerState state = new PlayerState(respawnData.playerId, position, rotation);
        PlayerManager.Instance?.UpdatePlayerState(respawnData.playerId, state);
    }

    void ProcessObjectTransform(string jsonData)
    {
        ObjectTransformData transformData = NetworkProtocol.DeserializeFromJson<ObjectTransformData>(jsonData);
        
        if (transformData == null)
            return;

        RotationAnimation[] rotationObjects = FindObjectsOfType<RotationAnimation>();
        foreach (var rotationObj in rotationObjects)
        {
            rotationObj.ApplyTransform(transformData);
        }

        Elevator[] elevators = FindObjectsOfType<Elevator>();
        foreach (var elevator in elevators)
        {
            elevator.ApplyTransform(transformData);
        }
    }

    void HandleServerClosed()
    {
        Debug.Log("[GameController] Server closed connection");
        ReturnToMenu();
    }
    
    void HandlePlayerSpawnedName(int id, bool isLocal)
    {
        if (nameCache.ContainsKey(id))
        {
            RegisterAndApplyName(id, nameCache[id]);
        }
    }
    
    void ProcessHealthPackPickup(string jsonData)
    {
        HealthPackData healthPackData = NetworkProtocol.DeserializeFromJson<HealthPackData>(jsonData);
        
        if (healthPackData == null)
            return;
        
        Debug.Log($"<color=green>[GameController] Processing health pack pickup: Pack {healthPackData.healthPackId} collected by Player {healthPackData.collectorId}</color>");
        
        HealthPack[] healthPacks = FindObjectsByType<HealthPack>(FindObjectsSortMode.None);
        foreach (HealthPack pack in healthPacks)
        {
            if (pack.healthPackId == healthPackData.healthPackId)
            {
                pack.ProcessNetworkPickup(healthPackData.collectorId);
                break;
            }
        }
    }
    #endregion

    #region Network - Connection
    void CheckConnection()
    {
        float timeSinceLastPacket = Time.time - lastPacketTime;
        if (timeSinceLastPacket > connectionTimeout)
        {
            Debug.LogWarning("[GameController] Connection lost due to timeout");
            ReturnToMenu();
        }
    }
    #endregion

    #region Event Handlers
    void HandlePlayerHit(int shooterId, int targetId, float damage, Vector3 hitPoint)
    {
        SendShootData(shooterId, targetId, damage, hitPoint, targetId != -1);
    }

    void HandlePlayerDeath(int victimId, int killerId)
    {
        SendDeathData(victimId, killerId);
        
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

            SendPlayerRespawn(playerId, position, 0f);
        }
        
        Debug.Log($"[GameController] Player {playerId} respawned at {position}");
    }
    #endregion

    #region Camera
    void UpdateCamera()
    {
        GameObject localPlayer = PlayerManager.Instance.LocalPlayer;
        Quaternion rotation = localPlayer.transform.rotation;
        Vector3 targetPos = localPlayer.transform.position + rotation * cameraOffset;
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

        if (isServerHost && serverClients.Count > 0 && udpSocket != null)
        {
            try
            {
                BroadcastToClients(NetworkProtocol.SERVER_CLOSED);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Cleanup] Could not notify clients: {ex.Message}");
            }
        }

        if (udpSocket != null)
        {
            try
            {
                udpSocket.Close();
                udpSocket = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameController] Error closing socket: {ex.Message}");
            }
        }

        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnPlayerHit -= HandlePlayerHit;
            EventManager.Instance.OnPlayerDied -= HandlePlayerDeath;
            EventManager.Instance.OnPlayerRespawned -= HandlePlayerRespawn;
        }

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

    void RegisterAndApplyName(int id, string name)
    {
        if (nameCache.ContainsKey(id))
            nameCache[id] = name;
        else
            nameCache.Add(id, name);

        if (PlayerManager.Instance != null)
        {
            GameObject playerObj = PlayerManager.Instance.GetPlayer(id);
            if (playerObj != null)
            {
                PlayerNameDisplay display = playerObj.GetComponent<PlayerNameDisplay>();
                if (display != null)
                {
                    display.SetName(name);
                }
                playerObj.name = $"Player_{id}_{name}";
            }
        }
    }
    /// <summary>
    /// Obtiene estadísticas de red para mostrar en UI.
    /// </summary>
    public void GetNetworkStats(out int pingMs, out int sent, out int received, out float packetsPerSecond)
    {
        float sessionTime = Time.time - sessionStartTime;
        packetsPerSecond = packetsReceived / Mathf.Max(1f, sessionTime);
        sent = packetsSent;
        received = packetsReceived;

        if (packetsReceived <= 0)
        {
            pingMs = -1;
        }
        else
        {
            pingMs = Mathf.Max(0, (int)((Time.time - lastPacketTime) * 1000f));
        }
    }
    #endregion
}