using UnityEngine;
using BrickOps.Core;

namespace BrickOps.Players
{
    /// <summary>
    /// Sincroniza las animaciones de jugadores remotos basándose en PlayerState recibido de la red
    /// Se adjunta automáticamente a jugadores remotos para replicar sus animaciones    /// </summary>
    public class RemotePlayerAnimator : MonoBehaviour
    {
        #region Private Variables
        private Animator animator;
        private bool isInitialized = false;
        
        // Cache de estados anteriores para detectar cambios
        private bool lastWalking = false;
        private bool lastRunning = false;
        private bool lastAiming = false;
        private bool lastGrounded = true;
        private bool lastShooting = false;
        private bool lastJumping = false;
        private int lastShootCount = -1;
        private int lastJumpCount = -1;

        // Sistema de buffer para triggers (solución para eventos de un frame)
        private int shootBufferFrames = 0;
        private int jumpBufferFrames = 0;
        private const int TRIGGER_BUFFER_DURATION = 10; // Mantener trigger activo por 5 frames

        // Hashes de parámetros del Animator (optimización)
        private static readonly int HashIsWalking = Animator.StringToHash("IsWalking");
        private static readonly int HashIsRunning = Animator.StringToHash("IsRunning");
        private static readonly int HashIsAiming = Animator.StringToHash("IsAiming");
        private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int HashJump = Animator.StringToHash("Jump");
        private static readonly int HashShoot = Animator.StringToHash("Shoot");

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
            // Buscar Animator en este objeto o en hijos
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator == null)
            {
                Debug.LogWarning($"[RemotePlayerAnimator] ⚠ Animator not found on {gameObject.name}! Animations will NOT sync.");
                isInitialized = false;
                return;
            }

            isInitialized = true;
            Debug.Log($"[RemotePlayerAnimator] ✓ Initialized on {gameObject.name}");
        }
        #endregion

        #region Animation Sync
        /// <summary>
        /// Aplica el estado recibido al Animator
        /// </summary>
        public void ApplyAnimationState(PlayerState state)
        {
            if (!isInitialized || animator == null || state == null)
                return;

            // Actualizar parámetros bool (solo si cambiaron para optimizar)
            if (state.isWalking != lastWalking)
            {
                animator.SetBool(HashIsWalking, state.isWalking);
                lastWalking = state.isWalking;
                
                if (showDebug)
                    Debug.Log($"[RemotePlayerAnimator] Walking: {state.isWalking}");
            }

            if (state.isRunning != lastRunning)
            {
                animator.SetBool(HashIsRunning, state.isRunning);
                lastRunning = state.isRunning;
                
                if (showDebug)
                    Debug.Log($"[RemotePlayerAnimator] Running: {state.isRunning}");
            }
            // DETECTAR DISPARO (Si el número cambió, dispara)
            if (lastShootCount != -1 && state.shootCount > lastShootCount)
            {
                animator.SetTrigger(HashShoot);
            }
            lastShootCount = state.shootCount;

            // DETECTAR SALTO
            if (lastJumpCount != -1 && state.jumpCount > lastJumpCount)
            {
                animator.SetTrigger(HashJump);
            }
            lastJumpCount = state.jumpCount;

            if (state.isAiming != lastAiming)
            {
                animator.SetBool(HashIsAiming, state.isAiming);
                lastAiming = state.isAiming;
                
                if (showDebug)
                    Debug.Log($"[RemotePlayerAnimator] Aiming: {state.isAiming}");
            }

            if (state.isGrounded != lastGrounded)
            {
                animator.SetBool(HashIsGrounded, state.isGrounded);
                lastGrounded = state.isGrounded;
                
                if (showDebug)
                    Debug.Log($"[RemotePlayerAnimator] Grounded: {state.isGrounded}");
            }            // Triggers - se activan cuando hay cambio de false a true
            // Usamos un sistema de buffer para que los triggers no se pierdan
            if (state.isShooting && !lastShooting)
            {
                animator.SetTrigger(HashShoot);
                shootBufferFrames = TRIGGER_BUFFER_DURATION;
                
                if (showDebug)
                    Debug.Log($"[RemotePlayerAnimator] 💥 Shoot triggered! (Buffer: {TRIGGER_BUFFER_DURATION} frames)");
            }
            // Si el trigger sigue activo y el buffer no ha expirado, re-activarlo
            if (state.isShooting && shootBufferFrames > 0)
            {
                // NO re-activar trigger, solo mantener el buffer
                if (showDebug && shootBufferFrames == TRIGGER_BUFFER_DURATION - 1)
                    Debug.Log($"[RemotePlayerAnimator] 💥 Shoot buffer maintained (frames left: {shootBufferFrames})");
            }
            lastShooting = state.isShooting;

            if (state.isJumping && !lastJumping)
            {
                animator.SetTrigger(HashJump);
                jumpBufferFrames = TRIGGER_BUFFER_DURATION;
                
                if (showDebug)
                    Debug.Log($"[RemotePlayerAnimator] Jump triggered! (Buffer: {TRIGGER_BUFFER_DURATION} frames)");
            }
            // Si el trigger sigue activo y el buffer no ha expirado, re-activarlo
            if (state.isJumping && jumpBufferFrames > 0)
            {
                // NO re-activar trigger, solo mantener el buffer
                if (showDebug && jumpBufferFrames == TRIGGER_BUFFER_DURATION - 1)
                    Debug.Log($"[RemotePlayerAnimator] Jump buffer maintained (frames left: {jumpBufferFrames})");
            }
            lastJumping = state.isJumping;
        }

        /// <summary>
        /// Reinicia todos los estados a valores por defecto
        /// </summary>
        public void ResetAnimationState()
        {
            if (!isInitialized || animator == null)
                return;

            animator.SetBool(HashIsWalking, false);
            animator.SetBool(HashIsRunning, false);
            animator.SetBool(HashIsAiming, false);
            animator.SetBool(HashIsGrounded, true);

            lastWalking = false;
            lastRunning = false;
            lastAiming = false;
            lastGrounded = true;
            lastShooting = false;
            lastJumping = false;

            Debug.Log($"[RemotePlayerAnimator] Animation state reset on {gameObject.name}");
        }
        #endregion

        #region Unity Lifecycle
        void Update()
        {
            // Procesar buffer de triggers
            ProcessTriggerBuffers();
        }

        /// <summary>
        /// Procesa los buffers de triggers para mantenerlos activos por varios frames
        /// </summary>
        void ProcessTriggerBuffers()
        {
            if (!isInitialized || animator == null)
                return;

            // Procesar buffer de disparo
            if (shootBufferFrames > 0)
            {
                shootBufferFrames--;
                if (shootBufferFrames == 0 && showDebug)
                {
                    Debug.Log($"[RemotePlayerAnimator] Shoot trigger buffer expired");
                }
            }

            // Procesar buffer de salto
            if (jumpBufferFrames > 0)
            {
                jumpBufferFrames--;
                if (jumpBufferFrames == 0 && showDebug)
                {
                    Debug.Log($"[RemotePlayerAnimator] Jump trigger buffer expired");
                }
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Verifica si el Animator está inicializado correctamente
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Obtiene el Animator
        /// </summary>
        public Animator GetAnimator() => animator;
        #endregion
    }
}
