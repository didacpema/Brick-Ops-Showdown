using UnityEngine;

namespace BrickOps.Players
{
    /// <summary>
    /// Controla la rotación vertical del torso (TorsoI) para apuntar arriba/abajo
    /// El torso rota en el eje X local siguiendo el ángulo de la cámara
    /// </summary>
    public class TorsoAimController : MonoBehaviour
    {
    #region Inspector Variables
    [Header("Torso Reference")]
    [Tooltip("Referencia al hueso TorsoI (padre de brazos, arma y cabeza)")]
    public Transform torsoTransform;

    [Tooltip("Referencia al Transform raíz del jugador (para pivote global)")]
    public Transform playerRootTransform;        [Header("Rotation Settings")]
        [Tooltip("Ángulo máximo de rotación hacia arriba (grados)")]
        [Range(0f, 90f)]
        public float maxUpAngle = 60f;

        [Tooltip("Ángulo máximo de rotación hacia abajo (grados)")]
        [Range(0f, 90f)]
        public float maxDownAngle = 40f;

        [Tooltip("Velocidad de interpolación de la rotación")]
        [Range(1f, 30f)]
        public float rotationSpeed = 15f;

        [Tooltip("Multiplicador del ángulo de la cámara (1 = rotación completa del torso)")]
        [Range(0f, 1f)]
        public float torsoInfluence = 0.8f;

        [Header("Debug")]
        [Tooltip("Mostrar información de debug en consola")]
        public bool showDebug = false;

        [Tooltip("Mostrar gizmos de debug en Scene view")]
        public bool showGizmos = true;
        #endregion

    #region Private Variables
    private CameraController cameraController;
    private Quaternion initialTorsoRotation;
    private float currentTorsoAngle = 0f;
    private bool isInitialized = false;
    #endregion        #region Initialization
        void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
        // Validar torso
        if (torsoTransform == null)
        {
            Debug.LogError("[TorsoAimController] ❌ TorsoI Transform no asignado!");
            return;
        }

        // Buscar o asignar player root
        if (playerRootTransform == null)
        {
            playerRootTransform = transform;
        }

        // Buscar CameraController
        cameraController = GetComponentInChildren<CameraController>();
        if (cameraController == null)
        {
            Debug.LogWarning("[TorsoAimController] ⚠ CameraController no encontrado!");
            return;
        }

        // Guardar rotación inicial del torso
        initialTorsoRotation = torsoTransform.localRotation;            isInitialized = true;
            Debug.Log("[TorsoAimController] ✓ Inicializado correctamente");
        }

        #region Unity Lifecycle
        void LateUpdate()
        {
            if (!isInitialized || cameraController == null)
                return;

            UpdateTorsoRotation();
        }
        #endregion

        #region Torso Logic
        /// <summary>
        /// Actualiza la rotación del torso basándose en el ángulo de la cámara
        /// </summary>
        void UpdateTorsoRotation()
        {
            // Obtener ángulo vertical de la cámara (en grados)
            float cameraAngle = cameraController.GetVerticalAngleDegrees();

            // Calcular ángulo objetivo del torso con influencia configurable
            float targetAngle = cameraAngle * torsoInfluence;

            // Clampear el ángulo del torso
            targetAngle = Mathf.Clamp(targetAngle, -maxDownAngle, maxUpAngle);

        // Interpolar suavemente hacia el ángulo objetivo
        currentTorsoAngle = Mathf.Lerp(currentTorsoAngle, targetAngle, Time.deltaTime * rotationSpeed);

        // Aplicar rotación en el eje X GLOBAL del player (no del hueso)
        // Primero aplicar la rotación inicial, luego rotar en el espacio WORLD
        torsoTransform.localRotation = initialTorsoRotation;
        torsoTransform.Rotate(playerRootTransform.right, currentTorsoAngle, Space.World);            // Debug opcional
            if (showDebug)
            {
                Debug.Log($"[TorsoAim] Camera: {cameraAngle:F1}° | Torso: {currentTorsoAngle:F1}° | Target: {targetAngle:F1}°");
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Obtiene el ángulo actual del torso en grados
        /// </summary>
        public float GetCurrentTorsoAngle()
        {
            return currentTorsoAngle;
        }

        /// <summary>
        /// Obtiene el ángulo normalizado del torso (-1 a 1)
        /// -1 = máximo abajo, 0 = horizontal, 1 = máximo arriba
        /// </summary>
        public float GetNormalizedTorsoAngle()
        {
            if (currentTorsoAngle > 0)
            {
                return currentTorsoAngle / maxUpAngle;
            }
            else
            {
                return currentTorsoAngle / -maxDownAngle;
            }
        }

        /// <summary>
        /// Resetea la rotación del torso a su estado inicial
        /// </summary>
        public void ResetTorsoRotation()
        {
            if (torsoTransform != null)
            {
                torsoTransform.localRotation = initialTorsoRotation;
                currentTorsoAngle = 0f;
            }
        }

        /// <summary>
        /// Verifica si el sistema está inicializado
        /// </summary>
        public bool IsInitialized()
        {
            return isInitialized;
        }
        #endregion

        #region Debug Gizmos
        void OnDrawGizmos()
        {
            if (!showGizmos || torsoTransform == null)
                return;

            // Dibujar línea desde el torso hacia adelante (dirección de apuntado)
            Vector3 torsoPosition = torsoTransform.position;
            Vector3 aimDirection = torsoTransform.forward;

            // Línea de dirección de apuntado
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(torsoPosition, aimDirection * 2f);

            // Esfera en el torso
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(torsoPosition, 0.1f);

            // Indicador de ángulo
            if (Application.isPlaying)
            {
                Gizmos.color = currentTorsoAngle > 0 ? Color.green : (currentTorsoAngle < 0 ? Color.red : Color.white);
                Gizmos.DrawWireSphere(torsoPosition + aimDirection * 2f, 0.15f);
            }
        }
        #endregion
    }
}
