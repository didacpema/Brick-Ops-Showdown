using UnityEngine;
using UnityEngine.UI;

namespace BrickOps.UI
{
    /// <summary>
    /// Crosshair dinámico que se expande/contrae según el spread del arma
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
        
        [Header("Shoot Expansion Settings")]
        [Tooltip("Expansión adicional del gap al disparar")]
        public float shootExpansion = 5f;
        
        [Tooltip("Duración de la expansión por disparo (segundos)")]
        public float shootExpansionDuration = 0.1f;
        
        [Tooltip("Velocidad de recuperación de la expansión")]
        [Range(1f, 30f)]
        public float shootRecoverySpeed = 8f;

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
        
        // Sistema de expansión por disparo
        private float shootExpansionTimer;
        private float currentShootExpansion;
        private int lastShootCount = -1;
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            InitializeCrosshair();
            parentCanvas = GetComponentInParent<Canvas>();
        }        void Update()
        {
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
            
            if (inputManager == null)
            {
                inputManager = FindAnyObjectByType<InputManager>();
            }
            
            if (playerCamera == null && weaponController != null)
            {
                playerCamera = weaponController.playerCamera;
            }
            
            bool isAiming = inputManager != null && inputManager.IsAiming();
            SetLinesVisible(isAiming);
            
            UpdateCrosshairSpread();
            UpdateShootExpansion();
            UpdateCenterDotPosition();
        }
        #endregion

        #region Initialization
        void InitializeCrosshair()
        {
            if (topLine == null || bottomLine == null || leftLine == null || rightLine == null)
            {
                CreateCrosshairLines();
            }
            
            lineImages = new Image[4];
            if (topLine != null) lineImages[0] = topLine.GetComponent<Image>();
            if (bottomLine != null) lineImages[1] = bottomLine.GetComponent<Image>();
            if (leftLine != null) lineImages[2] = leftLine.GetComponent<Image>();
            if (rightLine != null) lineImages[3] = rightLine.GetComponent<Image>();
            
            if (centerDot != null)
            {
                centerDotImage = centerDot.GetComponent<Image>();
            }
            ApplyVisualSettings();
            
            currentGap = idleGap;
        }void CreateCrosshairLines()
        {
            topLine = CreateLine("Top", new Vector2(lineThickness, lineLength));
            topLine.anchoredPosition = new Vector2(0, idleGap + lineLength / 2);
            
            bottomLine = CreateLine("Bottom", new Vector2(lineThickness, lineLength));
            bottomLine.anchoredPosition = new Vector2(0, -(idleGap + lineLength / 2));
            
            leftLine = CreateLine("Left", new Vector2(lineLength, lineThickness));
            leftLine.anchoredPosition = new Vector2(-(idleGap + lineLength / 2), 0);
            
            rightLine = CreateLine("Right", new Vector2(lineLength, lineThickness));
            rightLine.anchoredPosition = new Vector2(idleGap + lineLength / 2, 0);
            
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
            
            foreach (Image img in lineImages)
            {
                if (img != null)
                {
                    img.color = finalColor;
                }
            }
            
            if (centerDotImage != null)
            {
                centerDotImage.color = finalColor;
            }
        }
        #endregion

        #region Crosshair Update
        void UpdateCrosshairSpread()
        {
            targetGap = GetTargetGapForCurrentState();
            
            currentGap = Mathf.Lerp(currentGap, targetGap, Time.deltaTime * smoothSpeed);
            
            UpdateLinePositions();
        }
        
        void UpdateShootExpansion()
        {
            if (weaponController == null) return;
            
            int currentShootCount = GetCurrentShootCount();
            if (lastShootCount != -1 && currentShootCount > lastShootCount)
            {
                shootExpansionTimer = shootExpansionDuration;
                currentShootExpansion = shootExpansion;
            }
            lastShootCount = currentShootCount;
            
            if (shootExpansionTimer > 0)
            {
                shootExpansionTimer -= Time.deltaTime;
            }
            
            float targetExpansion = shootExpansionTimer > 0 ? shootExpansion : 0f;
            currentShootExpansion = Mathf.Lerp(currentShootExpansion, targetExpansion, Time.deltaTime * shootRecoverySpeed);
        }
        
        int GetCurrentShootCount()
        {
            if (inputManager != null)
            {
                var state = inputManager.GetCurrentPlayerState(0);
                if (state != null)
                {
                    return state.shootCount;
                }
            }
            return 0;
        }        float GetTargetGapForCurrentState()
        {
            if (inputManager != null && !inputManager.IsGrounded())
            {
                return jumpingGap;
            }
            
            float currentSpread = weaponController.GetCurrentSpread();
            
            if (currentSpread >= 0.1f)
            {
                return jumpingGap;
            }
            else if (currentSpread >= 0.08f)
            {
                return runningGap;
            }
            else if (currentSpread >= 0.04f)
            {
                return walkingGap;
            }
            else if (currentSpread >= 0.02f)
            {
                return idleGap;
            }
            else if (currentSpread >= 0.015f)
            {
                return aimingGap + (walkingGap - aimingGap) * 0.5f;
            }
            else
            {
                return aimingGap;
            }
        }

        void UpdateLinePositions()
        {
            if (weaponController == null || playerCamera == null) return;
            
            Vector3 actualImpactPoint = weaponController.GetActualBulletImpactPoint();
            
            Vector3 screenPoint = playerCamera.WorldToScreenPoint(actualImpactPoint);
            
            Vector2 targetOffset = Vector2.zero;
            if (parentCanvas != null)
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    screenPoint,
                    parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : playerCamera,
                    out localPoint
                );
                
                targetOffset = localPoint;
            }
            
            float finalGap = currentGap + currentShootExpansion;
            
            if (topLine != null)
            {
                topLine.anchoredPosition = targetOffset + new Vector2(0, finalGap + lineLength / 2);
            }
            
            if (bottomLine != null)
            {
                bottomLine.anchoredPosition = targetOffset + new Vector2(0, -(finalGap + lineLength / 2));
            }
            
            if (leftLine != null)
            {
                leftLine.anchoredPosition = targetOffset + new Vector2(-(finalGap + lineLength / 2), 0);
            }
              if (rightLine != null)
            {
                rightLine.anchoredPosition = targetOffset + new Vector2(finalGap + lineLength / 2, 0);
            }
        }
        void UpdateCenterDotPosition()
        {
            if (centerDot == null) return;
            
            centerDot.anchoredPosition = Vector2.zero;
        }
        #endregion

        #region Public API
        public void SetWeaponController(WeaponController controller)
        {
            weaponController = controller;
        }

        public void UpdateVisuals()
        {
            ApplyVisualSettings();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
        
        void SetLinesVisible(bool visible)
        {
            if (topLine != null) topLine.gameObject.SetActive(visible);
            if (bottomLine != null) bottomLine.gameObject.SetActive(visible);
            if (leftLine != null) leftLine.gameObject.SetActive(visible);
            if (rightLine != null) rightLine.gameObject.SetActive(visible);
        }
        #endregion

        #region Editor
        void OnValidate()
        {
            if (Application.isPlaying && lineImages != null)
            {
                ApplyVisualSettings();
            }
            
            if (!Application.isPlaying && centerDot != null)
            {
                centerDot.anchoredPosition = Vector2.zero;
            }
        }
        #endregion
    }
}
