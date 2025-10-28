using UnityEngine;

public class InputManager : MonoBehaviour
{
    #region Private Variables
    private GameController gameController;
    private Rigidbody rb;
    
    [Header("Mouse Settings")]
    public float mouseSensitivity = 2f;
    private float mouseX = 0f;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        gameController = GetComponent<GameController>();
        if (gameController == null)
        {
            Debug.LogError("GameController not found on this GameObject!");
        }
        
        // Obtener el Rigidbody del player object
        if (gameController != null && gameController.myPlayerObject != null)
        {
            rb = gameController.myPlayerObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("Rigidbody not found on myPlayerObject! Adding one...");
                rb = gameController.myPlayerObject.AddComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }
        
        // Bloquear y ocultar el cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    #endregion

    #region Input Handling
    public void HandleInput()
    {
        if (gameController == null || gameController.myPlayerObject == null || rb == null) return;

        HandleMovement();
        HandleRotation();
        HandleJump();
        UpdateMyData();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Desbloquear cursor al salir
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            gameController.ReturnToMenu();
        }
    }

    void HandleMovement()
    {
        Vector3 movement = Vector3.zero;
        Transform playerTransform = gameController.myPlayerObject.transform;
        
        if (Input.GetKey(KeyCode.W)) movement += playerTransform.forward;
        if (Input.GetKey(KeyCode.S)) movement -= playerTransform.forward;
        if (Input.GetKey(KeyCode.A)) movement -= playerTransform.right;
        if (Input.GetKey(KeyCode.D)) movement += playerTransform.right;

        if (movement != Vector3.zero)
        {
            // Usar velocidad del Rigidbody en lugar de transform.position
            Vector3 velocity = movement.normalized * gameController.moveSpeed;
            velocity.y = rb.linearVelocity.y; // Mantener la velocidad vertical (gravedad/salto)
            rb.linearVelocity = velocity;
        }
        else
        {
            // Detener movimiento horizontal pero mantener velocidad vertical
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    void HandleRotation()
    {
        Transform playerTransform = gameController.myPlayerObject.transform;
        
        // Rotación con mouse (eje X del mouse rota en Y del jugador)
        mouseX += Input.GetAxis("Mouse X") * mouseSensitivity;
        playerTransform.rotation = Quaternion.Euler(0, mouseX, 0);
        
        // Rotación con teclado (Q/E) - opcional, puedes comentar si solo quieres mouse
        if (Input.GetKey(KeyCode.Q))
        {
            mouseX -= gameController.rotateSpeed * Time.deltaTime;
            playerTransform.rotation = Quaternion.Euler(0, mouseX, 0);
        }
        if (Input.GetKey(KeyCode.E))
        {
            mouseX += gameController.rotateSpeed * Time.deltaTime;
            playerTransform.rotation = Quaternion.Euler(0, mouseX, 0);
        }
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);
        }
    }

    bool IsGrounded()
    {
        // Raycast para detectar si está en el suelo
        return Physics.Raycast(gameController.myPlayerObject.transform.position, Vector3.down, 1.1f);
    }

    void UpdateMyData()
    {
        Vector3 pos = gameController.myPlayerObject.transform.position;
        gameController.myData.posX = pos.x;
        gameController.myData.posY = pos.y;
        gameController.myData.posZ = pos.z;
        gameController.myData.rotY = gameController.myPlayerObject.transform.eulerAngles.y;
    }
    #endregion
}
