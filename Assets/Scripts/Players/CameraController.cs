using UnityEngine;

namespace BrickOps.Players
{
    /// <summary>
    /// Sistema de cámara en primera persona con rotación independiente de animaciones
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        #region Inspector Variables
        [Header("Follow Settings")]
        [Tooltip("Transform del JUGADOR ROOT (NO el hueso del torso)")]
        public Transform playerRoot;
        
        [Tooltip("Offset local desde el root del jugador (altura de ojos)")]
        public Vector3 cameraOffset = new Vector3(0f, 1.6f, 0f);
        
        [Tooltip("Velocidad de seguimiento suave")]
        [Range(1f, 30f)]
        public float followSpeed = 20f;

        [Header("Rotation Settings")]
        [Tooltip("Sensibilidad del mouse")]
        public float mouseSensitivity = 2f;

        [Tooltip("Ángulo máximo hacia arriba")]
        public float maxVerticalAngle = 80f;

        [Tooltip("Ángulo máximo hacia abajo")]
        public float minVerticalAngle = -80f;

        [Header("Zoom Settings")]
        [Tooltip("FOV normal")]
        public float normalFOV = 60f;

        [Tooltip("FOV al apuntar")]
        public float aimFOV = 40f;

        [Tooltip("FOV al sprintar")]
        public float sprintFOV = 70f;

        [Tooltip("Velocidad de transición del zoom")]
        public float zoomSpeed = 10f;

        [Header("Procedural Shake Settings")]
        [Tooltip("Intensidad del shake al caminar")]
        public float walkShakeIntensity = 0.005f;

        [Tooltip("Intensidad del shake al correr")]
        public float runShakeIntensity = 0.015f;
        
        [Tooltip("Intensidad del shake al saltar")]
        public float jumpShakeIntensity = 0.03f;
        
        [Tooltip("Frecuencia del shake")]
        public float shakeFrequency = 10f;
        
        [Tooltip("Duración del shake de salto")]
        public float jumpShakeDuration = 0.3f;
        #endregion
        
        #region Private Variables
        private Camera cam;
        private CameraShake cameraShake;
        
        // Estados
        private bool isAiming;
        private bool isSprinting;
        private bool isWalking;
        private bool isRunning;
        
        // Rotación (SOLO por input del jugador)
        private float verticalRotation;
        private float horizontalRotation;
        
        // Posición base (sin shake)
        private Vector3 targetPosition;
        
        // Shake procedural
        private float shakeTime;
        private float jumpShakeTimer;
        private bool isJumpShaking;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            cam = GetComponent<Camera>();
            cameraShake = GetComponent<CameraShake>();
            normalFOV = cam.fieldOfView;
            
            if (playerRoot == null)
            {
                Debug.LogError("[CameraController] PlayerRoot not assigned!");
            }
        }

        void LateUpdate()
        {
            HandleCameraRotation();
            UpdateCameraPosition();
            UpdateZoom();
            ApplyProceduralShake();
        }
        #endregion

        #region Camera Logic
        /// <summary>
        /// Maneja SOLO la rotación por input del mouse
        /// </summary>
        void HandleCameraRotation()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            // Rotación horizontal (Y global)
            horizontalRotation += mouseX;
            
            // Rotación vertical (X local) con clamp
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
            
            // Aplicar rotación SOLO por input (ignorar animaciones)
            transform.rotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);
        }

        /// <summary>
        /// Actualiza la posición base de la cámara (sin animaciones)
        /// </summary>
        void UpdateCameraPosition()
        {
            if (playerRoot == null) return;
            
            // Posición base: root del jugador + offset fijo
            // NO usa ningún hueso animado
            Vector3 desiredPosition = playerRoot.position + playerRoot.TransformDirection(cameraOffset);
            
            // Interpolación suave
            targetPosition = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);
        }

        void UpdateZoom()
        {
            float targetFOV = normalFOV;
            
            if (isAiming)
                targetFOV = aimFOV;
            else if (isSprinting)
                targetFOV = sprintFOV;
            
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }
        
        void ApplyProceduralShake()
        {
            Vector3 shakeOffset = Vector3.zero;
            
            // Shake de salto
            if (isJumpShaking)
            {
                jumpShakeTimer -= Time.deltaTime;
                if (jumpShakeTimer <= 0)
                {
                    isJumpShaking = false;
                }
                else
                {
                    float t = 1f - (jumpShakeTimer / jumpShakeDuration);
                    float shake = Mathf.Sin(t * Mathf.PI * 2) * jumpShakeIntensity * (jumpShakeTimer / jumpShakeDuration);
                    shakeOffset.y += shake;
                }
            }
            
            // Shake de movimiento
            if (isWalking || isRunning)
            {
                shakeTime += Time.deltaTime * shakeFrequency;
                
                float intensity = isRunning ? runShakeIntensity : walkShakeIntensity;
                shakeOffset.y += Mathf.Sin(shakeTime) * intensity;
                shakeOffset.x += Mathf.Sin(shakeTime * 0.5f) * intensity * 0.5f;
            }
            else
            {
                shakeTime = Mathf.Lerp(shakeTime, 0, Time.deltaTime * 5f);
            }
            
            // Combinar con shake de eventos
            if (cameraShake != null)
            {
                shakeOffset += cameraShake.GetCurrentShakeOffset();
            }
            
            // Aplicar: posición base + shake en espacio local
            transform.position = targetPosition + transform.TransformDirection(shakeOffset);
        }
        #endregion

        #region Public API
        public void SetAiming(bool aiming) => isAiming = aiming;
        public void SetSprinting(bool sprinting) => isSprinting = sprinting;
        public void SetMovementState(bool walking, bool running)
        {
            isWalking = walking;
            isRunning = running;
        }
        public void TriggerJumpShake()
        {
            isJumpShaking = true;
            jumpShakeTimer = jumpShakeDuration;
        }
        public Camera GetCamera() => cam;
        public float GetVerticalAngleNormalized() => verticalRotation / maxVerticalAngle;
        public float GetVerticalAngleDegrees() => verticalRotation;
        #endregion
    }
}
