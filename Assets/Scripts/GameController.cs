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

    void Start()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            SceneManager.LoadScene("MainMenu");
            return;
        }

        myPlayerId = NetworkManager.Instance.myPlayerId;

        if (myPlayerId == -1)
        {
            Debug.LogError("Player ID not assigned!");
            SceneManager.LoadScene("WaitingRoom");
            return;
        }

        // Asignar objetos y cámaras según ID
        if (myPlayerId == 1)
        {
            myPlayerObject = player1Object;
            otherPlayerObject = player2Object;
            myCamera = camera1;
            
            camera1.enabled = true;
            camera2.enabled = false;

            player1Object.GetComponent<Renderer>().material.color = Color.blue;
            player2Object.GetComponent<Renderer>().material.color = Color.red;
        }
        else if (myPlayerId == 2)
        {
            myPlayerObject = player2Object;
            otherPlayerObject = player1Object;
            myCamera = camera2;
            
            camera1.enabled = false;
            camera2.enabled = true;

            player2Object.GetComponent<Renderer>().material.color = Color.blue;
            player1Object.GetComponent<Renderer>().material.color = Color.red;
        }

        infoText.text = $"You are Player {myPlayerId}\nWASD: Move | Q/E: Rotate\nESC: Exit";

        // Configurar socket
        udpSocket = NetworkManager.Instance.udpSocket;
        serverEndPoint = NetworkManager.Instance.serverEndPoint;

        // Inicializar datos
        myData = new PlayerData(myPlayerId, myPlayerObject.transform.position, myPlayerObject.transform.eulerAngles.y);
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
        if (otherData != null)
        {
            otherPlayerObject.transform.position = Vector3.Lerp(
                otherPlayerObject.transform.position,
                otherData.GetPosition(),
                Time.deltaTime * 10f
            );

            Quaternion targetRot = Quaternion.Euler(0, otherData.rotY, 0);
            otherPlayerObject.transform.rotation = Quaternion.Lerp(
                otherPlayerObject.transform.rotation,
                targetRot,
                Time.deltaTime * 10f
            );
        }

        // Actualizar cámara
        UpdateCamera();
    }

    void HandleInput()
    {
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
            // SERIALIZACIÓN con JsonUtility
            string json = JsonUtility.ToJson(myData);
            string message = "PLAYER_DATA:" + json;

            byte[] data = Encoding.UTF8.GetBytes(message);
            udpSocket.SendTo(data, serverEndPoint);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send failed: {ex.Message}");
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
                        string json = msg.Substring("PLAYER_DATA:".Length);

                        // DESERIALIZACIÓN
                        PlayerData receivedData = JsonUtility.FromJson<PlayerData>(json);

                        // Solo actualizar si es del otro jugador
                        if (receivedData.playerId != myPlayerId)
                        {
                            otherData = receivedData;
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