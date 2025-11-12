using UnityEngine;
using BrickOps.Core;
using BrickOps.Players;

/// <summary>
/// Sistema de control del jugador optimizado
/// Maneja input, movimiento, rotación, salto y animaciones
/// Integrado con CameraController y CameraShake
/// </summary>
public class InputManager : MonoBehaviour
{
    #region Inspector Variables
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;

    [Header("Rotation")]
    public float mouseSensitivity = 2f;
    public float keyboardRotateSpeed = 100f;

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
        }

        weaponController = playerObject.GetComponent<WeaponController>();
        
        cameraController = playerObject.GetComponentInChildren<CameraController>();
        if (cameraController != null)
        {
            cameraShake = cameraController.GetComponent<CameraShake>();
        }

        return rb != null;
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
        if (jumpBufferFrames > 0) jumpBufferFrames--;

        UpdateGroundStatus();
        ProcessInput();
        UpdateAnimations();
        
        if (cameraController != null)
        {
            cameraController.SetAiming(isAiming);
            cameraController.SetSprinting(isRunning);
            cameraController.SetMovementState(isMoving && !isRunning, isRunning);
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
            moveDirection += playerTransform.right * horizontal;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        isRunning = Input.GetKey(KeyCode.LeftShift) && moveDirection.sqrMagnitude > 0.01f && !isAiming;
        isMoving = moveDirection.sqrMagnitude > 0.01f;
        currentMoveSpeed = isMoving ? (isRunning ? runSpeed : walkSpeed) : 0f;

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
        float mouseInput = Input.GetAxis("Mouse X");
        if (Mathf.Abs(mouseInput) > 0.001f)
        {
            mouseX += mouseInput * mouseSensitivity;
            playerTransform.rotation = Quaternion.Euler(0, mouseX, 0);
        }

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
    }

    void CaptureShootingInput()
    {
        if (weaponController == null) return;

        if (Input.GetMouseButtonDown(0) && Time.time >= lastShootTime + shootCooldown)
        {
            weaponController.TryShoot();
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

        // Raycast desde el centro del jugador hacia abajo
        if (Physics.Raycast(playerTransform.position, Vector3.down, out RaycastHit hit, 
            groundCheckDistance, groundLayers))
        {
            float verticalVelocity = rb.linearVelocity.y;
            float distanceToGround = hit.distance;
            
            // Está en el suelo si:
            // 1. La distancia es pequeña Y no está subiendo rápido
            // 2. O está cayendo y cerca del suelo
            bool isNearGround = distanceToGround < 0.2f;
            bool notMovingUpFast = verticalVelocity < 1f;
            
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

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(playerTransform.position, Vector3.down * groundCheckDistance);
        
        if (isGrounded)
        {
            Gizmos.DrawSphere(playerTransform.position + Vector3.down * groundCheckDistance, 0.1f);
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

    public BrickOps.Core.PlayerState GetCurrentPlayerState(int playerId)
    {
        if (playerTransform == null) return null;

        return new BrickOps.Core.PlayerState(
            playerId,
            playerTransform.position,
            playerTransform.eulerAngles.y,
            isMoving && !isRunning,
            isRunning,
            isAiming,
            isGrounded,
            shootBufferFrames > 0,
            jumpBufferFrames > 0
        );
    }

    public CameraController GetCameraController() => cameraController;
    public CameraShake GetCameraShake() => cameraShake;
    #endregion
}
