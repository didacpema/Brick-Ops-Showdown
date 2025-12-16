using UnityEngine;
using BrickOps.Networking;
using BrickOps.Core;

public class RotationAnimation : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 75f;
    [SerializeField] private int objectId = 0; 
    
    // OPTIMITZACIÓ: Baixem el sendRate a 5 (suficient per rotacions visuals)
    [SerializeField] private float sendRate = 5f; 

    private float nextSendTime = 0f;
    private bool isServer = false;

    // Variables per a la interpolació al client
    private Quaternion targetRotation;
    private float interpolationSpeed = 10f;

    private void Start()
    {
        if (NetworkManager.Instance != null)
        {
            isServer = NetworkManager.Instance.isServer;
        }
        targetRotation = transform.rotation;
    }

    private void Update()
    {  
        if (isServer || NetworkManager.Instance == null)
        {
            // Lògica del Servidor: Rotar i enviar
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

            if (isServer && Time.time >= nextSendTime)
            {
                SendTransformUpdate();
                nextSendTime = Time.time + (1f / sendRate);
            }
        }
        else
        {
            // Lògica del Client: Interpolació SUAU
            // En lloc de teletransportar la rotació, la suavitzem
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * interpolationSpeed);
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

    public void ApplyTransform(ObjectTransformData data)
    {
        if (data.objectId == objectId)
        {
            // En lloc d'aplicar directament, guardem el target per interpolar al Update
            targetRotation = Quaternion.Euler(data.GetRotation());
            
            // Si la desincronització és massa gran (més de 45 graus), forcem el salt
            if (Quaternion.Angle(transform.rotation, targetRotation) > 45f)
            {
                transform.rotation = targetRotation;
            }
        }
    }
}