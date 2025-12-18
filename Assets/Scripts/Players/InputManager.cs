using UnityEngine;
using BrickOps.Core;
using BrickOps.Players;

public class InputManager : MonoBehaviour
{
    #region Inspector Variables
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float aimWalkSpeed = 3f;
    public float crouchSpeed = 2f;

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
    private bool isCrouching;
    private float currentMoveSpeed;
      private float lastJumpTime;
    private float lastShootTime;
    private int jumpBufferFrames;
    private int shootBufferFrames;
    private int currentShootCount;
    private int currentJumpCount;
    private bool justShot; 
    private float shootAnimationTimer; 
    
    private const int TRIGGER_BUFFER_DURATION = 10;
    private const float JUMP_GROUND_CHECK_DELAY = 0.2f;
    private const float SHOOT_ANIMATION_DURATION = 0.6f;
    #endregion

    #region Animation Hashes
    private static readonly int HashIsWalking = Animator.StringToHash("IsWalking");
    private static readonly int HashIsRunning = Animator.StringToHash("IsRunning");
    private static readonly int HashIsAiming = Animator.StringToHash("IsAiming");
    private static readonly int HashIsCrouching = Animator.StringToHash("IsCrouching");
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
        
        ConnectCrosshairToWeapon();

        return rb != null;
    }
      void ConnectCrosshairToWeapon()
    {
        if (weaponController == null) return;
        
        BrickOps.UI.DynamicCrosshair crosshair = FindAnyObjectByType<BrickOps.UI.DynamicCrosshair>();
        if (crosshair != null)
        {
            crosshair.SetWeaponController(weaponController);
            crosshair.inputManager = this; 
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

        UpdateGroundStatus();
        
        if (shootBufferFrames > 0) shootBufferFrames--;
        
        if (jumpBufferFrames > 0)
        {
            jumpBufferFrames--;
            if (CanJump())
            {
                PerformJump();
                jumpBufferFrames = 0;
            }
        }
        
        ProcessInput();
        UpdateAnimations();
        
        if (cameraController != null)
        {
            cameraController.SetAiming(isAiming);
            cameraController.SetSprinting(isRunning);
            cameraController.SetMovementState(isMoving, isRunning);
        }
        
        if (weaponController != null)
        {
            weaponController.SetAiming(isAiming);
            weaponController.SetMovementState(isMoving, isRunning);
            weaponController.SetGrounded(isGrounded); 
        }
    }
    #endregion

    #region Input Processing
    void ProcessInput()
    {
        CaptureMovementInput();
        CaptureRotationInput();
        CaptureCrouchInput();
        CaptureJumpInput();
        CaptureAimingInput();
        CaptureShootingInput();
        CaptureReloadInput();
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

        if (isCrouching && Input.GetKey(KeyCode.LeftShift) && moveDirection.sqrMagnitude > 0.01f)
        {
            isCrouching = false;
        }

        bool wantsToRun = Input.GetKey(KeyCode.LeftShift) && moveDirection.sqrMagnitude > 0.01f && !isCrouching;
        if (wantsToRun && weaponController != null && weaponController.IsReloading())
        {
            weaponController.CancelReload();
        }
        
        bool canRun = wantsToRun && !isAiming && !justShot;
        isRunning = canRun;
        isMoving = moveDirection.sqrMagnitude > 0.01f;
        
        if (isMoving)
        {
            if (isCrouching)
            {
                currentMoveSpeed = crouchSpeed;
            }
            else if (isAiming)
            {
                currentMoveSpeed = aimWalkSpeed;
            }
            else if (isRunning)
            {
                currentMoveSpeed = runSpeed;
            }
            else
            {
                currentMoveSpeed = walkSpeed;
            }
        }
        else
        {
            currentMoveSpeed = 0f;
        }
        
        if (justShot)
        {
            if (Time.time >= lastShootTime + SHOOT_ANIMATION_DURATION)
            {
                justShot = false;
            }
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
        float sensitivity = cameraController != null ? cameraController.mouseSensitivity : 2f;
        float mouseInput = Input.GetAxis("Mouse X") * sensitivity;
        if (Mathf.Abs(mouseInput) > 0.001f)
        {
            mouseX += mouseInput;
        }

        playerTransform.rotation = Quaternion.Euler(0, mouseX, 0);
    }

    void CaptureCrouchInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
        {
            isCrouching = !isCrouching;
        }
    }

    void CaptureJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (weaponController != null && weaponController.IsReloading())
            {
                weaponController.CancelReload();
            }
            
            if (isCrouching)
            {
                isCrouching = false;
            }
            else if (CanJump())
            {
                jumpBufferFrames = TRIGGER_BUFFER_DURATION;
            }
        }
    }

    void CaptureAimingInput()
    {
        isAiming = Input.GetMouseButton(1);
        
        if (weaponController != null)
        {
            weaponController.SetAiming(isAiming);
        }
    }    void CaptureShootingInput()
    {
        if (weaponController == null) return;

        if (Input.GetMouseButtonDown(0) && Time.time >= lastShootTime + shootCooldown)
        {
            bool canShoot = !weaponController.IsReloading() && weaponController.GetCurrentAmmo() > 0;
            
            weaponController.TryShoot();
            
            if (canShoot)
            {
                justShot = true;
                isRunning = false;
                currentShootCount++;
                lastShootTime = Time.time;
                shootBufferFrames = TRIGGER_BUFFER_DURATION;
                shootAnimationTimer = SHOOT_ANIMATION_DURATION;
                
                if (animator != null)
                {
                    animator.SetTrigger(HashShoot);
                }

                if (cameraController != null)
                {
                    cameraController.TriggerShootShake();
                }
            }
        }
        
        if (shootAnimationTimer > 0)
        {
            shootAnimationTimer -= Time.deltaTime;
        }
    }
    
    void CaptureReloadInput()
    {
        if (weaponController == null) return;
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            weaponController.TryReload();
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
        
        if (timeSinceJump < JUMP_GROUND_CHECK_DELAY)
        {
            isGrounded = false;
            return;
        }

        float rayLength = 0.8f; 
        Vector3 rayOrigin = playerTransform.position + Vector3.up * 0.2f;
        
        bool hitGround = false;
        float minDistance = float.MaxValue;
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, groundLayers))
        {
            if (!hit.collider.transform.IsChildOf(playerTransform))
            {
                hitGround = true;
                minDistance = Mathf.Min(minDistance, hit.distance);
            }
        }
        
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
            
            bool isNearGround = minDistance < 0.6f; 
            bool notMovingUpFast = verticalVelocity < 2f; 
            
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
        
        Gizmos.DrawRay(rayOrigin, Vector3.down * rayLength);
        
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
        animator.SetBool(HashIsCrouching, isCrouching);
        
        bool isReloading = weaponController != null && weaponController.IsReloading();
        float targetWeight = (isAiming || shootAnimationTimer > 0 || isReloading) ? 1f : 0f;
        float currentWeight = animator.GetLayerWeight(1);
        
        float blendSpeed = targetWeight > currentWeight ? 10f : 3f; 
        float newWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * blendSpeed);
        animator.SetLayerWeight(1, newWeight);
    }
    #endregion

    #region Public API
    public bool IsGrounded() => isGrounded;
    public bool IsMoving() => isMoving;
    public bool IsRunning() => isRunning;
    public bool IsAiming() => isAiming;
    public bool IsCrouching() => isCrouching;
    public bool IsShootAnimationActive() => shootAnimationTimer > 0;
    public float GetCurrentSpeed() => currentMoveSpeed;
    
    /// <summary>
    /// Fuerza la salida del modo apuntar (usado por WeaponController al recargar)
    /// </summary>
    public void ForceStopAiming()
    {
        isAiming = false;
    }

    public void ResetMouseRotation()
    {
        if (playerTransform != null)
        {
            mouseX = playerTransform.eulerAngles.y;
        }
    }

    public string GetDebugInfo()
    {
        return $"Ground: {isGrounded} | Moving: {isMoving} | Running: {isRunning} | Crouch: {isCrouching} | " +
               $"Speed: {currentMoveSpeed:F2} | Aiming: {isAiming}";
    }

    public PlayerState GetCurrentPlayerState(int playerId)
    {
        if (playerTransform == null) return null;

        bool isReloading = weaponController != null && weaponController.IsReloading();

        return new PlayerState(
            playerId,
            playerTransform.position,
            playerTransform.eulerAngles.y,
            isMoving && !isRunning,
            isRunning,
            isAiming,
            isCrouching,
            isGrounded,
            isReloading,
            currentShootCount,
            currentJumpCount
        );
    }

    public CameraController GetCameraController() => cameraController;
    #endregion
}
