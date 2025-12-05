using UnityEngine;

public class RotationAnimation : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 75f;

    private void Update()
    {
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}
