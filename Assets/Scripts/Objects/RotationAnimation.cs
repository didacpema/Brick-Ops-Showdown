using UnityEngine;
using BrickOps.Networking;
using BrickOps.Core;

public class RotationAnimation : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 75f;
    [SerializeField] private int objectId = 0; 
    [SerializeField] private float sendRate = 10f; 

    private float nextSendTime = 0f;
    private bool isServer = false;

    private void Start()
    {
        if (NetworkManager.Instance != null)
        {
            isServer = NetworkManager.Instance.isServer;
        }
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
            transform.position = data.GetPosition();
            transform.eulerAngles = data.GetRotation();
        }
    }
}
