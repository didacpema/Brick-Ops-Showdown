using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using BrickOps.Networking;
using BrickOps.Core;

public class GameController : MonoBehaviour
{
    public static GameController instance;

    #region Inspector Variables
    [Header("Prefabs")]
    [Tooltip("Arrastra aquí el prefab del jugador")]
    public GameObject playerPrefab;

    [Header("Spawn Points")]
    [Tooltip("Posiciones de spawn para cada jugador")]
    public Transform[] spawnPoints;

    [Header("Camera")]
    [Tooltip("Cámara principal que seguirá a MI jugador")]
    public Camera mainCamera;

    [Header("UI")]
    public TMP_Text infoText;
    public TMP_Text killFeedText; // Nuevo: feed de kills

    [Header("Settings")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 100f;
    public float sendRate = 0.05f;
    public Vector3 cameraOffset = new Vector3(0, 2, -3);
    #endregion

    #region Private Variables
    private Socket udpSocket;
    private EndPoint serverEndPoint;
    private byte[] buffer = new byte[2048];

    // MI jugador (el que controlo)
    private int myPlayerId;
    private GameObject myPlayerObject;
    private PlayerState myState;
    private PlayerHealth myHealth;
    private InputManager inputManager;

    // OTROS jugadores (diccionario: playerId -> GameObject)
    private Dictionary<int, GameObject> otherPlayers = new Dictionary<int, GameObject>();
    private Dictionary<int, PlayerState> otherStates = new Dictionary<int, PlayerState>();

    private float nextSendTime = 0f;
    
    // Estadísticas
    private int packetsSent = 0;
    private int packetsReceived = 0;
    
    // Latencia
    private float pingInterval = 1.0f;
    private float nextPingTime = 0f;
    private long lastPingTimestampMs = 0;
    private float rttMs = -1f;
    private float rttSmoothedMs = -1f;
    
    // Kill feed
    private List<string> killFeedMessages = new List<string>();
    private const int MAX_KILL_FEED_LINES = 5;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("=== GameController Start ===");
        
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            SceneManager.LoadScene("MainMenu");
            return;
        }

        myPlayerId = NetworkManager.Instance.myPlayerId;
        Debug.Log($"My Player ID: {myPlayerId}");

        if (myPlayerId == -1)
        {
            Debug.LogError("Player ID not assigned!");
            SceneManager.LoadScene("WaitingRoom");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("¡PlayerPrefab no asignado en el Inspector!");
            return;
        }

        SetupNetworking();
        SpawnMyPlayer();
        SetupInputManager();
        SetupUI();

        mainCamera = myPlayerObject != null ? myPlayerObject.GetComponentInChildren<Camera>() : mainCamera;

        
        Debug.Log($"<color=lime>Player {myPlayerId} initialized and ready!</color>");
    }

    void Update()
    {
        if (inputManager != null)
        {
            inputManager.HandleInput();
        }

        ReceiveData();

        if (Time.time >= nextSendTime)
        {
            SendMyData();
            nextSendTime = Time.time + sendRate;
        }

        // Enviar ping periódico para medir RTT real si el servidor responde con PONG
        if (Time.time >= nextPingTime)
        {
            SendPing();
            nextPingTime = Time.time + pingInterval;
        }

        UpdateOtherPlayers();
        UpdateCamera();
        UpdateInfoText();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMenu();
        }
    }

    void OnApplicationQuit()
    {
        udpSocket?.Close();
    }
    #endregion

    #region Initialization
    void SetupNetworking()
    {
        udpSocket = NetworkManager.Instance.udpSocket;
        serverEndPoint = NetworkManager.Instance.serverEndPoint;

        if (udpSocket == null || serverEndPoint == null)
        {
            Debug.LogError("UDP Socket or Server EndPoint is null!");
            return;
        }
        
        Debug.Log("✓ Network configured");
    }

    void SpawnMyPlayer()
    {
        Vector3 spawnPosition = GetSpawnPosition(myPlayerId);
        
        myPlayerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        myPlayerObject.name = $"Player_{myPlayerId}_ME";

        Camera cam = myPlayerObject.GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cam.gameObject.SetActive(true);
            mainCamera = cam;
        }
        
        Renderer renderer = myPlayerObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(renderer.material);
            renderer.material.color = Color.blue;
        }

        Rigidbody rb = myPlayerObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = myPlayerObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        WeaponController weapon = myPlayerObject.GetComponent<WeaponController>();
        if (weapon != null && mainCamera != null)
        {
            weapon.InitializeForLocalPlayer(mainCamera);
        }

        // Inicializar PlayerHealth
        myHealth = myPlayerObject.GetComponent<PlayerHealth>();
        if (myHealth == null)
        {
            myHealth = myPlayerObject.AddComponent<PlayerHealth>();
        }
        myHealth.Initialize(myPlayerId, true);

        myState = new PlayerState(myPlayerId, myPlayerObject.transform.position, myPlayerObject.transform.eulerAngles.y);

        Debug.Log($"✓ Spawned MY player at {spawnPosition}");
    }

    Vector3 GetSpawnPosition(int playerId)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = (playerId - 1) % spawnPoints.Length;
            if (spawnPoints[index] != null)
            {
                return spawnPoints[index].position;
            }
        }

        switch (playerId)
        {
            case 1: return new Vector3(-5, 1, 0);
            case 2: return new Vector3(5, 1, 0);
            default: return new Vector3(0, 1, playerId * 3);
        }
    }

    void SetupInputManager()
    {
        inputManager = gameObject.AddComponent<InputManager>();
        inputManager.Initialize(this, myPlayerObject);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("✓ Input manager configured");
    }

    void SetupUI()
    {
        if (infoText != null)
        {
            UpdateInfoText();
        }
        
        if (killFeedText != null)
        {
            killFeedText.text = "";
        }
    }
    #endregion

    #region Player Management
    void SpawnOtherPlayer(int playerId, Vector3 position, float rotation)
    {
        if (otherPlayers.ContainsKey(playerId))
        {
            Debug.LogWarning($"Player {playerId} already exists!");
            return;
        }

        GameObject otherPlayer = Instantiate(playerPrefab, position, Quaternion.Euler(0, rotation, 0));
        otherPlayer.name = $"Player_{playerId}_OTHER";

        Camera cam = otherPlayer.GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cam.enabled = false;
            cam.gameObject.SetActive(false);
        }
        AudioListener al = otherPlayer.GetComponentInChildren<AudioListener>();
        if (al != null)
        {
            al.enabled = false;
        }

        Renderer renderer = otherPlayer.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(renderer.material);
            renderer.material.color = Color.red;
        }

        Rigidbody rb = otherPlayer.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Inicializar PlayerHealth
        PlayerHealth health = otherPlayer.GetComponent<PlayerHealth>();
        if (health == null)
        {
            health = otherPlayer.AddComponent<PlayerHealth>();
        }
        health.Initialize(playerId, false);

        // Desactivar WeaponController para jugadores remotos
        WeaponController weapon = otherPlayer.GetComponent<WeaponController>();
        if (weapon != null)
        {
            weapon.enabled = false; // No disparan localmente
        }

        otherPlayers[playerId] = otherPlayer;
        
        Debug.Log($"<color=yellow>✓ Spawned OTHER player {playerId} at {position}</color>");
    }

    void UpdateOtherPlayers()
    {
        foreach (var kvp in otherStates)
        {
            int playerId = kvp.Key;
            PlayerState state = kvp.Value;

            if (!otherPlayers.ContainsKey(playerId))
            {
                SpawnOtherPlayer(playerId, state.GetPosition(), state.rotY);
            }

            GameObject otherPlayer = otherPlayers[playerId];
            if (otherPlayer != null && otherPlayer.activeSelf)
            {
                Vector3 targetPos = state.GetPosition();
                Quaternion targetRot = Quaternion.Euler(0, state.rotY, 0);

                otherPlayer.transform.position = Vector3.Lerp(
                    otherPlayer.transform.position,
                    targetPos,
                    Time.deltaTime * 10f
                );

                otherPlayer.transform.rotation = Quaternion.Lerp(
                    otherPlayer.transform.rotation,
                    targetRot,
                    Time.deltaTime * 10f
                );
            }
        }
    }

    void UpdateCamera()
    {
        if (mainCamera != null && myPlayerObject != null)
        {
            Quaternion rotation = myPlayerObject.transform.rotation;
            Vector3 targetPos = myPlayerObject.transform.position + rotation * cameraOffset;

            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position, 
                targetPos, 
                Time.deltaTime * 5f
            );
            mainCamera.transform.LookAt(myPlayerObject.transform.position + Vector3.up);
        }
    }

    void UpdateInfoText()
    {
        if (infoText == null || myPlayerObject == null) return;

        string status = otherPlayers.Count > 0 ? $"CONNECTED ({otherPlayers.Count} otros)" : "SOLO";
        Vector3 myPos = myPlayerObject.transform.position;
        
        string healthInfo = myHealth != null ? $"HP: {myHealth.GetHealthPercentage() * 100:F0}%" : "";
        
        string otherInfo = "";
        foreach (var kvp in otherStates)
        {
            otherInfo += $"\nPlayer {kvp.Key}: ({kvp.Value.posX:F1}, {kvp.Value.posY:F1}, {kvp.Value.posZ:F1})";
        }

        string pingInfo = rttSmoothedMs >= 0 ? $" | Ping: {rttSmoothedMs:F0} ms" : "";

        infoText.text = $"You are Player {myPlayerId} [{status}] {healthInfo}{pingInfo}\n" +
                       $"My Pos: ({myPos.x:F1}, {myPos.y:F1}, {myPos.z:F1})" +
                       otherInfo + "\n\n" +
                       $"WASD: Move | Mouse: Look | LClick: Shoot\n" +
                       $"Space: Jump | ESC: Exit";
    }
    #endregion

    #region Shooting System
    /// <summary>
    /// Llamado cuando MI jugador dispara e impacta a alguien
    /// </summary>
    public void OnPlayerShot(int shooterId, int targetId, float damage, Vector3 hitPoint)
    {
        // Enviar el disparo al servidor para que lo retransmita
        ShootData shootData = new ShootData(shooterId, targetId, damage, hitPoint, targetId != -1);
        SendShootData(shootData);
    }

    /// <summary>
    /// Llamado cuando un jugador muere
    /// </summary>
    public void OnPlayerDied(int victimId, int killerId)
    {
        AddKillFeedMessage($"Player {killerId} eliminó a Player {victimId}");
        
        // Enviar notificación de muerte al servidor
        DeathData deathData = new DeathData(victimId, killerId);
        SendDeathData(deathData);
    }

    /// <summary>
    /// Solicitar respawn al servidor
    /// </summary>
    public void RequestRespawn(int playerId)
    {
        Vector3 spawnPos = GetSpawnPosition(playerId);
        
        if (myPlayerObject != null)
        {
            myPlayerObject.transform.position = spawnPos;
            myPlayerObject.transform.rotation = Quaternion.identity;
        }
        
        Debug.Log($"[GameController] Player {playerId} respawned at {spawnPos}");
    }

    void AddKillFeedMessage(string message)
    {
        killFeedMessages.Add(message);
        
        if (killFeedMessages.Count > MAX_KILL_FEED_LINES)
        {
            killFeedMessages.RemoveAt(0);
        }
        
        if (killFeedText != null)
        {
            killFeedText.text = string.Join("\n", killFeedMessages);
        }
    }
    #endregion

    #region Networking
    void SendMyData()
    {
        if (udpSocket == null || serverEndPoint == null || myState == null) return;

        try
        {
            if (myPlayerObject != null)
            {
                myState.UpdateFromTransform(myPlayerObject.transform);
            }

            string json = myState.ToJson();
            string message = "PLAYER_DATA:" + json;

            byte[] data = Encoding.UTF8.GetBytes(message);
            udpSocket.SendTo(data, serverEndPoint);
            packetsSent++;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send failed: {ex.Message}");
        }
    }

    void SendShootData(ShootData shootData)
    {
        if (udpSocket == null || serverEndPoint == null) return;

        try
        {
            string json = shootData.ToJson();
            string message = "SHOOT_DATA:" + json;

            byte[] data = Encoding.UTF8.GetBytes(message);
            udpSocket.SendTo(data, serverEndPoint);
            
            Debug.Log($"<color=orange>[Net] Enviado disparo: Shooter {shootData.shooterId} -> Target {shootData.targetId}</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send shoot data failed: {ex.Message}");
        }
    }

    void SendDeathData(DeathData deathData)
    {
        if (udpSocket == null || serverEndPoint == null) return;

        try
        {
            string json = deathData.ToJson();
            string message = "DEATH_DATA:" + json;

            byte[] data = Encoding.UTF8.GetBytes(message);
            udpSocket.SendTo(data, serverEndPoint);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send death data failed: {ex.Message}");
        }
    }

    void ReceiveData()
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
                        ProcessPlayerData(msg);
                    }
                    else if (msg.StartsWith("SHOOT_DATA:"))
                    {
                        ProcessShootData(msg);
                    }
                    else if (msg.StartsWith("DEATH_DATA:"))
                    {
                        ProcessDeathData(msg);
                    }
                    else if (msg.StartsWith("PONG:"))
                    {
                        ProcessPong(msg);
                    }
                    else if (msg == "SERVER_CLOSED")
                    {
                        Debug.Log("Server closed");
                        ReturnToMenu();
                    }
                }
            }
        }
        catch (SocketException) { }
    }

    void ProcessPlayerData(string msg)
    {
        packetsReceived++;
        
        string json = msg.Substring("PLAYER_DATA:".Length);
        PlayerState receivedState = PlayerState.FromJson(json);

        if (receivedState.playerId == myPlayerId)
        {
            return;
        }

        if (!otherStates.ContainsKey(receivedState.playerId))
        {
            Debug.Log($"<color=green>✓ First data from Player {receivedState.playerId}!</color>");
        }

        otherStates[receivedState.playerId] = receivedState;
    }

    // --- Ping / Pong ---
    void SendPing()
    {
        if (udpSocket == null || serverEndPoint == null) return;

        try
        {
            lastPingTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string message = "PING:" + lastPingTimestampMs;
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpSocket.SendTo(data, serverEndPoint);
            Debug.Log($"<color=cyan>[Ping] Enviado PING con timestamp {lastPingTimestampMs}</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send ping failed: {ex.Message}");
        }
    }

    void ProcessPong(string msg)
    {
        // Espera formato: "PONG:<timestampMs>"
        string payload = msg.Substring("PONG:".Length);
        if (long.TryParse(payload, out long sentMs))
        {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            float measured = (float)(nowMs - sentMs);
            if (measured >= 0 && measured < 10000f)
            {
                rttMs = measured;
                rttSmoothedMs = rttSmoothedMs < 0 ? rttMs : Mathf.Lerp(rttSmoothedMs, rttMs, 0.25f);
                Debug.Log($"<color=green>[Ping] Recibido PONG! RTT: {measured:F0} ms | Suavizado: {rttSmoothedMs:F0} ms</color>");
            }
        }
        else
        {
            Debug.LogWarning($"[Ping] PONG mal formateado: {msg}");
        }
    }

    void ProcessShootData(string msg)
    {
        string json = msg.Substring("SHOOT_DATA:".Length);
        ShootData shootData = ShootData.FromJson(json);

        Debug.Log($"<color=cyan>[Net] Recibido disparo: Shooter {shootData.shooterId} -> Target {shootData.targetId}</color>");

        // Si YO soy el objetivo, aplicar daño
        if (shootData.targetId == myPlayerId && myHealth != null)
        {
            myHealth.TakeDamage(shootData.damage, shootData.shooterId);
        }

        // Si el disparo viene de OTRO jugador, reproducir efectos visuales
        if (shootData.shooterId != myPlayerId && otherPlayers.ContainsKey(shootData.shooterId))
        {
            GameObject shooterObject = otherPlayers[shootData.shooterId];
            WeaponController weapon = shooterObject.GetComponent<WeaponController>();
            
            if (weapon != null)
            {
                weapon.PlayShootEffect(shootData.GetHitPoint(), shootData.didHit);
            }
        }
    }

    void ProcessDeathData(string msg)
    {
        string json = msg.Substring("DEATH_DATA:".Length);
        DeathData deathData = DeathData.FromJson(json);

        AddKillFeedMessage($"Player {deathData.killerId} eliminó a Player {deathData.victimId}");

        // Si otro jugador murió, desactivar su GameObject
        if (deathData.victimId != myPlayerId && otherPlayers.ContainsKey(deathData.victimId))
        {
            GameObject victim = otherPlayers[deathData.victimId];
            if (victim != null)
            {
                victim.SetActive(false);
                // Se reactivará cuando recibamos su PlayerState después del respawn
            }
        }
    }
    #endregion

    #region Public API
    public void UpdateMyState(Vector3 position, float rotationY)
    {
        if (myState != null)
        {
            myState.posX = position.x;
            myState.posY = position.y;
            myState.posZ = position.z;
            myState.rotY = rotationY;
        }
    }

    public PlayerState GetMyState()
    {
        return myState;
    }

    public GameObject GetMyPlayerObject()
    {
        return myPlayerObject;
    }
    #endregion

    #region Scene Management
    public void ReturnToMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (NetworkManager.Instance != null)
        {
            Destroy(NetworkManager.Instance.gameObject);
        }
        SceneManager.LoadScene("MainMenu");
    }
    #endregion
}