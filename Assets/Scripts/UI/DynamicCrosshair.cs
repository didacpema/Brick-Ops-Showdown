using UnityEngine;
using UnityEngine.UI;

namespace BrickOps.UI
{
    /// <summary>
    /// Crosshair dinámico que se expande/contrae según el spread del arma
    /// Similar al sistema de Valorant
    /// </summary>
    public class DynamicCrosshair : MonoBehaviour
    {
        #region Inspector Variables
        [Header("Crosshair Lines")]
        [Tooltip("Línea superior del crosshair")]
        public RectTransform topLine;
        
        [Tooltip("Línea inferior del crosshair")]
        public RectTransform bottomLine;
        
        [Tooltip("Línea izquierda del crosshair")]
        public RectTransform leftLine;
        
        [Tooltip("Línea derecha del crosshair")]
        public RectTransform rightLine;
        
        [Tooltip("Punto central (opcional)")]
        public RectTransform centerDot;        [Header("Crosshair Settings - Gap por Estado")]
        [Tooltip("Gap cuando está quieto y apuntando")]
        public float aimingGap = 3f;
        
        [Tooltip("Gap cuando está quieto sin apuntar")]
        public float idleGap = 8f;
        
        [Tooltip("Gap cuando está andando")]
        public float walkingGap = 15f;
        
        [Tooltip("Gap cuando está corriendo")]
        public float runningGap = 25f;
        
        [Tooltip("Gap cuando está saltando")]
        public float jumpingGap = 30f;
        
        [Tooltip("Velocidad de transición (mayor = más rápido)")]
        [Range(1f, 30f)]
        public float smoothSpeed = 15f;

        [Header("Visual Settings")]
        [Tooltip("Grosor de las líneas")]
        public float lineThickness = 2f;
        
        [Tooltip("Longitud de las líneas")]
        public float lineLength = 8f;
        
        [Tooltip("Color del crosshair")]
        public Color crosshairColor = Color.white;
        
        [Tooltip("Opacidad del crosshair")]
        [Range(0f, 1f)]
        public float crosshairOpacity = 1f;        [Header("References")]
        [Tooltip("WeaponController del jugador local")]
        public WeaponController weaponController;
        
        [Tooltip("InputManager del jugador (opcional, para detectar salto)")]
        public InputManager inputManager;
        
        [Tooltip("Cámara del jugador (para proyectar posiciones 3D a pantalla)")]
        public Camera playerCamera;
        #endregion

        #region Private Variables
        private float currentGap;
        private float targetGap;
        private Image[] lineImages;
        private Image centerDotImage;
        private Canvas parentCanvas;
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            InitializeCrosshair();
            parentCanvas = GetComponentInParent<Canvas>();
        }        void Update()
        {
            // Intentar auto-asignar WeaponController si aún no está asignado
            if (weaponController == null)
            {
                weaponController = FindAnyObjectByType<WeaponController>();
                if (weaponController != null)
                {
                    Debug.Log("[DynamicCrosshair] WeaponController encontrado y conectado automáticamente.");
                    playerCamera = weaponController.playerCamera;
                }
                return;
            }
            
            // Intentar auto-asignar InputManager si aún no está asignado
            if (inputManager == null)
            {
                inputManager = FindAnyObjectByType<InputManager>();
            }
            
            // Auto-asignar cámara si falta
            if (playerCamera == null && weaponController != null)
            {
                playerCamera = weaponController.playerCamera;
            }
            
            UpdateCrosshairSpread();
            UpdateCenterDotPosition();
        }
        #endregion

        #region Initialization
        void InitializeCrosshair()
        {
            // Si no hay líneas creadas, crearlas automáticamente
            if (topLine == null || bottomLine == null || leftLine == null || rightLine == null)
            {
                CreateCrosshairLines();
            }
            
            // Obtener componentes Image
            lineImages = new Image[4];
            if (topLine != null) lineImages[0] = topLine.GetComponent<Image>();
            if (bottomLine != null) lineImages[1] = bottomLine.GetComponent<Image>();
            if (leftLine != null) lineImages[2] = leftLine.GetComponent<Image>();
            if (rightLine != null) lineImages[3] = rightLine.GetComponent<Image>();
            
            if (centerDot != null)
            {
                centerDotImage = centerDot.GetComponent<Image>();
            }
              // Aplicar estilo visual
            ApplyVisualSettings();
            
            currentGap = idleGap;
        }void CreateCrosshairLines()
        {
            // Crear línea superior
            topLine = CreateLine("Top", new Vector2(lineThickness, lineLength));
            topLine.anchoredPosition = new Vector2(0, idleGap + lineLength / 2);
            
            // Crear línea inferior
            bottomLine = CreateLine("Bottom", new Vector2(lineThickness, lineLength));
            bottomLine.anchoredPosition = new Vector2(0, -(idleGap + lineLength / 2));
            
            // Crear línea izquierda
            leftLine = CreateLine("Left", new Vector2(lineLength, lineThickness));
            leftLine.anchoredPosition = new Vector2(-(idleGap + lineLength / 2), 0);
            
            // Crear línea derecha
            rightLine = CreateLine("Right", new Vector2(lineLength, lineThickness));
            rightLine.anchoredPosition = new Vector2(idleGap + lineLength / 2, 0);
            
            // Crear punto central (opcional)
            if (centerDot == null)
            {
                GameObject dotObj = new GameObject("CenterDot");
                dotObj.transform.SetParent(transform, false);
                centerDot = dotObj.AddComponent<RectTransform>();
                centerDot.sizeDelta = new Vector2(2f, 2f);
                centerDot.anchoredPosition = Vector2.zero;
                
                Image img = dotObj.AddComponent<Image>();
                img.color = crosshairColor;
            }
            
            Debug.Log("[DynamicCrosshair] Líneas del crosshair creadas automáticamente");
        }

        RectTransform CreateLine(string name, Vector2 size)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.SetParent(transform, false);
            
            RectTransform rect = lineObj.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            
            Image img = lineObj.AddComponent<Image>();
            img.color = crosshairColor;
            
            return rect;
        }

        void ApplyVisualSettings()
        {
            Color finalColor = crosshairColor;
            finalColor.a = crosshairOpacity;
            
            // Aplicar a las líneas
            foreach (Image img in lineImages)
            {
                if (img != null)
                {
                    img.color = finalColor;
                }
            }
            
            // Aplicar al punto central
            if (centerDotImage != null)
            {
                centerDotImage.color = finalColor;
            }
              // Actualizar tamaños
            //if (topLine != null) topLine.sizeDelta = new Vector2(lineThickness, lineLength);
            //if (bottomLine != null) bottomLine.sizeDelta = new Vector2(lineThickness, lineLength);
            //if (leftLine != null) leftLine.sizeDelta = new Vector2(lineLength, lineThickness);
            //if (rightLine != null) rightLine.sizeDelta = new Vector2(lineLength, lineThickness);
        }
        #endregion

        #region Crosshair Update
        void UpdateCrosshairSpread()
        {
            // Determinar el gap objetivo según el estado del jugador
            targetGap = GetTargetGapForCurrentState();
            
            // Suavizar la transición
            currentGap = Mathf.Lerp(currentGap, targetGap, Time.deltaTime * smoothSpeed);
            
            // Actualizar posiciones de las líneas
            UpdateLinePositions();
        }        float GetTargetGapForCurrentState()
        {
            // Prioridad 1: Si está saltando (no está en el suelo)
            if (inputManager != null && !inputManager.IsGrounded())
            {
                return jumpingGap;
            }
            
            // Obtener el spread actual para determinar el estado
            float currentSpread = weaponController.GetCurrentSpread();
            
            // Determinar estado basándose en el spread del arma
            // jumpingSpread = 0.1
            // runningSpread = 0.08
            // walkingSpread = 0.04
            // walkingAimSpread = 0.015
            // standingSpread = 0.02
            // standingAimSpread = 0.005
            
            if (currentSpread >= 0.1f)
            {
                // Saltando (jumpingSpread = 0.1)
                return jumpingGap;
            }
            else if (currentSpread >= 0.08f)
            {
                // Corriendo (runningSpread = 0.08)
                return runningGap;
            }
            else if (currentSpread >= 0.04f)
            {
                // Andando (walkingSpread = 0.04)
                return walkingGap;
            }
            else if (currentSpread >= 0.02f)
            {
                // Quieto (standingSpread = 0.02)
                return idleGap;
            }
            else if (currentSpread >= 0.015f)
            {
                // Andando + Apuntando (walkingAimSpread = 0.015)
                return aimingGap + (walkingGap - aimingGap) * 0.5f;
            }
            else
            {
                // Quieto + Apuntando (standingAimSpread = 0.005)
                return aimingGap;
            }
        }

        void UpdateLinePositions()
        {
            // Las barras permanecen en el centro, solo cambia su separación (gap)
            if (topLine != null)
            {
                topLine.anchoredPosition = new Vector2(0, currentGap + lineLength / 2);
            }
            
            if (bottomLine != null)
            {
                bottomLine.anchoredPosition = new Vector2(0, -(currentGap + lineLength / 2));
            }
            
            if (leftLine != null)
            {
                leftLine.anchoredPosition = new Vector2(-(currentGap + lineLength / 2), 0);
            }
              if (rightLine != null)
            {
                rightLine.anchoredPosition = new Vector2(currentGap + lineLength / 2, 0);
            }
            
            // El centerDot NO se mueve aquí, se actualiza en UpdateCenterDotPosition()
        }
        
        /// <summary>
        /// Actualiza la posición del dot central basándose en un raycast desde el muzzle
        /// </summary>
        void UpdateCenterDotPosition()
        {
            if (centerDot == null || weaponController == null || playerCamera == null) return;
            
            Transform muzzle = weaponController.muzzlePoint;
            if (muzzle == null) return;
            
            // Calcular dirección perfecta desde cámara (sin spread)
            Ray cameraRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            Vector3 targetPoint;
            
            // Primero hacer raycast desde la cámara para encontrar el punto objetivo
            if (Physics.Raycast(cameraRay, out RaycastHit cameraHit, weaponController.range, weaponController.hitLayers))
            {
                targetPoint = cameraHit.point;
            }
            else
            {
                targetPoint = playerCamera.transform.position + playerCamera.transform.forward * weaponController.range;
            }
            
            // Ahora hacer raycast desde el muzzle hacia ese punto
            Vector3 directionFromMuzzle = (targetPoint - muzzle.position).normalized;
            Vector3 finalHitPoint;
            
            if (Physics.Raycast(muzzle.position, directionFromMuzzle, out RaycastHit muzzleHit, weaponController.range, weaponController.hitLayers))
            {
                // Hay algo entre el muzzle y el target
                finalHitPoint = muzzleHit.point;
            }
            else
            {
                // No hay obstáculos, usar el punto objetivo
                finalHitPoint = targetPoint;
            }
            
            // Proyectar el punto 3D a coordenadas de pantalla
            Vector3 screenPoint = playerCamera.WorldToScreenPoint(finalHitPoint);
            
            // Convertir a coordenadas del canvas
            if (parentCanvas != null)
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    screenPoint,
                    parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : playerCamera,
                    out localPoint
                );
                
                // Aplicar la posición al dot
                centerDot.anchoredPosition = localPoint;
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Asigna el WeaponController manualmente
        /// </summary>
        public void SetWeaponController(WeaponController controller)
        {
            weaponController = controller;
        }

        /// <summary>
        /// Actualiza la configuración visual del crosshair en runtime
        /// </summary>
        public void UpdateVisuals()
        {
            ApplyVisualSettings();
        }

        /// <summary>
        /// Muestra u oculta el crosshair
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
        #endregion

        #region Editor
        void OnValidate()
        {
            // Aplicar cambios en tiempo de edición
            if (Application.isPlaying && lineImages != null)
            {
                ApplyVisualSettings();
            }
            
            // Asegurar que el dot empiece en el centro en el editor
            if (!Application.isPlaying && centerDot != null)
            {
                centerDot.anchoredPosition = Vector2.zero;
            }
        }
        #endregion
    }
}
