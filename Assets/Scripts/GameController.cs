using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using BrickOps.Networking;

#region Data Structures
[Serializable]
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
#endregion

public class GameController : MonoBehaviour
{
    //game controller instance
    public static GameController instance;

    #region Inspector Variables
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
    #endregion

    #region Private Variables
    private Socket udpSocket;
    private EndPoint serverEndPoint;
    private byte[] buffer = new byte[2048];

    public PlayerData myData;
    private PlayerData otherData;
    public GameObject myPlayerObject;
    private GameObject otherPlayerObject;
    private Camera myCamera;
    private InputManager inputManager;
    private int myPlayerId;
    private float nextSendTime = 0f;
    
    private int packetsSent = 0;
    private int packetsReceived = 0;
    private int packetsReceivedFromOther = 0;
    #endregion

    #region Unity Lifecycle
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

        if (player1Object == null || player2Object == null)
        {
            Debug.LogError("Player objects not assigned in inspector!");
            return;
        }
        
        Debug.Log($"Player1Object: {player1Object.name} at {player1Object.transform.position}");
        Debug.Log($"Player2Object: {player2Object.name} at {player2Object.transform.position}");

        SetupPlayerObjects();
        SetupUI();
        SetupNetworking();
        
        myData = new PlayerData(myPlayerId, myPlayerObject.transform.position, myPlayerObject.transform.eulerAngles.y);
        inputManager = gameObject.AddComponent<InputManager>();
        
        LogInitializationComplete();
    }

    void Update()
    {
        inputManager.HandleInput();
        ReceiveData();

        if (Time.time >= nextSendTime)
        {
            SendMyData();
            nextSendTime = Time.time + sendRate;
        }

        UpdateOtherPlayer();
        UpdateCamera();

        if (infoText != null)
        {
            UpdateInfoText();
        }

        LogNetworkStats();
    }

    void OnApplicationQuit()
    {
        udpSocket?.Close();
    }
    #endregion

    #region Initialization
    void SetupPlayerObjects()
    {
        if (myPlayerId == 1)
        {
            myPlayerObject = player1Object;
            otherPlayerObject = player2Object;
            myCamera = camera1;
            
            camera1.enabled = true;
            camera2.enabled = false;
            
            AudioListener listener2 = camera2.GetComponent<AudioListener>();
            if (listener2 != null) listener2.enabled = false;
            
            AudioListener listener1 = camera1.GetComponent<AudioListener>();
            if (listener1 != null) listener1.enabled = true;

            SetPlayerColors(player1Object, player2Object);
        }
        else if (myPlayerId == 2)
        {
            myPlayerObject = player2Object;
            otherPlayerObject = player1Object;
            myCamera = camera2;
            
            camera1.enabled = false;
            camera2.enabled = true;
            
            AudioListener listener1 = camera1.GetComponent<AudioListener>();
            if (listener1 != null) listener1.enabled = false;
            
            AudioListener listener2 = camera2.GetComponent<AudioListener>();
            if (listener2 != null) listener2.enabled = true;

            SetPlayerColors(player2Object, player1Object);
        }
    }

    void SetPlayerColors(GameObject myObject, GameObject otherObject)
    {
        Renderer myRenderer = myObject.GetComponent<Renderer>();
        Renderer otherRenderer = otherObject.GetComponent<Renderer>();
        
        if (myRenderer != null && myRenderer.material != null)
        {
            myRenderer.material = new Material(myRenderer.material);
            myRenderer.material.color = Color.blue;
            Debug.Log($"Player {myPlayerId} (ME) set to BLUE with new material instance");
        }
        
        if (otherRenderer != null && otherRenderer.material != null)
        {
            otherRenderer.material = new Material(otherRenderer.material);
            otherRenderer.material.color = Color.red;
            Debug.Log($"Other Player set to RED with new material instance");
        }
    }

    void SetupUI()
    {
        if (infoText != null)
        {
            UpdateInfoText();
        }
    }

    void SetupNetworking()
    {
        udpSocket = NetworkManager.Instance.udpSocket;
        serverEndPoint = NetworkManager.Instance.serverEndPoint;

        if (udpSocket == null || serverEndPoint == null)
        {
            Debug.LogError("UDP Socket or Server EndPoint is null!");
            return;
        }
        
        Debug.Log("UDP Socket and Server EndPoint configured successfully");
    }

    void LogInitializationComplete()
    {
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
    #endregion

    

    #region Player Updates
    void UpdateOtherPlayer()
    {
        if (otherData != null && otherPlayerObject != null)
        {
            Vector3 targetPos = otherData.GetPosition();
            Vector3 currentPos = otherPlayerObject.transform.position;
            
            otherPlayerObject.transform.position = Vector3.Lerp(
                currentPos,
                targetPos,
                Time.deltaTime * 10f
            );

            Quaternion targetRot = Quaternion.Euler(0, otherData.rotY, 0);
            otherPlayerObject.transform.rotation = Quaternion.Lerp(
                otherPlayerObject.transform.rotation,
                targetRot,
                Time.deltaTime * 10f
            );
            
            if (Time.frameCount % 180 == 0)
            {
                float distance = Vector3.Distance(currentPos, targetPos);
                Debug.Log($"[Player {myPlayerId}] OTHER player current pos: {currentPos}, target pos: {targetPos}, distance: {distance:F2}");
            }
        }
        else
        {
            if (Time.frameCount % 300 == 0)
            {
                if (otherData == null)
                    Debug.LogWarning($"[Player {myPlayerId}] No data received from other player yet!");
                if (otherPlayerObject == null)
                    Debug.LogError($"[Player {myPlayerId}] otherPlayerObject is NULL!");
            }
        }
    }

    void UpdateCamera()
    {
        if (myCamera != null && myPlayerObject != null)
        {
            Quaternion rotation = myPlayerObject.transform.rotation;
            Vector3 targetPos = myPlayerObject.transform.position + rotation * cameraOffset;

            myCamera.transform.position = Vector3.Lerp(myCamera.transform.position, targetPos, Time.deltaTime * 5f);
            myCamera.transform.LookAt(myPlayerObject.transform.position + Vector3.up);
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
    #endregion

    #region Networking
    void SendMyData()
    {
        if (udpSocket == null || serverEndPoint == null) return;

        try
        {
            string json = JsonUtility.ToJson(myData);
            string message = "PLAYER_DATA:" + json;

            byte[] data = Encoding.UTF8.GetBytes(message);
            udpSocket.SendTo(data, serverEndPoint);
            packetsSent++;

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
                        ProcessPlayerData(msg);
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
        PlayerData receivedData = JsonUtility.FromJson<PlayerData>(json);

        if (receivedData.playerId != myPlayerId)
        {
            packetsReceivedFromOther++;
            
            if (otherData == null)
            {
                Debug.Log($"<color=green>[Player {myPlayerId}] ✓ FIRST DATA received from Player {receivedData.playerId}!</color>");
            }
            
            otherData = receivedData;
            
            if (Time.frameCount % 180 == 0)
            {
                Debug.Log($"[Player {myPlayerId}] Received from Player {receivedData.playerId}: Pos({receivedData.posX:F2}, {receivedData.posY:F2}, {receivedData.posZ:F2}) | Total received: {packetsReceivedFromOther}");
            }
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[Player {myPlayerId}] ⚠ Received my OWN data back! Server should exclude sender. (Packet #{packetsReceived})</color>");
        }
    }

    void LogNetworkStats()
    {
        if (Time.frameCount % 300 == 0 && Time.frameCount > 0)
        {
            Debug.Log($"<color=blue>[Player {myPlayerId}] === NETWORK STATS === \n" +
                     $"Packets Sent: {packetsSent} | Packets Received: {packetsReceived} | From Other Player: {packetsReceivedFromOther}\n" +
                     $"Other Player Data: {(otherData != null ? "AVAILABLE" : "NULL")}</color>");
        }
    }
    #endregion

    #region Scene Management
    public void ReturnToMenu()
    {
        if (NetworkManager.Instance != null)
        {
            Destroy(NetworkManager.Instance.gameObject);
        }
        SceneManager.LoadScene("MainMenu");
    }
    #endregion
}