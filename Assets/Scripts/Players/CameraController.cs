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
        
        [Tooltip("Distancia de la cámara detrás del jugador")]
        [Range(1f, 10f)]
        public float cameraDistance = 3f;
        
        [Tooltip("Velocidad de seguimiento suave")]
        [Range(1f, 30f)]
        public float followSpeed = 10f;

    [Header("Rotation Settings")]
    [Tooltip("Sensibilidad del mouse")]
    public float mouseSensitivity = 2f;

    [Tooltip("Ángulo máximo hacia arriba (debe coincidir con TorsoAimController)")]
    public float maxVerticalAngle = 60f;

    [Tooltip("Ángulo máximo hacia abajo (debe coincidir con TorsoAimController)")]
    public float minVerticalAngle = -40f;        [Header("Camera Collision")]
        [Tooltip("Activar colisión de cámara con obstáculos")]
        public bool enableCameraCollision = true;
        
        [Tooltip("Radio del raycast para colisión")]
        public float collisionRadius = 0.3f;
        
        [Tooltip("Layers que bloquean la cámara")]
        public LayerMask collisionLayers = -1;

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
            UpdateCameraPositionAndRotation();
            UpdateZoom();
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
        }

        /// <summary>
        /// Actualiza posición y rotación de forma orbital alrededor del punto de los ojos
        /// </summary>
        void UpdateCameraPositionAndRotation()
        {
            if (playerRoot == null) return;
            
            // 1. Calcular rotación final (SOLO por input del mouse)
            Quaternion cameraRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);
            
            // 2. Punto objetivo (pivot): posición del jugador + offset en espacio local
            Vector3 pivotPosition = playerRoot.position + playerRoot.TransformDirection(cameraOffset);
            
            // 3. Posición deseada de la cámara: detrás del pivot según la rotación
            Vector3 desiredCameraPosition = pivotPosition - (cameraRotation * Vector3.forward * cameraDistance);
            
            // 4. Detectar colisiones con paredes
            Vector3 finalCameraPosition = desiredCameraPosition;
            if (enableCameraCollision)
            {
                Vector3 direction = desiredCameraPosition - pivotPosition;
                float distance = direction.magnitude;
                
                if (Physics.SphereCast(pivotPosition, collisionRadius, direction.normalized, out RaycastHit hit, distance, collisionLayers))
                {
                    // Ajustar distancia si hay obstrucción
                    finalCameraPosition = pivotPosition + direction.normalized * (hit.distance - collisionRadius);
                }
            }
            
            // 5. Aplicar shake en espacio LOCAL de la cámara
            Vector3 shakeOffset = CalculateShakeOffset();
            finalCameraPosition += cameraRotation * shakeOffset;
            
            // 6. Aplicar con suavizado
            transform.position = Vector3.Lerp(transform.position, finalCameraPosition, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, cameraRotation, Time.deltaTime * followSpeed);
            
            // 7. Hacer que la cámara siempre mire al pivot
            transform.LookAt(pivotPosition);
        }

        Vector3 CalculateShakeOffset()
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
