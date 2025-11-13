using UnityEngine;

namespace BrickOps.Players
{
    /// <summary>
    /// Sistema de cámara que sigue al torso del jugador con zoom y efectos
    /// La cámara mantiene al torso en encuadre incluso cuando este rota verticalmente
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        #region Inspector Variables        [Header("Torso Follow Settings")]
        [Tooltip("Transform del torso a seguir (TorsoI)")]
        public Transform torsoTarget;
        
        [Tooltip("Transform de offset de cámara (hijo del torso, marca posición y rotación base)")]
        public Transform cameraOffsetTransform;
        
        [Tooltip("Velocidad de seguimiento del torso (mayor = más pegada)")]
        [Range(1f, 30f)]
        public float followSpeed = 15f;
        
        [Tooltip("Mantener la cámara siguiendo el torso incluso cuando rota verticalmente")]
        public bool followTorsoRotation = true;        [Tooltip("Compensación vertical adicional para mantener al jugador en frame al apuntar arriba/abajo")]
        [Range(0f, 2f)]
        public float verticalFramingOffset = 0.5f;

        [Tooltip("Reducir seguimiento durante disparo (0 = no seguir, 1 = seguir normal)")]
        [Range(0f, 1f)]
        public float shootingFollowDamping = 0.3f;

        [Tooltip("Duración del damping después de disparar")]
        public float shootingDampDuration = 0.3f;

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
        public float walkShakeIntensity = 0.005f;        [Tooltip("Intensidad del shake al correr")]
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
          // Camera follow
        private Vector3 baseOffset;
        private bool hasOffsetTransform;
        private Vector3 targetFollowPosition; // Posición base calculada antes de aplicar shake
        
        // Shooting damping
        private float shootingDampTimer;
        private bool isShooting;
        
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

            // Si hay offset transform, usarlo como base
            if (cameraOffsetTransform != null)
            {
                hasOffsetTransform = true;
                baseOffset = cameraOffsetTransform.localPosition;
            }
            else
            {
                hasOffsetTransform = false;
                baseOffset = transform.localPosition;
            }
        }

        void LateUpdate()
        {
            UpdateCameraFollow();
            UpdateCameraRotation();
            UpdateZoom();
            UpdateProceduralShake();
        }
        #endregion

        #region Camera Logic
        /// <summary>
        /// Actualiza la posición de la cámara para seguir al torso
        /// </summary>
        void UpdateCameraFollow()
        {
            if (torsoTarget == null)
                return;

            Vector3 targetPosition;
            Quaternion targetRotation;

            if (hasOffsetTransform && cameraOffsetTransform != null)
            {
                // Usar la posición y rotación del offset transform
                targetPosition = cameraOffsetTransform.position;
                
                if (followTorsoRotation)
                {
                    // La cámara hereda la rotación base del offset (que sigue al torso)
                    targetRotation = cameraOffsetTransform.rotation * Quaternion.Euler(verticalRotation, 0f, 0f);
                }
                else
                {
                    // Solo usar la posición del offset, no la rotación
                    targetRotation = transform.parent.rotation * Quaternion.Euler(verticalRotation, 0f, 0f);
                }
            }
            else
            {
                // Fallback: posicionarse relativo al torso
                targetPosition = torsoTarget.position + torsoTarget.TransformDirection(baseOffset);
                
                if (followTorsoRotation)
                {
                    targetRotation = torsoTarget.rotation * initialLocalRotation * Quaternion.Euler(verticalRotation, 0f, 0f);
                }
                else
                {
                    targetRotation = transform.parent.rotation * Quaternion.Euler(verticalRotation, 0f, 0f);
                }            }

            // Aplicar compensación vertical para mantener al jugador en encuadre
            // Cuando apunta arriba, la cámara se mueve hacia arriba
            // Cuando apunta abajo, la cámara se mueve hacia abajo
            float normalizedVertical = verticalRotation / maxVerticalAngle; // -1 a 1
            Vector3 framingCompensation = Vector3.up * normalizedVertical * verticalFramingOffset;
            targetPosition += framingCompensation;

            // Interpolar suavemente hacia la posición objetivo
            targetFollowPosition = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
            transform.position = targetFollowPosition; // Guardar la posición base sin shake
            
            // Aplicar rotación (sin interpolación para mantener responsividad)
            if (followTorsoRotation)
            {
                transform.rotation = targetRotation;
            }
        }

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
            }            else
            {
                // Reset gradual del tiempo cuando no se mueve
                shakeTime = Mathf.Lerp(shakeTime, 0, Time.deltaTime * 5f);
            }
            
            // Combinar shake procedural con shake de eventos (impacto, disparo, etc)
            Vector3 totalShakeOffset = proceduralShakeOffset;
            
            if (cameraShake != null)
            {
                totalShakeOffset += cameraShake.GetCurrentShakeOffset();
            }
            
            // Aplicar shake SUMANDO sobre la posición base guardada (no acumula)
            // Siempre partimos de targetFollowPosition y le sumamos el shake actual
            transform.position = targetFollowPosition + transform.TransformDirection(totalShakeOffset);
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
