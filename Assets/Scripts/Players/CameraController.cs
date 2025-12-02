using UnityEngine;

namespace BrickOps.Players
{
    /// <summary>
    /// Sistema de cámara third-person shooter con rotación orbital y cambio de hombro
    /// La cámara se posiciona en el hombro y siempre mira hacia un punto focal adelante del jugador
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        #region Inspector Variables
        [Header("Follow Settings")]
        [Tooltip("Transform del JUGADOR ROOT")]
        public Transform playerRoot;
        
        [Tooltip("Distancia focal desde el jugador (donde mira la cámara)")]
        [Range(5f, 50f)]
        public float targetDistance = 10f;
        
        [Tooltip("Altura del punto focal relativa al jugador")]
        public float targetHeight = 1.5f;
        
        [Tooltip("Ajuste de posición de cámara en Y/Z para mantener jugador en plano")]
        [Range(0f, 10f)]
        public float cameraVerticalAdjustment = 4f;
        
        [Tooltip("Velocidad de seguimiento suave")]
        [Range(1f, 30f)]
        public float followSpeed = 10f;

        [Header("Rotation Settings")]
        [Tooltip("Sensibilidad del mouse")]
        public float mouseSensitivity = 2f;

        [Tooltip("Ángulo máximo hacia arriba")]
        public float maxVerticalAngle = 60f;

        [Tooltip("Ángulo máximo hacia abajo")]
        public float minVerticalAngle = -40f;

        [Header("Camera Collision")]
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
        public Vector3 rightShoulderOffset = new Vector3(0.5f, 1.6f, -0.3f);
        
        [Tooltip("Offset hombro izquierdo (fallback si no hay transform)")]
        public Vector3 leftShoulderOffset = new Vector3(-0.5f, 1.6f, -0.3f);
        
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
        
        // Rotación vertical (el jugador ya rota horizontalmente)
        private float verticalRotation;
        
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
        /// Maneja SOLO la rotación vertical (InputManager controla la horizontal)
        /// </summary>
        void HandleCameraRotation()
        {
            // La sensibilidad se aplica automáticamente desde Input.GetAxis con mouseSensitivity
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            // Rotación vertical con clamp (+ para subir, - para bajar)
            verticalRotation += mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
        }

        /// <summary>
        /// Actualiza posición de la cámara en el hombro y la hace mirar hacia el punto focal
        /// </summary>
        void UpdateCameraPositionAndRotation()
        {
            if (playerRoot == null) return;
            
            // 1. Calcular punto focal (target) - donde debe mirar la cámara
            Vector3 forwardDirection = playerRoot.forward;
            Vector3 upOffset = Vector3.up * (targetHeight + Mathf.Tan(verticalRotation * Mathf.Deg2Rad) * targetDistance);
            Vector3 targetPosition = playerRoot.position + forwardDirection * targetDistance + upOffset;
            
            // 2. Ajustar offset de la cámara según rotación vertical para mantener jugador en plano
            float verticalRadians = verticalRotation * Mathf.Deg2Rad;
            float yAdjustment = -Mathf.Sin(verticalRadians) * cameraVerticalAdjustment;
            float zAdjustment = -Mathf.Cos(verticalRadians) * cameraVerticalAdjustment + cameraVerticalAdjustment;
            
            Vector3 adjustedOffset = currentShoulderOffset + new Vector3(0, yAdjustment, zAdjustment);
            Vector3 desiredPosition = playerRoot.position + playerRoot.TransformDirection(adjustedOffset);
            
            // 3. Sistema de colisión simplificado y efectivo
            Vector3 finalCameraPosition = desiredPosition;
            if (enableCameraCollision)
            {
                // Calcular dirección desde una posición segura cerca del jugador hacia la posición deseada
                Vector3 safeStartPosition = playerRoot.position + Vector3.up * targetHeight;
                Vector3 directionToCamera = desiredPosition - safeStartPosition;
                float distanceToCamera = directionToCamera.magnitude;
                
                // Raycast simple desde posición segura hacia cámara
                if (Physics.Raycast(safeStartPosition, directionToCamera.normalized, out RaycastHit hit, distanceToCamera, collisionLayers))
                {
                    // Si hay colisión, colocar cámara justo antes del obstáculo
                    finalCameraPosition = hit.point - directionToCamera.normalized * collisionRadius;
                }
            }
            
            // 4. Aplicar shake
            Vector3 shakeOffset = CalculateShakeOffset();
            finalCameraPosition += playerRoot.TransformDirection(shakeOffset);
            
            // 5. Aplicar posición suavizada
            transform.position = Vector3.Lerp(transform.position, finalCameraPosition, Time.deltaTime * followSpeed);
            
            // 6. Hacer que la cámara SIEMPRE mire al punto focal (target)
            Vector3 lookDirection = targetPosition - transform.position;
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
            }
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
        
        /// <summary>
        /// Obtiene el punto focal donde mira la cámara (útil para armas)
        /// </summary>
        public Vector3 GetTargetPosition()
        {
            if (playerRoot == null) return transform.position + transform.forward * targetDistance;
            
            Vector3 forwardDirection = playerRoot.forward;
            Vector3 upOffset = Vector3.up * (targetHeight + Mathf.Tan(verticalRotation * Mathf.Deg2Rad) * targetDistance);
            return playerRoot.position + forwardDirection * targetDistance + upOffset;
        }
        #endregion
    }
}
