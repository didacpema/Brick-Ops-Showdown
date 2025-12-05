using System.Collections;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    [SerializeField] private float moveDistance = 5.5f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waitTime = 1.5f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool movingUp = true;

    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + Vector3.up * moveDistance;
        StartCoroutine(ElevatorCycle());
    }

    IEnumerator ElevatorCycle()
    {
        while (true)
        {
            // Mover el elevador
            Vector3 destination = movingUp ? targetPosition : startPosition;
            
            while (Vector3.Distance(transform.position, destination) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = destination;

            // Esperar 1.5 segundos
            yield return new WaitForSeconds(waitTime);

            // Cambiar dirección
            movingUp = !movingUp;
        }
    }
}
