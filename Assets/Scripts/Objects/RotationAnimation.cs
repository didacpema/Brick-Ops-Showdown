using UnityEngine;
using BrickOps.Networking;
using BrickOps.Core;

public class RotationAnimation : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 75f;
    [SerializeField] private int objectId = 0; 
    [SerializeField] private float sendRate = 5f; 

    private float nextSendTime = 0f;
    private bool isServer = false;

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
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

            if (isServer && Time.time >= nextSendTime)
            {
                SendTransformUpdate();
                nextSendTime = Time.time + (1f / sendRate);
            }
        }
        else
        {
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
            targetRotation = Quaternion.Euler(data.GetRotation());
            
            if (Quaternion.Angle(transform.rotation, targetRotation) > 45f)
            {
                transform.rotation = targetRotation;
            }
        }
    }
}