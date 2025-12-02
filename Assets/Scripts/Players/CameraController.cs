using UnityEngine;

namespace BrickOps.Players
{
    /// <summary>
    /// Sistema de cámara third-person con rotación orbital y cambio de hombro
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        #region Inspector Variables
        [Header("Follow Settings")]
        [Tooltip("Transform del JUGADOR ROOT")]
        public Transform playerRoot;
        
        [Tooltip("Distancia de la cámara detrás del jugador")]
        [Range(1f, 10f)]
        public float cameraDistance = 3f;
        
        [Tooltip("Velocidad de seguimiento suave")]
        [Range(1f, 30f)]
        public float followSpeed = 10f;

        [Header("Rotation Settings")]
        [Tooltip("Sensibilidad del mouse")]
        public float mouseSensitivity = 2f;

        [Tooltip("Ángulo máximo hacia arriba")]
        public float maxVerticalAngle = 60f;

        [Tooltip("Ángulo máximo hacia abajo")]
        public float minVerticalAngle = -40f;        [Header("Camera Collision")]
        [Tooltip("Activar colisión de cámara con obstáculos")]
        public bool enableCameraCollision = true;
        
        [Tooltip("Radio del raycast para colisión")]
        public float collisionRadius = 0.3f;
        
        [Tooltip("Layers que bloquean la cámara")]
        public LayerMask collisionLayers = -1;

        [Header("Shoulder Switch Settings")]
        [Tooltip("Transform de la cámara en hombro derecho")]
        public Transform rightShoulderCamera;
        
        [Tooltip("Transform de la cámara en hombro izquierdo")]
        public Transform leftShoulderCamera;
        
        [Tooltip("Offset hombro derecho (fallback si no hay transform)")]
        public Vector3 rightShoulderOffset = new Vector3(0.5f, 1.6f, 0f);
        
        [Tooltip("Offset hombro izquierdo (fallback si no hay transform)")]
        public Vector3 leftShoulderOffset = new Vector3(-0.5f, 1.6f, 0f);
        
        [Tooltip("Velocidad de transición entre hombros")]
        [Range(1f, 20f)]
        public float shoulderSwitchSpeed = 10f;

        [Header("Zoom Settings")]
        [Tooltip("FOV al apuntar")]
        public float aimFOV = 40f;

        [Tooltip("FOV al sprintar")]
        public float sprintFOV = 70f;

        [Tooltip("Velocidad de transición del zoom")]
        public float zoomSpeed = 10f;

        [Header("Shake Settings")]
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
        private float normalFOV;
        
        // Estados
        private bool isAiming;
        private bool isSprinting;
        private bool isMoving;
        
        // Rotación
        private float verticalRotation;
        private float horizontalRotation;
        
        // Shake procedural
        private float shakeTime;
        private float jumpShakeTimer;
        
        // Shoulder switching
        private bool isRightShoulder = true;
        private Vector3 currentShoulderOffset;
        private Vector3 targetShoulderOffset;
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
            
            // Inicializar offsets de hombro
            currentShoulderOffset = rightShoulderCamera != null ? rightShoulderCamera.localPosition : rightShoulderOffset;
            targetShoulderOffset = currentShoulderOffset;
        }

        void LateUpdate()
        {
            HandleShoulderSwitch();
            HandleCameraRotation();
            UpdateCameraPositionAndRotation();
            UpdateZoom();
        }
        #endregion

        #region Camera Logic
        /// <summary>
        /// Maneja el cambio de hombro al presionar Q
        /// </summary>
        void HandleShoulderSwitch()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                isRightShoulder = !isRightShoulder;
                Transform targetCam = isRightShoulder ? rightShoulderCamera : leftShoulderCamera;
                Vector3 fallbackOffset = isRightShoulder ? rightShoulderOffset : leftShoulderOffset;
                targetShoulderOffset = targetCam != null ? targetCam.localPosition : fallbackOffset;
            }
            
            currentShoulderOffset = Vector3.Lerp(currentShoulderOffset, targetShoulderOffset, Time.deltaTime * shoulderSwitchSpeed);
        }

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
        }

        /// <summary>
        /// Actualiza posición y rotación de forma orbital alrededor del punto de los ojos
        /// </summary>
        void UpdateCameraPositionAndRotation()
        {
            if (playerRoot == null) return;
            
            // 1. Calcular rotación de la cámara
            Quaternion cameraRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);
            
            // 2. Punto pivot (donde mira la cámara)
            Vector3 pivotPosition = playerRoot.position + playerRoot.TransformDirection(currentShoulderOffset);
            
            // 3. Posición deseada de la cámara (orbital)
            Vector3 desiredPosition = pivotPosition - (cameraRotation * Vector3.forward * cameraDistance);
            
            // 4. Colisión con paredes
            if (enableCameraCollision)
            {
                Vector3 direction = desiredPosition - pivotPosition;
                float distance = direction.magnitude;
                
                if (Physics.SphereCast(pivotPosition, collisionRadius, direction.normalized, out RaycastHit hit, distance, collisionLayers))
                {
                    desiredPosition = pivotPosition + direction.normalized * (hit.distance - collisionRadius);
                }
            }
            
            // 5. Aplicar shake
            Vector3 shakeOffset = CalculateShakeOffset();
            desiredPosition += cameraRotation * shakeOffset;
            
            // 6. Aplicar posición y rotación suavizada
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, cameraRotation, Time.deltaTime * followSpeed);
        }

        Vector3 CalculateShakeOffset()
        {
            Vector3 shakeOffset = Vector3.zero;
            
            // Shake de salto
            if (jumpShakeTimer > 0)
            {
                jumpShakeTimer -= Time.deltaTime;
                float t = 1f - (jumpShakeTimer / jumpShakeDuration);
                float shake = Mathf.Sin(t * Mathf.PI * 2) * jumpShakeIntensity * (jumpShakeTimer / jumpShakeDuration);
                shakeOffset.y += shake;
            }
            
            // Shake de movimiento
            if (isMoving)
            {
                shakeTime += Time.deltaTime * shakeFrequency;
                float intensity = isSprinting ? runShakeIntensity : walkShakeIntensity;
                shakeOffset.y += Mathf.Sin(shakeTime) * intensity;
                shakeOffset.x += Mathf.Sin(shakeTime * 0.5f) * intensity * 0.5f;
            }
            else
            {
                shakeTime = Mathf.Lerp(shakeTime, 0, Time.deltaTime * 5f);
            }
            
            // Combinar con shake de eventos (disparo, impactos)
            if (cameraShake != null)
            {
                shakeOffset += cameraShake.GetCurrentShakeOffset();
            }
            
            return shakeOffset;
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
        #endregion

        #region Public API
        public void SetAiming(bool aiming) => isAiming = aiming;
        public void SetSprinting(bool sprinting) => isSprinting = sprinting;
        public void SetMovementState(bool moving, bool sprinting)
        {
            isMoving = moving;
            isSprinting = sprinting;
        }
        public void TriggerJumpShake() => jumpShakeTimer = jumpShakeDuration;
        public Camera GetCamera() => cam;
        public float GetVerticalAngleNormalized() => verticalRotation / maxVerticalAngle;
        public float GetVerticalAngleDegrees() => verticalRotation;
        #endregion
    }
}
