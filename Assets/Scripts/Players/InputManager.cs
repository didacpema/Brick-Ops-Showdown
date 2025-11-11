using UnityEngine;
using BrickOps.Core;

/// <summary>
/// Gestiona el input del jugador local
/// Independiente del GameController para mejor modularidad
/// </summary>
public class InputManager : MonoBehaviour
{
    #region Inspector Variables
    [Header("Movement Settings")]
    [Tooltip("Velocidad de movimiento")]
    public float moveSpeed = 5f;
    
    [Tooltip("Velocidad de rotación con teclado")]
    public float keyboardRotateSpeed = 100f;
    
    [Tooltip("Fuerza del salto")]
    public float jumpForce = 5f;

    [Header("Mouse Settings")]
    [Tooltip("Sensibilidad del mouse")]
    public float mouseSensitivity = 2f;
    #endregion

    #region Private Variables
    private GameObject playerObject;
    private Rigidbody rb;
    private WeaponController weaponController;
    private float mouseX = 0f;
    private bool isInitialized = false;
    private bool isGrounded = false;
    
    // Optimización: cachear transforms
    private Transform playerTransform;
    #endregion

    #region Initialization
    /// <summary>
    /// Inicializa el InputManager con el jugador
    /// </summary>
    public void Initialize(GameObject player)
    {
        if (player == null)
        {
            Debug.LogError("[InputManager] Player object is null!");
            return;
        }

        playerObject = player;
        playerTransform = player.transform;

        // Obtener componentes necesarios
        if (!TryGetComponents())
        {
            Debug.LogError("[InputManager] Failed to get required components!");
            return;
        }

        isInitialized = true;
        Debug.Log("[InputManager] ✓ Initialized successfully");
    }

    bool TryGetComponents()
    {
        // Rigidbody
        rb = playerObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("[InputManager] Rigidbody not found, adding one...");
            rb = playerObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        // WeaponController
        weaponController = playerObject.GetComponent<WeaponController>();
        if (weaponController == null)
        {
            Debug.LogWarning("[InputManager] WeaponController not found!");
        }

        return rb != null;
    }
    #endregion

    #region Unity Lifecycle
    void Update()
    {
        if (!isInitialized || playerObject == null || rb == null)
            return;

        CheckGroundStatus();
        HandleInput();
    }
    #endregion

    #region Input Handling
    void HandleInput()
    {
        HandleMovement();
        HandleRotation();
        HandleJump();
        HandleShooting();
    }

    void HandleMovement()
    {
        // Obtener input
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D
        float vertical = Input.GetAxisRaw("Vertical");     // W/S

        // Calcular dirección de movimiento
        Vector3 movement = Vector3.zero;
        
        if (Mathf.Abs(vertical) > 0.01f)
        {
            movement += playerTransform.forward * vertical;
        }
        
        if (Mathf.Abs(horizontal) > 0.01f)
        {
            movement += playerTransform.right * horizontal;
        }

        // Aplicar movimiento
        if (movement.sqrMagnitude > 0.01f)
        {
            // Normalizar para evitar movimiento más rápido en diagonal
            Vector3 velocity = movement.normalized * moveSpeed;
            velocity.y = rb.linearVelocity.y; // Mantener velocidad vertical
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
        // Rotación con mouse (primaria)
        float mouseInput = Input.GetAxis("Mouse X");
        if (Mathf.Abs(mouseInput) > 0.001f)
        {
            mouseX += mouseInput * mouseSensitivity;
            playerTransform.rotation = Quaternion.Euler(0, mouseX, 0);
        }

        // Rotación con teclado Q/E (secundaria)
        float keyboardRotation = 0f;
        
        if (Input.GetKey(KeyCode.Q))
        {
            keyboardRotation = -keyboardRotateSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            keyboardRotation = keyboardRotateSpeed * Time.deltaTime;
        }

        if (Mathf.Abs(keyboardRotation) > 0.001f)
        {
            mouseX += keyboardRotation;
            playerTransform.rotation = Quaternion.Euler(0, mouseX, 0);
        }
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void HandleShooting()
    {
        if (weaponController == null)
            return;

        // Disparo automático (mantener presionado)
        if (Input.GetMouseButton(0))
        {
            weaponController.TryShoot();
        }

        // Alternativa: disparo semi-automático
        // if (Input.GetMouseButtonDown(0))
        // {
        //     weaponController.TryShoot();
        // }
    }
    #endregion

    #region Ground Detection
    void CheckGroundStatus()
    {
        // Raycast para detectar suelo
        isGrounded = Physics.Raycast(
            playerTransform.position,
            Vector3.down,
            1.1f,
            LayerMask.GetMask("Default", "Ground")
        );
    }

    // Debug visual
    void OnDrawGizmos()
    {
        if (playerTransform != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(playerTransform.position, Vector3.down * 1.1f);
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// Verifica si el jugador está en el suelo
    /// </summary>
    public bool IsGrounded()
    {
        return isGrounded;
    }

    /// <summary>
    /// Reinicia la rotación del mouse
    /// </summary>
    public void ResetMouseRotation()
    {
        if (playerTransform != null)
        {
            mouseX = playerTransform.eulerAngles.y;
        }
    }
    #endregion
}