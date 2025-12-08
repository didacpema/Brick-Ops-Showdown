using UnityEngine;

namespace BrickOps.UI
{
    /// <summary>
    /// Script de ayuda para inicializar el crosshair en el jugador local
    /// </summary>
    public class CrosshairInitializer : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Canvas donde está el crosshair")]
        public Canvas gameCanvas;
        
        [Tooltip("Prefab del crosshair (opcional, si no está en la escena)")]
        public GameObject crosshairPrefab;
        
        private DynamicCrosshair dynamicCrosshair;
        private WeaponController weaponController;

        void Start()
        {
            InitializeCrosshair();
        }

        void InitializeCrosshair()
        {          
            if (gameCanvas == null)
            {
                gameCanvas = FindAnyObjectByType<Canvas>();
                
                if (gameCanvas == null)
                {
                    Debug.LogWarning("[CrosshairInitializer] No se encontró Canvas. Creando uno nuevo...");
                    CreateCanvas();
                }
            }

            dynamicCrosshair = FindAnyObjectByType<DynamicCrosshair>();
            
            if (dynamicCrosshair == null && crosshairPrefab != null)
            {
                GameObject crosshairObj = Instantiate(crosshairPrefab, gameCanvas.transform);
                dynamicCrosshair = crosshairObj.GetComponent<DynamicCrosshair>();
                Debug.Log("[CrosshairInitializer] Crosshair instanciado desde prefab");
            }

            weaponController = GetComponent<WeaponController>();
            if (weaponController == null)
            {
                weaponController = GetComponentInChildren<WeaponController>();
            }

            if (dynamicCrosshair != null && weaponController != null)
            {
                dynamicCrosshair.SetWeaponController(weaponController);
                Debug.Log("[CrosshairInitializer] Crosshair conectado correctamente al WeaponController");
            }
            else
            {
                if (dynamicCrosshair == null)
                    Debug.LogWarning("[CrosshairInitializer] No se encontró DynamicCrosshair en la escena");
                if (weaponController == null)
                    Debug.LogWarning("[CrosshairInitializer] No se encontró WeaponController en el jugador");
            }
        }

        void CreateCanvas()
        {
            GameObject canvasObj = new GameObject("GameCanvas");
            gameCanvas = canvasObj.AddComponent<Canvas>();
            gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            Debug.Log("[CrosshairInitializer] Canvas creado automáticamente");
        }

        public DynamicCrosshair GetCrosshair()
        {
            return dynamicCrosshair;
        }
    }
}
