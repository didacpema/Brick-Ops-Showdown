using UnityEngine;

public class InputManager : MonoBehaviour
{
    #region Private Variables
    private GameController gameController;
    private GameObject playerObject;
    private Rigidbody rb;
    private WeaponController weaponController;
    
    [Header("Mouse Settings")]
    public float mouseSensitivity = 2f;
    private float mouseX = 0f;
    
    private bool initialized = false;
    #endregion

    #region Initialization
    /// <summary>
    /// Inicializa el InputManager con referencias del GameController
    /// </summary>
    public void Initialize(GameController controller, GameObject player)
    {
        gameController = controller;
        playerObject = player;
        
        if (playerObject == null)
        {
            Debug.LogError("InputManager: playerObject is null!");
            return;
        }

        // Obtener o añadir Rigidbody
        rb = playerObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("InputManager: Rigidbody not found! Adding one...");
            rb = playerObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        // Obtener WeaponController
        weaponController = playerObject.GetComponent<WeaponController>();
        if (weaponController == null)
        {
            Debug.LogWarning("InputManager: WeaponController not found on player!");
        }
        else
        {
            // Inicializar el arma con la cámara principal
            Camera cam = (gameController != null && gameController.mainCamera != null) ? gameController.mainCamera : Camera.main;
            if (cam != null)
            {
                weaponController.InitializeForLocalPlayer(cam);
            }
        }

        initialized = true;
        Debug.Log("✓ InputManager initialized (with shooting)");
    }
    #endregion

    #region Input Handling
    public void HandleInput()
    {
        if (!initialized || playerObject == null || rb == null) return;

        HandleMovement();
        HandleRotation();
        HandleJump();
        HandleShooting();
    }

    void HandleMovement()
    {
        Vector3 movement = Vector3.zero;
        Transform playerTransform = playerObject.transform;
        
        // Input WASD
        if (Input.GetKey(KeyCode.W)) movement += playerTransform.forward;
        if (Input.GetKey(KeyCode.S)) movement -= playerTransform.forward;
        if (Input.GetKey(KeyCode.A)) movement -= playerTransform.right;
        if (Input.GetKey(KeyCode.D)) movement += playerTransform.right;

        if (movement != Vector3.zero)
        {
            // Aplicar velocidad horizontal, mantener velocidad vertical
            Vector3 velocity = movement.normalized * gameController.moveSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }
        else
        {
            // Detener movimiento horizontal
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    void HandleRotation()
    {
        Transform playerTransform = playerObject.transform;
        
        // Rotación con mouse
        mouseX += Input.GetAxis("Mouse X") * mouseSensitivity;
        playerTransform.rotation = Quaternion.Euler(0, mouseX, 0);
        
        // Rotación con teclado Q/E (opcional)
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

    void HandleShooting()
    {
        if (weaponController == null) return;

        // Disparo con clic izquierdo o botón izquierdo del mouse
        if (Input.GetMouseButton(0)) // Mantener presionado para automático
        {
            weaponController.TryShoot();
        }
        
        // O usar GetMouseButtonDown para semi-automático
        // if (Input.GetMouseButtonDown(0))
        // {
        //     weaponController.TryShoot();
        // }
    }

    bool IsGrounded()
    {
        // Raycast para detectar suelo
        return Physics.Raycast(playerObject.transform.position, Vector3.down, 1.1f);
    }
    #endregion
}