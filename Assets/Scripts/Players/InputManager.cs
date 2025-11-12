using UnityEngine;
using BrickOps.Core;

/// <summary>
/// ✨ Sistema completo y pulido de control del jugador ✨
/// Gestiona input, movimiento, rotación, salto y animaciones de forma profesional
/// Optimizado con hashes de parámetros y sistema de estados fluido
/// 
/// 🎬 ANIMATOR SETUP:
/// Este sistema está diseñado para trabajar con Animation Layers:
/// - Layer 0 (Movement): Controla cuerpo completo (Idle, Walk, Run, Jump)
/// - Layer 1 (UpperBody): Controla solo brazos/torso con Avatar Mask (Aim, Shoot)
/// 
/// Ver ANIMATOR_SETUP_GUIDE.md para configuración completa del Animator Controller
/// 
/// ⚡ FEATURES:
/// - Semi-automatic shooting con cooldown (0.4s)
/// - Sprint restringido mientras apuntas (más precisión)
/// - Camera zoom suave al apuntar (FOV 60→40)
/// - Jump cooldown para prevenir spam
/// - Ground detection precisa con triple verificación
/// - AimBlend parameter para Blend Trees opcionales
/// </summary>
public class InputManager : MonoBehaviour
{
    #region Inspector Variables
    [Header("Movement Settings")]
    [Tooltip("Velocidad de caminata")]
    public float walkSpeed = 3f;
    
    [Tooltip("Velocidad de carrera (Shift)")]
    public float runSpeed = 6f;

    [Header("Rotation Settings")]
    [Tooltip("Sensibilidad del mouse")]
    public float mouseSensitivity = 2f;
    
    [Tooltip("Velocidad de rotación con teclado Q/E")]
    public float keyboardRotateSpeed = 100f;

    [Header("Jump Settings")]
    [Tooltip("Fuerza del salto")]
    public float jumpForce = 4f;
    
    [Tooltip("Cooldown entre saltos (segundos)")]
    public float jumpCooldown = 1f;
    
    [Tooltip("Altura para detectar suelo")]
    public float groundCheckDistance = 1.1f;
    
    [Tooltip("Capas que cuentan como suelo")]
    public LayerMask groundLayers = -1;

    [Header("Aim Settings")]
    [Tooltip("FOV normal de la cámara")]
    public float normalFOV = 60f;
    
    [Tooltip("FOV al apuntar (menor = más zoom)")]
    public float aimFOV = 40f;
    
    [Tooltip("Velocidad de transición del zoom")]
    public float zoomSpeed = 10f;

    [Header("Debug")]
    [Tooltip("Mostrar información de debug en consola")]
    public bool showDebug = false;
    #endregion

    #region Private Variables - Components
    private GameObject playerObject;
    private Transform playerTransform;
    private Rigidbody rb;
    private Animator animator;
    private WeaponController weaponController;
    private Camera playerCamera;
    #endregion

    #region Private Variables - State
    private bool isInitialized = false;
    private bool isGrounded = false;
    private float mouseX = 0f;
    private bool wasGroundedLastFrame = false;
    #endregion

    #region Private Variables - Movement
    private Vector3 moveDirection = Vector3.zero;
    private bool isMoving = false;
    private bool isRunning = false;
    private bool isAiming = false;
    private float currentMoveSpeed = 0f;
    #endregion    #region Private Variables - Jump
    private float lastJumpTime = 0f;
    private float jumpGroundCheckDelay = 0.2f; // Tiempo que ignora el suelo después de saltar
    private int jumpBufferFrames = 0; // Contador de frames para mantener el trigger activo
    private const int TRIGGER_BUFFER_DURATION = 10; // Mantener trigger activo por 10 frames (~333ms)

    #region Private Variables - Shooting
    private float lastShootTime = 0f;
    private float shootCooldown = 0.4f; // Cooldown mínimo entre disparos
    private int shootBufferFrames = 0; // Contador de frames para mantener el trigger activo
    #endregion

    #region Animation Parameter Hashes (Optimización)
    // Usar hashes en lugar de strings es MUCHO más eficiente
    private static readonly int HashIsWalking = Animator.StringToHash("IsWalking");
    private static readonly int HashIsRunning = Animator.StringToHash("IsRunning");
    private static readonly int HashIsAiming = Animator.StringToHash("IsAiming");
    private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashAimBlend = Animator.StringToHash("AimBlend");
    private static readonly int HashJump = Animator.StringToHash("Jump");
    private static readonly int HashShoot = Animator.StringToHash("Shoot");
    #endregion

    #region Initialization
    /// <summary>
    /// Inicializa el InputManager con el jugador
    /// </summary>
    public void Initialize(GameObject player)
    {
        if (player == null)
        {
            Debug.LogError("[InputManager] ❌ Player object is null!");
            return;
        }

        playerObject = player;
        playerTransform = player.transform;
        mouseX = playerTransform.eulerAngles.y;

        if (!TryGetComponents())
        {
            Debug.LogError("[InputManager] ❌ Failed to get required components!");
            return;
        }

        ConfigurePhysics();
        
        isInitialized = true;
        Debug.Log("[InputManager] ✓ Initialized successfully with animations");
    }

    /// <summary>
    /// Obtiene todos los componentes necesarios
    /// </summary>
    bool TryGetComponents()
    {
        // Rigidbody (REQUERIDO)
        rb = playerObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("[InputManager] Rigidbody not found, adding one...");
            rb = playerObject.AddComponent<Rigidbody>();
        }

        // Animator (IMPORTANTE para animaciones)
        animator = playerObject.GetComponent<Animator>();
        if (animator == null)
        {
            // Intentar buscar en hijos
            animator = playerObject.GetComponentInChildren<Animator>();
            
            if (animator != null)
            {
                Debug.Log("[InputManager] ✓ Animator found in children");
            }
            else
            {
                Debug.LogWarning("[InputManager] ⚠ Animator not found! Animations will NOT work.");
            }
        }
        else
        {
            Debug.Log("[InputManager] ✓ Animator found and ready");
        }

        // WeaponController (OPCIONAL)
        weaponController = playerObject.GetComponent<WeaponController>();
        if (weaponController == null)
        {
            Debug.LogWarning("[InputManager] ⚠ WeaponController not found!");
        }

        // Camera (para el zoom al apuntar)
        playerCamera = playerObject.GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            Debug.LogWarning("[InputManager] ⚠ Camera not found! Zoom will NOT work.");
        }
        else
        {
            normalFOV = playerCamera.fieldOfView; // Guardar FOV original
            Debug.Log("[InputManager] ✓ Camera found, FOV saved");
        }

        return rb != null;
    }

    /// <summary>
    /// Configura las propiedades físicas del jugador
    /// </summary>
    void ConfigurePhysics()
    {
        if (rb == null) return;

        // Congelar rotaciones en X y Z para evitar que el jugador se caiga
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        Debug.Log("[InputManager] ✓ Physics configured");
    }
    #endregion    #region Unity Lifecycle
    void Update()
    {
        if (!isInitialized)
            return;

        // ✅ NUEVO: Debug para verificar que los buffers funcionan
        if (showDebug)
        {
            if (shootBufferFrames > 0)
                Debug.Log($"[InputManager] 🔥 Shoot buffer active: {shootBufferFrames} frames left");
            
            if (jumpBufferFrames > 0)
                Debug.Log($"[InputManager] 🦘 Jump buffer active: {jumpBufferFrames} frames left");
        }

        // Decrementar contadores de buffer al inicio del frame
        if (shootBufferFrames > 0) shootBufferFrames--;
        if (jumpBufferFrames > 0) jumpBufferFrames--;

        // Ground detection
        UpdateGroundStatus();

        // Procesar input
        ProcessInput();

        // Actualizar animaciones
        UpdateAnimations();

        // Actualizar zoom de cámara
        UpdateCameraZoom();
    }
    
    #region Input Processing
    /// <summary>
    /// Procesa todo el input del jugador
    /// </summary>
    void ProcessInput()
    {
        CaptureMovementInput();
        CaptureRotationInput();
        CaptureJumpInput();
        CaptureAimingInput();
        CaptureShootingInput();
    }

    /// <summary>
    /// Captura el input de movimiento (WASD + Shift)
    /// </summary>
    void CaptureMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D
        float vertical = Input.GetAxisRaw("Vertical");     // W/S

        // Calcular dirección de movimiento
        moveDirection = Vector3.zero;
        
        if (Mathf.Abs(vertical) > 0.01f)
            moveDirection += playerTransform.forward * vertical;
        
        if (Mathf.Abs(horizontal) > 0.01f)
            moveDirection += playerTransform.right * horizontal;

        // Normalizar para evitar velocidad mayor en diagonal
        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        // Detectar si está corriendo (Shift) - NO se puede correr mientras se apunta
        isRunning = Input.GetKey(KeyCode.LeftShift) && moveDirection.sqrMagnitude > 0.01f && !isAiming;
        
        // Detectar si se está moviendo
        isMoving = moveDirection.sqrMagnitude > 0.01f;
        
        // Calcular velocidad
        currentMoveSpeed = isMoving ? (isRunning ? runSpeed : walkSpeed) : 0f;

        // Aplicar movimiento
        if (isMoving)
        {
            Vector3 velocity = moveDirection * currentMoveSpeed;
            velocity.y = rb.linearVelocity.y; // Mantener velocidad vertical
            rb.linearVelocity = velocity;
        }
        else
        {
            // Detener movimiento horizontal
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    /// <summary>
    /// Captura el input de rotación (Mouse + Q/E)
    /// </summary>
    void CaptureRotationInput()
    {
        // Rotación con mouse (PRINCIPAL)
        float mouseInput = Input.GetAxis("Mouse X");
        if (Mathf.Abs(mouseInput) > 0.001f)
        {
            mouseX += mouseInput * mouseSensitivity;
            playerTransform.rotation = Quaternion.Euler(0, mouseX, 0);
        }

        // Rotación con teclado Q/E (SECUNDARIA)
        if (Input.GetKey(KeyCode.Q))
        {
            mouseX -= keyboardRotateSpeed * Time.deltaTime;
            playerTransform.rotation = Quaternion.Euler(0, mouseX, 0);
        }
        else if (Input.GetKey(KeyCode.E))
        {
            mouseX += keyboardRotateSpeed * Time.deltaTime;
            playerTransform.rotation = Quaternion.Euler(0, mouseX, 0);
        }
    }

    /// <summary>
    /// Captura el input de salto (Space)
    /// Solo permite saltar si está en el suelo Y ha pasado el cooldown
    /// </summary>
    void CaptureJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && CanJump())
        {
            PerformJump();
        }
    }

    /// <summary>
    /// Captura el input de apuntar (Click derecho)
    /// </summary>
    void CaptureAimingInput()
    {
        isAiming = Input.GetMouseButton(1); // Right click
    }

    /// <summary>
    /// Captura el input de disparo (Click izquierdo)
    /// SEMI-AUTOMÁTICO: Solo dispara una vez por click con cooldown
    /// </summary>
    void CaptureShootingInput()
    {
        if (weaponController == null)
            return;        // Disparo SEMI-AUTOMÁTICO con cooldown adicional
        // Solo dispara si se hace click Y ha pasado el tiempo de cooldown
        if (Input.GetMouseButtonDown(0) && Time.time >= lastShootTime + shootCooldown)
        {
            weaponController.TryShoot();
            lastShootTime = Time.time;
            
            // Activar buffer de frames para sincronización de red
            shootBufferFrames = TRIGGER_BUFFER_DURATION;
            
            // Trigger de animación
            if (animator != null)
            {
                animator.SetTrigger(HashShoot);
            }
            
            if (showDebug)
                Debug.Log($"[InputManager] 💥 Shot fired at {Time.time:F2} (buffer: {TRIGGER_BUFFER_DURATION} frames)");
        }
    }
    #endregion

    #region Jump System
    /// <summary>
    /// Verifica si el jugador puede saltar
    /// </summary>
    bool CanJump()
    {
        return isGrounded && (Time.time >= lastJumpTime + jumpCooldown);
    }    /// <summary>
    /// Ejecuta el salto
    /// </summary>
    void PerformJump()
    {
        if (rb == null) return;        // Aplicar fuerza de salto
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        
        // Actualizar tiempo del último salto
        lastJumpTime = Time.time;
        
        // IMPORTANTE: Forzar isGrounded a false inmediatamente al saltar
        isGrounded = false;
        
        // Activar buffer de frames para sincronización de red
        jumpBufferFrames = TRIGGER_BUFFER_DURATION;
        
        // Trigger de animación
        if (animator != null)
        {
            animator.SetTrigger(HashJump);
        }
        
        if (showDebug)
            Debug.Log($"[InputManager] 🦘 Jump performed at {Time.time:F2} (buffer: {TRIGGER_BUFFER_DURATION} frames)");
    }
    #endregion

    #region Ground Detection
    /// <summary>
    /// Actualiza el estado del suelo
    /// </summary>
    void UpdateGroundStatus()
    {
        if (playerTransform == null) return;

        wasGroundedLastFrame = isGrounded;

        // Si acabamos de saltar, ignorar la detección de suelo por un breve momento
        float timeSinceJump = Time.time - lastJumpTime;
        if (timeSinceJump < jumpGroundCheckDelay)
        {
            isGrounded = false;
            
            if (showDebug)
            {
                Debug.Log($"[InputManager] ⏳ Ignoring ground check - {timeSinceJump:F2}s since jump");
            }
            return;
        }

        // Raycast hacia abajo para detectar suelo
        RaycastHit hit;
        bool rayHitGround = Physics.Raycast(
            playerTransform.position,
            Vector3.down,
            out hit,
            groundCheckDistance,
            groundLayers
        );

        // Solo considerar que está en el suelo si:
        // 1. El raycast detecta suelo Y
        // 2. La velocidad vertical es negativa (cayendo) O está muy cerca del suelo (casi tocando)
        if (rayHitGround && rb != null)
        {
            float verticalVelocity = rb.linearVelocity.y;
            float distanceToGround = hit.distance;
            
            // CRÍTICO: Solo true si:
            // - Está cayendo (velocidad negativa) Y cerca del suelo (< 0.2)
            // - O está completamente quieto (velocidad muy baja) Y muy cerca (< 0.15)
            bool isFalling = verticalVelocity < -0.1f && distanceToGround < 0.2f;
            bool isStationary = Mathf.Abs(verticalVelocity) < 0.1f && distanceToGround < 0.15f;
            
            isGrounded = isFalling || isStationary;
            
            if (showDebug)
            {
                Debug.Log($"[InputManager] Ray: {rayHitGround} | Dist: {distanceToGround:F3} | VelY: {verticalVelocity:F2} | Ground: {isGrounded}");
            }
        }
        else
        {
            isGrounded = false;
        }

        // Log cuando aterriza
        if (isGrounded && !wasGroundedLastFrame && showDebug)
        {
            Debug.Log("[InputManager] 🎯 Landed!");
        }
    }

    /// <summary>
    /// Visualización de debug en el editor
    /// </summary>
    void OnDrawGizmos()
    {
        if (playerTransform == null || !showDebug)
            return;

        // Dibujar ray de detección de suelo
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(playerTransform.position, Vector3.down * groundCheckDistance);
        
        // Dibujar esfera en punto de contacto
        if (isGrounded)
        {
            Gizmos.DrawSphere(playerTransform.position + Vector3.down * groundCheckDistance, 0.1f);
        }
    }
    #endregion

    #region Animation System
    /// <summary>
    /// Actualiza todos los parámetros del Animator
    /// Sistema optimizado con hashes y lógica pulida
    /// </summary>
    void UpdateAnimations()
    {
        if (animator == null)
            return;

        // IsGrounded: CRÍTICO para el sistema de salto/aire
        animator.SetBool(HashIsGrounded, isGrounded);

        // IsWalking: se activa cuando hay movimiento pero NO está corriendo
        bool shouldWalk = isMoving && !isRunning;
        animator.SetBool(HashIsWalking, shouldWalk);

        // IsRunning: se activa solo cuando corre
        animator.SetBool(HashIsRunning, isRunning);

        // IsAiming: refleja el estado de apuntar
        animator.SetBool(HashIsAiming, isAiming);

        // AimBlend: Para blend tree en el aire (evita reinicio de animación)
        // Solo se usa cuando está en el aire, permite apuntar sin reiniciar la animación de salto

        

        // Debug opcional
        if (showDebug && isMoving)
        {
            string state = isRunning ? "Running" : "Walking";
            Debug.Log($"[InputManager] 🎬 Animation: {state} | Speed: {currentMoveSpeed:F2} | Grounded: {isGrounded}");
        }
    }
    #endregion

    #region Camera System
    /// <summary>
    /// Actualiza el zoom de la cámara basándose en si está apuntando o no
    /// </summary>
    void UpdateCameraZoom()
    {
        if (playerCamera == null)
            return;

        // FOV objetivo según si está apuntando
        float targetFOV = isAiming ? aimFOV : normalFOV;
        
        // Suavizar transición del zoom
        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFOV,
            Time.deltaTime * zoomSpeed
        );

        // Debug opcional
        if (showDebug && isAiming)
        {
            Debug.Log($"[InputManager] 🔭 Aiming | FOV: {playerCamera.fieldOfView:F1}");
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// Verifica si el jugador está en el suelo
    /// </summary>
    public bool IsGrounded() => isGrounded;

    /// <summary>
    /// Verifica si el jugador se está moviendo
    /// </summary>
    public bool IsMoving() => isMoving;

    /// <summary>
    /// Verifica si el jugador está corriendo
    /// </summary>
    public bool IsRunning() => isRunning;

    /// <summary>
    /// Verifica si el jugador está apuntando
    /// </summary>
    public bool IsAiming() => isAiming;

    /// <summary>
    /// Obtiene la velocidad actual de movimiento
    /// </summary>
    public float GetCurrentSpeed() => currentMoveSpeed;

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

    /// <summary>
    /// Obtiene información de debug
    /// </summary>
    public string GetDebugInfo()
    {
        return $"Ground: {isGrounded} | Moving: {isMoving} | Running: {isRunning} | " +
               $"Speed: {currentMoveSpeed:F2} | Aiming: {isAiming}";
    }    /// <summary>
    /// Crea un PlayerState completo con datos de posición y animación para sincronización de red
    /// </summary>
    public BrickOps.Core.PlayerState GetCurrentPlayerState(int playerId)
    {
        if (playerTransform == null)
            return null;

        return new BrickOps.Core.PlayerState(
            playerId,
            playerTransform.position,
            playerTransform.eulerAngles.y,
            isMoving && !isRunning,  // isWalking
            isRunning,                // isRunning
            isAiming,                 // isAiming
            isGrounded,               // isGrounded
            shootBufferFrames > 0,    // isShooting - TRUE mientras el buffer esté activo
            jumpBufferFrames > 0      // isJumping - TRUE mientras el buffer esté activo
        );
    }
    #endregion
}
