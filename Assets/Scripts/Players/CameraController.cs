using UnityEngine;

namespace BrickOps.Players
{
    /// <summary>
    /// Sistema de cámara con zoom y efectos (posición y rotación se controlan desde el prefab)
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        #region Inspector Variables
        [Header("Zoom Settings")]
        [Tooltip("FOV normal")]
        public float normalFOV = 60f;

        [Tooltip("FOV al apuntar")]
        public float aimFOV = 40f;

        [Tooltip("FOV al sprintar (mayor FOV = sensación de velocidad)")]
        public float sprintFOV = 70f;

        [Tooltip("Velocidad de transición del zoom")]
        public float zoomSpeed = 10f;

        [Header("Camera Rotation Settings")]
        [Tooltip("Sensibilidad del mouse para rotación vertical")]
        public float verticalSensitivity = 2f;

        [Tooltip("Ángulo máximo de rotación hacia arriba")]
        public float maxVerticalAngle = 80f;

        [Tooltip("Ángulo máximo de rotación hacia abajo")]
        public float minVerticalAngle = -80f;

        [Header("Procedural Shake Settings")]
        [Tooltip("Intensidad del shake al caminar")]
        public float walkShakeIntensity = 0.005f;

        [Tooltip("Intensidad del shake al correr")]
        public float runShakeIntensity = 0.015f;

        [Tooltip("Intensidad del shake al saltar")]
        public float jumpShakeIntensity = 0.03f;

        [Tooltip("Frecuencia del shake (velocidad de la oscilación)")]
        public float shakeFrequency = 10f;

        [Tooltip("Duración del shake de salto")]
        public float jumpShakeDuration = 0.3f;
        #endregion

        #region Private Variables
        private Camera cam;
        private CameraShake cameraShake;
        private bool isAiming;
        private bool isSprinting;
        private bool isWalking;
        private bool isRunning;
        
        // Camera rotation
        private float verticalRotation;
        private Quaternion initialLocalRotation;
        
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
            initialLocalRotation = transform.localRotation;
            verticalRotation = transform.localEulerAngles.x;
        }

        void LateUpdate()
        {
            UpdateCameraRotation();
            UpdateZoom();
            UpdateProceduralShake();
        }
        #endregion

        #region Camera Logic
        void UpdateCameraRotation()
        {
            // Capturar input del mouse Y para rotación vertical
            float mouseY = Input.GetAxis("Mouse Y");
            
            if (Mathf.Abs(mouseY) > 0.001f)
            {
                verticalRotation -= mouseY * verticalSensitivity;
                verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
            }
            
            // Aplicar rotación local (solo en X para arriba/abajo)
            transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        void UpdateZoom()
        {
            float targetFOV;
            
            if (isAiming)
            {
                targetFOV = aimFOV;
            }
            else if (isSprinting)
            {
                targetFOV = sprintFOV;
            }
            else
            {
                targetFOV = normalFOV;
            }
            
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }

        void UpdateProceduralShake()
        {
            Vector3 proceduralShakeOffset = Vector3.zero;
            
            // Shake de salto (temporal)
            if (isJumpShaking)
            {
                jumpShakeTimer -= Time.deltaTime;
                if (jumpShakeTimer <= 0)
                {
                    isJumpShaking = false;
                }
                else
                {
                    float normalizedTime = 1f - (jumpShakeTimer / jumpShakeDuration);
                    float shake = Mathf.Sin(normalizedTime * Mathf.PI * 2) * jumpShakeIntensity * (jumpShakeTimer / jumpShakeDuration);
                    proceduralShakeOffset.y += shake;
                }
            }
            
            // Shake continuo (caminar/correr)
            if (isWalking || isRunning)
            {
                shakeTime += Time.deltaTime * shakeFrequency;
                
                float intensity = isRunning ? runShakeIntensity : walkShakeIntensity;
                
                // Oscilación vertical (bobbing)
                proceduralShakeOffset.y += Mathf.Sin(shakeTime) * intensity;
                
                // Oscilación horizontal sutil
                proceduralShakeOffset.x += Mathf.Sin(shakeTime * 0.5f) * intensity * 0.5f;
            }
            else
            {
                // Reset gradual del tiempo cuando no se mueve
                shakeTime = Mathf.Lerp(shakeTime, 0, Time.deltaTime * 5f);
            }
            
            // Combinar shake procedural con shake de eventos (CameraShake)
            Vector3 finalOffset = proceduralShakeOffset;
            Vector3 originalPosition;
            
            if (cameraShake != null)
            {
                finalOffset += cameraShake.GetCurrentShakeOffset();
                originalPosition = cameraShake.GetOriginalPosition();
            }
            else
            {
                originalPosition = transform.localPosition;
            }
            
            // Aplicar el shake combinado
            transform.localPosition = originalPosition + finalOffset;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Establece el estado de aiming
        /// </summary>
        public void SetAiming(bool aiming)
        {
            isAiming = aiming;
        }

        /// <summary>
        /// Establece el estado de sprint
        /// </summary>
        public void SetSprinting(bool sprinting)
        {
            isSprinting = sprinting;
        }

        /// <summary>
        /// Establece el estado de movimiento
        /// </summary>
        public void SetMovementState(bool walking, bool running)
        {
            isWalking = walking;
            isRunning = running;
        }

        /// <summary>
        /// Dispara el shake de salto
        /// </summary>
        public void TriggerJumpShake()
        {
            isJumpShaking = true;
            jumpShakeTimer = jumpShakeDuration;
        }

        /// <summary>
        /// Obtiene la cámara
        /// </summary>
        public Camera GetCamera()
        {
            return cam;
        }

        /// <summary>
        /// Obtiene el ángulo vertical actual de la cámara (normalizado de -1 a 1)
        /// -1 = mirando hacia abajo, 0 = horizontal, 1 = mirando hacia arriba
        /// </summary>
        public float GetVerticalAngleNormalized()
        {
            return verticalRotation / maxVerticalAngle;
        }

        /// <summary>
        /// Obtiene el ángulo vertical actual en grados
        /// </summary>
        public float GetVerticalAngleDegrees()
        {
            return verticalRotation;
        }
        #endregion
    }
}
