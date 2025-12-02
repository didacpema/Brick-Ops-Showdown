using UnityEngine;
using BrickOps.UI;

/// <summary>
/// Script de testing para el crosshair dinámico
/// Agrégalo a un GameObject en la escena para hacer pruebas sin necesidad del jugador
/// </summary>
public class CrosshairTester : MonoBehaviour
{
    [Header("Referencias")]
    public DynamicCrosshair crosshair;
    public WeaponController weaponController;

    [Header("Test Settings")]
    [Tooltip("Simular estados sin necesidad de movimiento real")]
    public bool enableSimulation = true;
    
    [Header("Estados Simulados")]
    public bool simulateAiming = false;
    public bool simulateWalking = false;
    public bool simulateRunning = false;

    [Header("Info")]
    [SerializeField] private float currentSpread;
    [SerializeField] private string currentState;    void Start()
    {
        // Auto-encontrar componentes si no están asignados
        if (crosshair == null)
        {
            crosshair = FindAnyObjectByType<DynamicCrosshair>();
        }

        if (weaponController == null)
        {
            weaponController = FindAnyObjectByType<WeaponController>();
        }

        if (crosshair == null)
        {
            Debug.LogWarning("[CrosshairTester] No se encontró DynamicCrosshair en la escena");
        }

        if (weaponController == null)
        {
            Debug.LogWarning("[CrosshairTester] No se encontró WeaponController en la escena");
        }
    }

    void Update()
    {
        if (!enableSimulation) return;

        // Simular estados
        if (weaponController != null)
        {
            weaponController.SetAiming(simulateAiming);
            weaponController.SetMovementState(simulateWalking || simulateRunning, simulateRunning);
        }

        // Actualizar info de debug
        UpdateDebugInfo();

        // Controles de teclado para testing rápido
        HandleKeyboardInput();
    }

    void UpdateDebugInfo()
    {
        if (weaponController != null)
        {
            currentSpread = weaponController.GetCurrentSpread();
            currentState = GetCurrentStateName();
        }
    }

    string GetCurrentStateName()
    {
        if (simulateRunning)
            return "🏃 Corriendo";
        
        if (simulateWalking)
        {
            if (simulateAiming)
                return "🚶🎯 Andando + Apuntando";
            else
                return "🚶 Andando";
        }

        if (simulateAiming)
            return "🎯 Quieto + Apuntando";

        return "🧍 Quieto";
    }

    void HandleKeyboardInput()
    {
        // Toggle states con teclas numéricas
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Estado 1: Quieto sin apuntar
            simulateAiming = false;
            simulateWalking = false;
            simulateRunning = false;
            Debug.Log("[Test] Estado: Quieto sin apuntar");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Estado 2: Quieto apuntando
            simulateAiming = true;
            simulateWalking = false;
            simulateRunning = false;
            Debug.Log("[Test] Estado: Quieto apuntando");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Estado 3: Andando sin apuntar
            simulateAiming = false;
            simulateWalking = true;
            simulateRunning = false;
            Debug.Log("[Test] Estado: Andando sin apuntar");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            // Estado 4: Andando apuntando
            simulateAiming = true;
            simulateWalking = true;
            simulateRunning = false;
            Debug.Log("[Test] Estado: Andando apuntando");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            // Estado 5: Corriendo
            simulateAiming = false;
            simulateWalking = false;
            simulateRunning = true;
            Debug.Log("[Test] Estado: Corriendo");
        }

        // Toggle individual con teclas
        if (Input.GetKeyDown(KeyCode.A))
        {
            simulateAiming = !simulateAiming;
            Debug.Log($"[Test] Aiming: {simulateAiming}");
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            simulateWalking = !simulateWalking;
            if (simulateWalking) simulateRunning = false;
            Debug.Log($"[Test] Walking: {simulateWalking}");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            simulateRunning = !simulateRunning;
            if (simulateRunning)
            {
                simulateWalking = false;
                simulateAiming = false;
            }
            Debug.Log($"[Test] Running: {simulateRunning}");
        }
    }

    void OnGUI()
    {
        if (!enableSimulation) return;

        GUILayout.BeginArea(new Rect(10, 10, 400, 300));
        GUILayout.Box("🎯 CROSSHAIR TESTER");
        
        GUILayout.Label($"Estado Actual: {currentState}");
        GUILayout.Label($"Spread: {currentSpread:F4}");
        
        GUILayout.Space(10);
        GUILayout.Label("=== CONTROLES ===");
        GUILayout.Label("Teclas 1-5: Estados preconfigurados");
        GUILayout.Label("  1 - Quieto sin apuntar");
        GUILayout.Label("  2 - Quieto apuntando");
        GUILayout.Label("  3 - Andando sin apuntar");
        GUILayout.Label("  4 - Andando apuntando");
        GUILayout.Label("  5 - Corriendo");
        
        GUILayout.Space(10);
        GUILayout.Label("Teclas individuales:");
        GUILayout.Label($"  A - Apuntar (actual: {simulateAiming})");
        GUILayout.Label($"  W - Andar (actual: {simulateWalking})");
        GUILayout.Label($"  R - Correr (actual: {simulateRunning})");
        
        GUILayout.EndArea();
    }
}
