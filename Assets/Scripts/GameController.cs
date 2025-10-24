using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using BrickOps.Networking;

[System.Serializable]
public class PlayerData
{
    public int playerId;
    public float posX;
    public float posY;
    public float posZ;
    public float rotY;

    public PlayerData(int id, Vector3 pos, float rotation)
    {
        playerId = id;
        posX = pos.x;
        posY = pos.y;
        posZ = pos.z;
        rotY = rotation;
    }

    public Vector3 GetPosition()
    {
        return new Vector3(posX, posY, posZ);
    }
}

public class GameController : MonoBehaviour
{
    [Header("Player Objects")]
    public GameObject player1Object;
    public GameObject player2Object;

    [Header("Cameras")]
    public Camera camera1;
    public Camera camera2;

    [Header("UI")]
    public TMP_Text infoText;

    [Header("Settings")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 100f;
    public float sendRate = 0.05f;
    public Vector3 cameraOffset = new Vector3(0, 3, -5);

    private Socket udpSocket;
    private EndPoint serverEndPoint;
    private byte[] buffer = new byte[2048];

    private PlayerData myData;
    private PlayerData otherData;
    private GameObject myPlayerObject;
    private GameObject otherPlayerObject;
    private Camera myCamera;

    private int myPlayerId;
    private float nextSendTime = 0f;
    
    // Estadísticas de red para debug
    private int packetsSent = 0;
    private int packetsReceived = 0;
    private int packetsReceivedFromOther = 0;

    void Start()
    {
        Debug.Log("=== GameController Start ===");
        Debug.Log($"GameController Instance: {GetInstanceID()}");
        
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            SceneManager.LoadScene("MainMenu");
            return;
        }

        Debug.Log($"NetworkManager Info: {NetworkManager.Instance.GetDebugInfo()}");
        myPlayerId = NetworkManager.Instance.myPlayerId;
        Debug.Log($"My Player ID: {myPlayerId}");

        if (myPlayerId == -1)
        {
            Debug.LogError("Player ID not assigned!");
            SceneManager.LoadScene("WaitingRoom");
            return;
        }

        // Verificar que player1Object y player2Object están asignados
        if (player1Object == null || player2Object == null)
        {
            Debug.LogError("Player objects not assigned in inspector!");
            return;
        }
        
        Debug.Log($"Player1Object: {player1Object.name} at {player1Object.transform.position}");
        Debug.Log($"Player2Object: {player2Object.name} at {player2Object.transform.position}");

        // Asignar objetos y cámaras según ID
        if (myPlayerId == 1)
        {
            myPlayerObject = player1Object;
            otherPlayerObject = player2Object;
            myCamera = camera1;
            
            camera1.enabled = true;
            camera2.enabled = false;
            
            // Desactivar AudioListener de la cámara no usada
            AudioListener listener2 = camera2.GetComponent<AudioListener>();
            if (listener2 != null) listener2.enabled = false;
            
            AudioListener listener1 = camera1.GetComponent<AudioListener>();
            if (listener1 != null) listener1.enabled = true;

            // CRÍTICO: Crear materiales NUEVOS para evitar compartir el mismo material
            Renderer myRenderer = player1Object.GetComponent<Renderer>();
            Renderer otherRenderer = player2Object.GetComponent<Renderer>();
            
            if (myRenderer != null && myRenderer.material != null)
            {
                // Crear nueva instancia del material
                myRenderer.material = new Material(myRenderer.material);
                myRenderer.material.color = Color.blue;
                Debug.Log("Player 1 (ME) set to BLUE with new material instance");
            }
            
            if (otherRenderer != null && otherRenderer.material != null)
            {
                // Crear nueva instancia del material
                otherRenderer.material = new Material(otherRenderer.material);
                otherRenderer.material.color = Color.red;
                Debug.Log("Player 2 (OTHER) set to RED with new material instance");
            }
        }
        else if (myPlayerId == 2)
        {
            myPlayerObject = player2Object;
            otherPlayerObject = player1Object;
            myCamera = camera2;
            
            camera1.enabled = false;
            camera2.enabled = true;
            
            // Desactivar AudioListener de la cámara no usada
            AudioListener listener1 = camera1.GetComponent<AudioListener>();
            if (listener1 != null) listener1.enabled = false;
            
            AudioListener listener2 = camera2.GetComponent<AudioListener>();
            if (listener2 != null) listener2.enabled = true;

            // CRÍTICO: Crear materiales NUEVOS para evitar compartir el mismo material
            Renderer myRenderer = player2Object.GetComponent<Renderer>();
            Renderer otherRenderer = player1Object.GetComponent<Renderer>();
            
            if (myRenderer != null && myRenderer.material != null)
            {
                // Crear nueva instancia del material
                myRenderer.material = new Material(myRenderer.material);
                myRenderer.material.color = Color.blue;
                Debug.Log("Player 2 (ME) set to BLUE with new material instance");
            }
            
            if (otherRenderer != null && otherRenderer.material != null)
            {
                // Crear nueva instancia del material
                otherRenderer.material = new Material(otherRenderer.material);
                otherRenderer.material.color = Color.red;
                Debug.Log("Player 1 (OTHER) set to RED with new material instance");
            }
        }

        // Configurar UI (puede ser null si no está asignado)
        if (infoText != null)
        {
            UpdateInfoText();
        }

        // Configurar socket
        udpSocket = NetworkManager.Instance.udpSocket;
        serverEndPoint = NetworkManager.Instance.serverEndPoint;

        if (udpSocket == null || serverEndPoint == null)
        {
            Debug.LogError("UDP Socket or Server EndPoint is null!");
            return;
        }
        
        Debug.Log("UDP Socket and Server EndPoint configured successfully");

        // Inicializar datos
        myData = new PlayerData(myPlayerId, myPlayerObject.transform.position, myPlayerObject.transform.eulerAngles.y);
        
        Debug.Log($"GameController initialized for Player {myPlayerId}");
        Debug.Log($"<color=lime>==========================================");
        Debug.Log($"Player {myPlayerId} is ready to send/receive data!");
        Debug.Log($"My Object: {myPlayerObject.name} (Blue) - Instance ID: {myPlayerObject.GetInstanceID()}");
        Debug.Log($"Other Object: {otherPlayerObject.name} (Red) - Instance ID: {otherPlayerObject.GetInstanceID()}");
        Debug.Log($"My Material ID: {myPlayerObject.GetComponent<Renderer>().material.GetInstanceID()}");
        Debug.Log($"Other Material ID: {otherPlayerObject.GetComponent<Renderer>().material.GetInstanceID()}");
        Debug.Log($"Socket connected: {(udpSocket != null ? "YES" : "NO")}");
        Debug.Log($"==========================================</color>");
    }

    void Update()
    {
        HandleInput();
        ReceiveData();

        // Enviar datos periódicamente
        if (Time.time >= nextSendTime)
        {
            SendMyData();
            nextSendTime = Time.time + sendRate;
        }

        // Actualizar posición del otro jugador (interpolación)
        if (otherData != null && otherPlayerObject != null)
        {
            Vector3 targetPos = otherData.GetPosition();
            Vector3 currentPos = otherPlayerObject.transform.position;
            
            // Interpolar posición
            otherPlayerObject.transform.position = Vector3.Lerp(
                currentPos,
                targetPos,
                Time.deltaTime * 10f
            );

            // Interpolar rotación
            Quaternion targetRot = Quaternion.Euler(0, otherData.rotY, 0);
            otherPlayerObject.transform.rotation = Quaternion.Lerp(
                otherPlayerObject.transform.rotation,
                targetRot,
                Time.deltaTime * 10f
            );
            
            // Debug visual cada 3 segundos
            if (Time.frameCount % 180 == 0)
            {
                float distance = Vector3.Distance(currentPos, targetPos);
                Debug.Log($"[Player {myPlayerId}] OTHER player current pos: {currentPos}, target pos: {targetPos}, distance: {distance:F2}");
            }
        }
        else
        {
            // Si no hay datos del otro jugador, mostrar warning ocasionalmente
            if (Time.frameCount % 300 == 0) // Cada 5 segundos
            {
                if (otherData == null)
                    Debug.LogWarning($"[Player {myPlayerId}] No data received from other player yet!");
                if (otherPlayerObject == null)
                    Debug.LogError($"[Player {myPlayerId}] otherPlayerObject is NULL!");
            }
        }

        // Actualizar cámara
        UpdateCamera();
        
        // Actualizar UI cada frame
        if (infoText != null)
        {
            UpdateInfoText();
        }
        
        // Mostrar estadísticas cada 5 segundos
        if (Time.frameCount % 300 == 0 && Time.frameCount > 0)
        {
            Debug.Log($"<color=cyan>[Player {myPlayerId}] === NETWORK STATS === \n" +
                     $"Packets Sent: {packetsSent} | Packets Received: {packetsReceived} | From Other Player: {packetsReceivedFromOther}\n" +
                     $"Other Player Data: {(otherData != null ? "AVAILABLE" : "NULL")}</color>");
        }
    }
    
    void UpdateInfoText()
    {
        if (infoText == null) return;
        
        string status = otherData != null ? "CONNECTED" : "WAITING...";
        string otherPlayerPos = otherData != null ? 
            $"Other: ({otherData.posX:F1}, {otherData.posY:F1}, {otherData.posZ:F1})" : 
            "Other: No data";
        
        Vector3 myPos = myPlayerObject.transform.position;
        
        infoText.text = $"You are Player {myPlayerId} [{status}]\n" +
                       $"My Pos: ({myPos.x:F1}, {myPos.y:F1}, {myPos.z:F1})\n" +
                       $"{otherPlayerPos}\n\n" +
                       $"WASD: Move | Q/E: Rotate\n" +
                       $"ESC: Exit";
    }

    void HandleInput()
    {
        if (myPlayerObject == null) return;

        // Movimiento
        Vector3 movement = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) movement += myPlayerObject.transform.forward;
        if (Input.GetKey(KeyCode.S)) movement -= myPlayerObject.transform.forward;
        if (Input.GetKey(KeyCode.A)) movement -= myPlayerObject.transform.right;
        if (Input.GetKey(KeyCode.D)) movement += myPlayerObject.transform.right;

        if (movement != Vector3.zero)
        {
            myPlayerObject.transform.position += movement.normalized * moveSpeed * Time.deltaTime;
        }

        // Rotación
        if (Input.GetKey(KeyCode.Q))
        {
            myPlayerObject.transform.Rotate(0, -rotateSpeed * Time.deltaTime, 0);
        }
        if (Input.GetKey(KeyCode.E))
        {
            myPlayerObject.transform.Rotate(0, rotateSpeed * Time.deltaTime, 0);
        }

        // Actualizar datos
        Vector3 pos = myPlayerObject.transform.position;
        myData.posX = pos.x;
        myData.posY = pos.y;
        myData.posZ = pos.z;
        myData.rotY = myPlayerObject.transform.eulerAngles.y;

        // Salir
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMenu();
        }
    }

    void UpdateCamera()
    {
        if (myCamera != null && myPlayerObject != null)
        {
            // Calcular posición de cámara basada en la rotación del jugador
            Quaternion rotation = myPlayerObject.transform.rotation;
            Vector3 targetPos = myPlayerObject.transform.position + rotation * cameraOffset;

            myCamera.transform.position = Vector3.Lerp(myCamera.transform.position, targetPos, Time.deltaTime * 5f);
            myCamera.transform.LookAt(myPlayerObject.transform.position + Vector3.up);
        }
    }

    void SendMyData()
    {
        if (udpSocket == null || serverEndPoint == null) return;

        try
        {
            // myData ya está actualizado en HandleInput(), solo enviamos
            // Serialización con JsonUtility
            string json = JsonUtility.ToJson(myData);
            string message = "PLAYER_DATA:" + json;

            byte[] data = Encoding.UTF8.GetBytes(message);
            udpSocket.SendTo(data, serverEndPoint);
            packetsSent++;
            
            // Debug cada 2 segundos aprox
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"[Player {myPlayerId}] Sending MY position: ({myData.posX:F2}, {myData.posY:F2}, {myData.posZ:F2}) | Packets sent: {packetsSent}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Player {myPlayerId}] Send failed: {ex.Message}");
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
                        packetsReceived++;
                        
                        string json = msg.Substring("PLAYER_DATA:".Length);

                        // Deserialización
                        PlayerData receivedData = JsonUtility.FromJson<PlayerData>(json);

                        // Solo actualizar si es del otro jugador
                        if (receivedData.playerId != myPlayerId)
                        {
                            packetsReceivedFromOther++;
                            
                            // Primera vez que recibimos datos
                            if (otherData == null)
                            {
                                Debug.Log($"<color=green>[Player {myPlayerId}] ✓ FIRST DATA received from Player {receivedData.playerId}!</color>");
                            }
                            
                            otherData = receivedData;
                            
                            // Debug periódico cada 3 segundos
                            if (Time.frameCount % 180 == 0)
                            {
                                Debug.Log($"[Player {myPlayerId}] Received from Player {receivedData.playerId}: Pos({receivedData.posX:F2}, {receivedData.posY:F2}, {receivedData.posZ:F2}) | Total received: {packetsReceivedFromOther}");
                            }
                        }
                        else
                        {
                            // Esto NO debería pasar - el servidor no debería enviar mis propios datos de vuelta
                            Debug.LogWarning($"<color=yellow>[Player {myPlayerId}] ⚠ Received my OWN data back! Server should exclude sender. (Packet #{packetsReceived})</color>");
                        }
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

    void ReturnToMenu()
    {
        if (NetworkManager.Instance != null)
        {
            Destroy(NetworkManager.Instance.gameObject);
        }
        SceneManager.LoadScene("MainMenu");
    }

    void OnApplicationQuit()
    {
        udpSocket?.Close();
    }
}