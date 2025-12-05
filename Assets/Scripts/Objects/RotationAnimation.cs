using UnityEngine;
using BrickOps.Networking;
using BrickOps.Core;

public class RotationAnimation : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 75f;
    [SerializeField] private int objectId = 0; // ID único para este objeto
    [SerializeField] private float sendRate = 10f; // Enviar 10 veces por segundo

    private float nextSendTime = 0f;
    private bool isServer = false;

    private void Start()
    {
        // Verificar si somos el servidor/host
        if (NetworkManager.Instance != null)
        {
            isServer = NetworkManager.Instance.isServer;
        }
    }

    private void Update()
    {
        // Solo el servidor/host actualiza la rotación del objeto
        if (isServer || NetworkManager.Instance == null)
        {
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

            // Sincronizar con clientes
            if (isServer && Time.time >= nextSendTime)
            {
                SendTransformUpdate();
                nextSendTime = Time.time + (1f / sendRate);
            }
        }
    }

    private void SendTransformUpdate()
    {
        if (GameController.Instance == null) return;

        ObjectTransformData data = new ObjectTransformData(
            objectId,
            transform.position,
            transform.eulerAngles
        );

        string message = NetworkProtocol.BuildMessage(NetworkProtocol.OBJECT_TRANSFORM, data);
        GameController.Instance.BroadcastToClients(message, null);
    }

    // Método para aplicar transformación recibida de la red
    public void ApplyTransform(ObjectTransformData data)
    {
        if (data.objectId == objectId)
        {
            transform.position = data.GetPosition();
            transform.eulerAngles = data.GetRotation();
        }
    }
}
