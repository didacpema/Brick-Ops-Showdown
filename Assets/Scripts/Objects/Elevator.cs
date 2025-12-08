using System.Collections;
using UnityEngine;
using BrickOps.Networking;
using BrickOps.Core;

public class Elevator : MonoBehaviour
{
    [SerializeField] private float moveDistance = 5.5f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waitTime = 1.5f;
    [SerializeField] private int objectId = 0; 
    [SerializeField] private float sendRate = 10f; 

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool movingUp = true;
    private bool isServer = false;
    private float nextSendTime = 0f;

    void Start()
    {
        if (NetworkManager.Instance != null)
        {
            isServer = NetworkManager.Instance.isServer;
        }

        startPosition = transform.position;
        targetPosition = startPosition + Vector3.up * moveDistance;

        if (isServer || NetworkManager.Instance == null)
        {
            StartCoroutine(ElevatorCycle());
        }
    }

    void Update()
    {
        if (isServer && Time.time >= nextSendTime)
        {
            SendTransformUpdate();
            nextSendTime = Time.time + (1f / sendRate);
        }
    }

    IEnumerator ElevatorCycle()
    {
        while (true)
        {
            Vector3 destination = movingUp ? targetPosition : startPosition;
            
            while (Vector3.Distance(transform.position, destination) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = destination;

            yield return new WaitForSeconds(waitTime);

            movingUp = !movingUp;
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
