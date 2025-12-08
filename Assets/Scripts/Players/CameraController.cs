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
        [Tooltip("Intensidad del shake al disparar")]
        public float shootShakeIntensity = 0.08f;
        
        [Tooltip("Duración del shake al disparar")]
        public float shootShakeDuration = 0.15f;
        
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
        
        [Tooltip("Multiplicador global de shake")]
        [Range(0f, 2f)]
        public float globalShakeMultiplier = 1f;

        [Header("Debug")]
        [Tooltip("Mostrar gizmos de debug para el sistema de colisión")]
        public bool showDebugGizmos = false;
        #endregion
        
        #region Private Variables
        private Camera cam;
        private float normalFOV;
        
        // Estados
        private bool isAiming;
        private bool isSprinting;
        private bool isMoving;
        
        // Rotación vertical (el jugador ya rota horizontalmente)
        private float verticalRotation;
        
        // Shake system
        private float shakeTime;
        private float jumpShakeTimer;
        private float shootShakeTimer;
        private Vector3 currentShakeOffset;
        
        // Shoulder switching
        private bool isRightShoulder = true;
        private Vector3 currentShoulderOffset;
        private Vector3 targetShoulderOffset;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            cam = GetComponent<Camera>();
            normalFOV = cam.fieldOfView;
            
            if (playerRoot == null)
            {
                Debug.LogError("[CameraController] PlayerRoot not assigned!");
            }
            
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
        void HandleShoulderSwitch()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                isRightShoulder = !isRightShoulder;
                Transform targetCam = isRightShoulder ? rightShoulderCamera : leftShoulderCamera;
                Vector3 fallbackOffset = isRightShoulder ? rightShoulderOffset : leftShoulderOffset;
                targetShoulderOffset = targetCam != null ? targetCam.localPosition : fallbackOffset;
            }
            
            currentShoulderOffset = Vector3.Lerp(currentShoulderOffset, targetShoulderOffset, Time.deltaTime * shoulderSwitchSpeed);
        }

        void HandleCameraRotation()
        {
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            verticalRotation += mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
        }
        void UpdateCameraPositionAndRotation()
        {
            if (playerRoot == null) return;
            
            Vector3 forwardDirection = playerRoot.forward;
            Vector3 upOffset = Vector3.up * (targetHeight + Mathf.Tan(verticalRotation * Mathf.Deg2Rad) * targetDistance);
            Vector3 targetPosition = playerRoot.position + forwardDirection * targetDistance + upOffset;
            
            float verticalRadians = verticalRotation * Mathf.Deg2Rad;
            float yAdjustment = -Mathf.Sin(verticalRadians) * cameraVerticalAdjustment;
            float zAdjustment = -Mathf.Cos(verticalRadians) * cameraVerticalAdjustment + cameraVerticalAdjustment;
              Vector3 adjustedOffset = currentShoulderOffset + new Vector3(0, yAdjustment, zAdjustment);
            Vector3 desiredPosition = playerRoot.position + playerRoot.TransformDirection(adjustedOffset);
            
            Vector3 finalCameraPosition = desiredPosition;
            if (enableCameraCollision)
            {
                Vector3 pivotPoint = playerRoot.position + Vector3.up * targetHeight;
                
                Vector3 directionToCamera = desiredPosition - pivotPoint;
                float maxDistance = directionToCamera.magnitude;
                
                if (Physics.SphereCast(pivotPoint, collisionRadius, directionToCamera.normalized, out RaycastHit hit, maxDistance, collisionLayers))
                {
                    if (!hit.collider.transform.IsChildOf(playerRoot) && hit.collider.transform != playerRoot)
                    {
                        float safeDistance = Mathf.Max(hit.distance - collisionRadius * 0.5f, 0.1f);
                        finalCameraPosition = pivotPoint + directionToCamera.normalized * safeDistance;
                    }
                }
            }
            
            Vector3 shakeOffset = CalculateShakeOffset();
            Vector3 shakeInWorldSpace = transform.TransformDirection(shakeOffset);
            
            Vector3 targetPosWithShake = finalCameraPosition + shakeInWorldSpace;
            transform.position = Vector3.Lerp(transform.position, targetPosWithShake, Time.deltaTime * followSpeed);
            
            Vector3 lookDirection = targetPosition - transform.position;
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
            }
        }

        Vector3 CalculateShakeOffset()
        {
            currentShakeOffset = Vector3.zero;
            
            if (shootShakeTimer > 0)
            {
                shootShakeTimer -= Time.deltaTime;
                float progress = 1f - (shootShakeTimer / shootShakeDuration);
                float damping = Mathf.Sin(progress * Mathf.PI);
                
                float x = (Mathf.PerlinNoise(Time.time * 30f, 0f) - 0.5f) * 2f;
                float y = (Mathf.PerlinNoise(0f, Time.time * 30f) - 0.5f) * 2f;
                
                currentShakeOffset += new Vector3(x, y, 0) * shootShakeIntensity * damping;
            }
            
            if (jumpShakeTimer > 0)
            {
                jumpShakeTimer -= Time.deltaTime;
                float t = 1f - (jumpShakeTimer / jumpShakeDuration);
                float shake = Mathf.Sin(t * Mathf.PI * 2) * jumpShakeIntensity * (jumpShakeTimer / jumpShakeDuration);
                currentShakeOffset.y += shake;
            }
            
            if (isMoving)
            {
                shakeTime += Time.deltaTime * shakeFrequency;
                float intensity = isSprinting ? runShakeIntensity : walkShakeIntensity;
                currentShakeOffset.y += Mathf.Sin(shakeTime) * intensity;
                currentShakeOffset.x += Mathf.Sin(shakeTime * 0.5f) * intensity * 0.5f;
            }
            else
            {
                shakeTime = Mathf.Lerp(shakeTime, 0, Time.deltaTime * 5f);
            }
            
            return currentShakeOffset * globalShakeMultiplier;
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
        public void TriggerShootShake() => shootShakeTimer = shootShakeDuration;
        
        public Camera GetCamera() => cam;
        public float GetVerticalAngleNormalized() => verticalRotation / maxVerticalAngle;
        public float GetVerticalAngleDegrees() => verticalRotation;
        
        public Vector3 GetTargetPosition()
        {
            if (playerRoot == null) return transform.position + transform.forward * targetDistance;
            
            Vector3 forwardDirection = playerRoot.forward;
            Vector3 upOffset = Vector3.up * (targetHeight + Mathf.Tan(verticalRotation * Mathf.Deg2Rad) * targetDistance);
            return playerRoot.position + forwardDirection * targetDistance + upOffset;
        }
        #endregion

        #region Debug
        void OnDrawGizmos()
        {
            if (!showDebugGizmos || playerRoot == null || !Application.isPlaying) return;
            
            Vector3 pivotPoint = playerRoot.position + Vector3.up * targetHeight;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pivotPoint, 0.1f);
            
            float verticalRadians = verticalRotation * Mathf.Deg2Rad;
            float yAdjustment = -Mathf.Sin(verticalRadians) * cameraVerticalAdjustment;
            float zAdjustment = -Mathf.Cos(verticalRadians) * cameraVerticalAdjustment + cameraVerticalAdjustment;
            Vector3 adjustedOffset = currentShoulderOffset + new Vector3(0, yAdjustment, zAdjustment);
            Vector3 desiredPosition = playerRoot.position + playerRoot.TransformDirection(adjustedOffset);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(desiredPosition, 0.15f);
            
            Vector3 directionToCamera = desiredPosition - pivotPoint;
            float maxDistance = directionToCamera.magnitude;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pivotPoint, desiredPosition);
            
            if (enableCameraCollision)
            {
                if (Physics.SphereCast(pivotPoint, collisionRadius, directionToCamera.normalized, out RaycastHit hit, maxDistance, collisionLayers))
                {
                    if (!hit.collider.transform.IsChildOf(playerRoot) && hit.collider.transform != playerRoot)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(hit.point, collisionRadius);
                        
                        float safeDistance = Mathf.Max(hit.distance - collisionRadius * 0.5f, 0.1f);
                        Vector3 finalPos = pivotPoint + directionToCamera.normalized * safeDistance;
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawWireSphere(finalPos, 0.15f);
                        
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(pivotPoint, hit.point);
                    }
                }
            }
            
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
            
            Vector3 forwardDirection = playerRoot.forward;
            Vector3 upOffset = Vector3.up * (targetHeight + Mathf.Tan(verticalRotation * Mathf.Deg2Rad) * targetDistance);
            Vector3 targetPosition = playerRoot.position + forwardDirection * targetDistance + upOffset;
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
            Gizmos.DrawLine(transform.position, targetPosition);
        }
        #endregion
    }
}
