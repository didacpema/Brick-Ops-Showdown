using UnityEngine;
using BrickOps.Core;
using BrickOps.Players;

/// <summary>
/// Sistema de control del jugador optimizado
/// Maneja input, movimiento, rotación, salto y animaciones
/// Integrado con CameraController, CameraShake y TorsoAimController
/// </summary>
public class InputManager : MonoBehaviour
{
    #region Inspector Variables
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;

    [Header("Jump")]
    public float jumpForce = 4f;
    public float jumpCooldown = 1f;
    public float groundCheckDistance = 1.1f;
    public LayerMask groundLayers = -1;

    [Header("Shooting")]
    public float shootCooldown = 0.4f;
    #endregion

    #region Components
    private GameObject playerObject;
    private Transform playerTransform;
    private Rigidbody rb;
    private Animator animator;
    private WeaponController weaponController;
    private CameraController cameraController;
    private CameraShake cameraShake;
    // TorsoAimController - se inicializa automáticamente en su propio Start()
    #endregion

    #region State
    private bool isInitialized;
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private float mouseX;
    
    private Vector3 moveDirection;
    private bool isMoving;
    private bool isRunning;
    private bool isAiming;
    private float currentMoveSpeed;
      private float lastJumpTime;
    private float lastShootTime;
    private int jumpBufferFrames;
    private int shootBufferFrames;
    private int currentShootCount;
    private int currentJumpCount;
    private bool justShot; // Para cancelar el correr al disparar
    
    private const int TRIGGER_BUFFER_DURATION = 10;
    private const float JUMP_GROUND_CHECK_DELAY = 0.2f;
    #endregion

    #region Animation Hashes
    private static readonly int HashIsWalking = Animator.StringToHash("IsWalking");
    private static readonly int HashIsRunning = Animator.StringToHash("IsRunning");
    private static readonly int HashIsAiming = Animator.StringToHash("IsAiming");
    private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashJump = Animator.StringToHash("Jump");
    private static readonly int HashShoot = Animator.StringToHash("Shoot");
    #endregion

    #region Initialization
    public void Initialize(GameObject player)
    {
        if (player == null)
        {
            Debug.LogError("[InputManager] Player object is null");
            return;
        }

        playerObject = player;
        playerTransform = player.transform;
        mouseX = playerTransform.eulerAngles.y;

        if (!TryGetComponents())
        {
            Debug.LogError("[InputManager] Failed to get required components");
            return;
        }

        ConfigurePhysics();
        isInitialized = true;
    }

    bool TryGetComponents()
    {
        rb = playerObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = playerObject.AddComponent<Rigidbody>();
        }

        animator = playerObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = playerObject.GetComponentInChildren<Animator>();
        }        weaponController = playerObject.GetComponent<WeaponController>();
        
        cameraController = playerObject.GetComponentInChildren<CameraController>();
        if (cameraController != null)
        {
            cameraShake = cameraController.GetComponent<CameraShake>();
        }

        // TorsoAimController se inicializa automáticamente en su propio Start()
        
        // Conectar el crosshair con el WeaponController del jugador
        ConnectCrosshairToWeapon();

        return rb != null;
    }
      void ConnectCrosshairToWeapon()
    {
        if (weaponController == null) return;
        
        // Buscar el crosshair en la escena
        BrickOps.UI.DynamicCrosshair crosshair = FindAnyObjectByType<BrickOps.UI.DynamicCrosshair>();
        if (crosshair != null)
        {
            crosshair.SetWeaponController(weaponController);
            crosshair.inputManager = this; // Asignar también el InputManager para detectar salto
            Debug.Log("[InputManager] Crosshair conectado al WeaponController del jugador");
        }
    }

    void ConfigurePhysics()
    {
        if (rb == null) return;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }
    #endregion

    #region Unity Lifecycle
    void Update()
    {
        if (!isInitialized) return;

        if (shootBufferFrames > 0) shootBufferFrames--;
        if (jumpBufferFrames > 0) jumpBufferFrames--;        UpdateGroundStatus();
        ProcessInput();
        UpdateAnimations();
        
        if (cameraController != null)
        {
            cameraController.SetAiming(isAiming);
            cameraController.SetSprinting(isRunning);
            cameraController.SetMovementState(isMoving, isRunning);
        }
        
        // Actualizar estado del arma para el crosshair dinámico
        if (weaponController != null)
        {
            weaponController.SetAiming(isAiming);
            weaponController.SetMovementState(isMoving, isRunning);
            weaponController.SetGrounded(isGrounded); // Actualizar estado de salto
        }
    }
    #endregion

    #region Input Processing
    void ProcessInput()
    {
        CaptureMovementInput();
        CaptureRotationInput();
        CaptureJumpInput();
        CaptureAimingInput();
        CaptureShootingInput();
    }

    void CaptureMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        moveDirection = Vector3.zero;
        
        if (Mathf.Abs(vertical) > 0.01f)
            moveDirection += playerTransform.forward * vertical;
        
        if (Mathf.Abs(horizontal) > 0.01f)
            moveDirection += playerTransform.right * horizontal;        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        // No permitir correr si acabas de disparar o si estás apuntando
        bool canRun = Input.GetKey(KeyCode.LeftShift) && moveDirection.sqrMagnitude > 0.01f && !isAiming && !justShot;
        isRunning = canRun;
        isMoving = moveDirection.sqrMagnitude > 0.01f;
        currentMoveSpeed = isMoving ? (isRunning ? runSpeed : walkSpeed) : 0f;
        
        // Reset del flag de disparo
        if (justShot && !Input.GetKey(KeyCode.LeftShift))
        {
            justShot = false;
        }

        if (isMoving)
        {
            Vector3 velocity = moveDirection * currentMoveSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    void CaptureRotationInput()
    {
        // Obtener sensibilidad de la cámara
        float sensitivity = cameraController != null ? cameraController.mouseSensitivity : 2f;
        float mouseInput = Input.GetAxis("Mouse X") * sensitivity;
        if (Mathf.Abs(mouseInput) > 0.001f)
        {
            mouseX += mouseInput;
        }

        // Always enforce rotation from mouseX so physics can't drift it
        playerTransform.rotation = Quaternion.Euler(0, mouseX, 0);
    }

    void CaptureJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && CanJump())
        {
            PerformJump();
        }
    }

    void CaptureAimingInput()
    {
        isAiming = Input.GetMouseButton(1);
        
        // Informar al arma del estado de apuntado
        if (weaponController != null)
        {
            weaponController.SetAiming(isAiming);
        }
    }    void CaptureShootingInput()
    {
        if (weaponController == null) return;

        if (Input.GetMouseButtonDown(0) && Time.time >= lastShootTime + shootCooldown)
        {
            // Marcar que acabamos de disparar para cancelar el correr
            justShot = true;
            isRunning = false;
            
            weaponController.TryShoot();
            currentShootCount++;
            lastShootTime = Time.time;
            shootBufferFrames = TRIGGER_BUFFER_DURATION;
            
            if (animator != null)
            {
                animator.SetTrigger(HashShoot);
            }

            if (cameraShake != null)
            {
                cameraShake.ShakeOnShoot();
            }
        }
    }
    #endregion

    #region Jump System
    bool CanJump()
    {
        return isGrounded && (Time.time >= lastJumpTime + jumpCooldown);
    }

    void PerformJump()
    {
        if (rb == null) return;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        currentJumpCount++;
        lastJumpTime = Time.time;
        isGrounded = false;
        jumpBufferFrames = TRIGGER_BUFFER_DURATION;
        
        if (animator != null)
        {
            animator.SetTrigger(HashJump);
        }
        
        // Trigger camera shake en salto
        if (cameraController != null)
        {
            cameraController.TriggerJumpShake();
        }
    }
    #endregion

    #region Ground Detection
    void UpdateGroundStatus()
    {
        if (playerTransform == null || rb == null) return;

        wasGroundedLastFrame = isGrounded;
        float timeSinceJump = Time.time - lastJumpTime;
        
        // No permitir detección de suelo inmediatamente después de saltar
        if (timeSinceJump < JUMP_GROUND_CHECK_DELAY)
        {
            isGrounded = false;
            return;
        }

        // Usar raycast simple desde abajo del jugador
        float rayLength = 0.8f; // Aumentado para pendientes pronunciadas
        Vector3 rayOrigin = playerTransform.position + Vector3.up * 0.2f; // Origen más alto
        
        // Hacer varios raycasts en un patrón para mejor detección en superficies irregulares
        bool hitGround = false;
        float minDistance = float.MaxValue;
        
        // Raycast central
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, groundLayers))
        {
            if (!hit.collider.transform.IsChildOf(playerTransform))
            {
                hitGround = true;
                minDistance = Mathf.Min(minDistance, hit.distance);
            }
        }
        
        // Raycasts adicionales en patrón circular más denso para superficies irregulares
        float offsetRadius = 0.3f; // Radio aumentado
        Vector3[] offsets = new Vector3[]
        {
            playerTransform.forward * offsetRadius,
            -playerTransform.forward * offsetRadius,
            playerTransform.right * offsetRadius,
            -playerTransform.right * offsetRadius,
            // Diagonales para mejor cobertura
            (playerTransform.forward + playerTransform.right).normalized * offsetRadius,
            (playerTransform.forward - playerTransform.right).normalized * offsetRadius,
            (-playerTransform.forward + playerTransform.right).normalized * offsetRadius,
            (-playerTransform.forward - playerTransform.right).normalized * offsetRadius
        };
        
        foreach (Vector3 offset in offsets)
        {
            Vector3 offsetOrigin = rayOrigin + offset;
            if (Physics.Raycast(offsetOrigin, Vector3.down, out RaycastHit offsetHit, rayLength, groundLayers))
            {
                if (!offsetHit.collider.transform.IsChildOf(playerTransform))
                {
                    hitGround = true;
                    minDistance = Mathf.Min(minDistance, offsetHit.distance);
                }
            }
        }
        
        if (hitGround)
        {
            float verticalVelocity = rb.linearVelocity.y;
            
            // Está en el suelo si está cerca Y no está subiendo rápido
            bool isNearGround = minDistance < 0.6f; // Aumentado significativamente para pendientes
            bool notMovingUpFast = verticalVelocity < 2f; // Más tolerante con velocidad vertical
            
            isGrounded = isNearGround && notMovingUpFast;
        }
        else
        {
            isGrounded = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        float rayLength = 0.8f;
        Vector3 rayOrigin = playerTransform.position + Vector3.up * 0.2f;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        
        // Raycast central
        Gizmos.DrawRay(rayOrigin, Vector3.down * rayLength);
        
        // Raycasts adicionales (8 en total)
        float offsetRadius = 0.3f;
        Vector3[] offsets = new Vector3[]
        {
            playerTransform.forward * offsetRadius,
            -playerTransform.forward * offsetRadius,
            playerTransform.right * offsetRadius,
            -playerTransform.right * offsetRadius,
            (playerTransform.forward + playerTransform.right).normalized * offsetRadius,
            (playerTransform.forward - playerTransform.right).normalized * offsetRadius,
            (-playerTransform.forward + playerTransform.right).normalized * offsetRadius,
            (-playerTransform.forward - playerTransform.right).normalized * offsetRadius
        };
        
        foreach (Vector3 offset in offsets)
        {
            Vector3 offsetOrigin = rayOrigin + offset;
            Gizmos.DrawRay(offsetOrigin, Vector3.down * rayLength);
        }
        
        if (isGrounded)
        {
            Gizmos.DrawWireSphere(rayOrigin, 0.3f);
        }
    }
    #endregion

    #region Animation System
    void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetBool(HashIsGrounded, isGrounded);
        animator.SetBool(HashIsWalking, isMoving && !isRunning);
        animator.SetBool(HashIsRunning, isRunning);
        animator.SetBool(HashIsAiming, isAiming);
    }
    #endregion

    #region Public API
    public bool IsGrounded() => isGrounded;
    public bool IsMoving() => isMoving;
    public bool IsRunning() => isRunning;
    public bool IsAiming() => isAiming;
    public float GetCurrentSpeed() => currentMoveSpeed;

    public void ResetMouseRotation()
    {
        if (playerTransform != null)
        {
            mouseX = playerTransform.eulerAngles.y;
        }
    }

    public string GetDebugInfo()
    {
        return $"Ground: {isGrounded} | Moving: {isMoving} | Running: {isRunning} | " +
               $"Speed: {currentMoveSpeed:F2} | Aiming: {isAiming}";
    }

    public PlayerState GetCurrentPlayerState(int playerId)
    {
        if (playerTransform == null) return null;

        return new PlayerState(
            playerId,
            playerTransform.position,
            playerTransform.eulerAngles.y,
            isMoving && !isRunning,
            isRunning,
            isAiming,
            isGrounded,
            shootBufferFrames > 0,
            jumpBufferFrames > 0,
            currentShootCount,
            currentJumpCount
        );
    }

    public CameraController GetCameraController() => cameraController;
    public CameraShake GetCameraShake() => cameraShake;
    #endregion
}
