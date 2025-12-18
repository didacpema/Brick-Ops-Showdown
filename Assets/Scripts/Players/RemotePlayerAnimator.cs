using UnityEngine;
using BrickOps.Core;

namespace BrickOps.Players
{
    public class RemotePlayerAnimator : MonoBehaviour
    {
        #region Private Variables
        private Animator animator;
        private bool isInitialized = false;
        
        // Cache de estados anteriores para detectar cambios
        private bool lastWalking = false;
        private bool lastRunning = false;
        private bool lastAiming = false;
        private bool lastCrouching = false;
        private bool lastGrounded = true;
        private bool lastReloading = false;
        private int lastShootCount = -1;
        private int lastJumpCount = -1;

        // Timer para mantener Upper Body Layer activo
        private float shootAnimationTimer = 0f;
        private const float SHOOT_ANIMATION_DURATION = 0.6f;

        // Hashes de parámetros del Animator
        private static readonly int HashIsWalking = Animator.StringToHash("IsWalking");
        private static readonly int HashIsRunning = Animator.StringToHash("IsRunning");
        private static readonly int HashIsAiming = Animator.StringToHash("IsAiming");
        private static readonly int HashIsCrouching = Animator.StringToHash("IsCrouching");
        private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int HashJump = Animator.StringToHash("Jump");
        private static readonly int HashShoot = Animator.StringToHash("Shoot");
        private static readonly int HashReload = Animator.StringToHash("Reload");

        [Header("Debug")]
        [Tooltip("Mostrar logs de sincronización")]
        public bool showDebug = false;
        #endregion

        #region Initialization
        void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator == null)
            {
                Debug.LogWarning($"[RemotePlayerAnimator] Animator not found on {gameObject.name}! Animations will NOT sync.");
                isInitialized = false;
                return;
            }

            isInitialized = true;
            Debug.Log($"[RemotePlayerAnimator] Initialized on {gameObject.name}");
        }
        #endregion

        #region Animation Sync
        public void ApplyAnimationState(PlayerState state)
        {
            if (!isInitialized || animator == null || state == null)
                return;

            if (state.isWalking != lastWalking)
            {
                animator.SetBool(HashIsWalking, state.isWalking);
                lastWalking = state.isWalking;
            }

            if (state.isRunning != lastRunning)
            {
                animator.SetBool(HashIsRunning, state.isRunning);
                lastRunning = state.isRunning;
            }

            if (state.isAiming != lastAiming)
            {
                animator.SetBool(HashIsAiming, state.isAiming);
                lastAiming = state.isAiming;
            }

            if (state.isCrouching != lastCrouching)
            {
                animator.SetBool(HashIsCrouching, state.isCrouching);
                lastCrouching = state.isCrouching;
            }

            if (state.isGrounded != lastGrounded)
            {
                animator.SetBool(HashIsGrounded, state.isGrounded);
                lastGrounded = state.isGrounded;
            }

            if (state.isReloading != lastReloading)
            {
                if (state.isReloading)
                {
                    animator.SetTrigger(HashReload);
                    
                    if (showDebug)
                        Debug.Log($"[RemotePlayerAnimator] Reload triggered!");
                }
                lastReloading = state.isReloading;
            }

            if (lastShootCount != -1 && state.shootCount > lastShootCount)
            {
                animator.SetTrigger(HashShoot);
                shootAnimationTimer = SHOOT_ANIMATION_DURATION;
                
                if (showDebug)
                    Debug.Log($"[RemotePlayerAnimator] Shoot triggered! Count: {state.shootCount}");
            }
            lastShootCount = state.shootCount;

            if (lastJumpCount != -1 && state.jumpCount > lastJumpCount)
            {
                animator.SetTrigger(HashJump);
                
                if (showDebug)
                    Debug.Log($"[RemotePlayerAnimator] Jump triggered! Count: {state.jumpCount}");
            }
            lastJumpCount = state.jumpCount;
            
            if (shootAnimationTimer > 0)
            {
                shootAnimationTimer -= Time.deltaTime;
            }
            
            float targetWeight = (state.isAiming || shootAnimationTimer > 0 || state.isReloading) ? 1f : 0f;
            float currentWeight = animator.GetLayerWeight(1);
            
            float blendSpeed = targetWeight > currentWeight ? 10f : 3f; 
            float newWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * blendSpeed);
            animator.SetLayerWeight(1, newWeight);
        }

        public void ResetAnimationState()
        {
            if (!isInitialized || animator == null)
                return;

            animator.SetBool(HashIsWalking, false);
            animator.SetBool(HashIsRunning, false);
            animator.SetBool(HashIsAiming, false);
            animator.SetBool(HashIsCrouching, false);
            animator.SetBool(HashIsGrounded, true);
            animator.SetLayerWeight(1, 0f);

            lastWalking = false;
            lastRunning = false;
            lastAiming = false;
            lastCrouching = false;
            lastGrounded = true;
            lastReloading = false;

            Debug.Log($"[RemotePlayerAnimator] Animation state reset on {gameObject.name}");
        }
        #endregion

        #region Unity Lifecycle
        void Update()
        {
            if (shootAnimationTimer > 0)
            {
                shootAnimationTimer -= Time.deltaTime;
            }
        }
        #endregion

        #region Public API
        public bool IsInitialized => isInitialized;

        public Animator GetAnimator() => animator;
        #endregion
    }
}
