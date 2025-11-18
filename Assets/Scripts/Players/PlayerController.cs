using UnityEngine;
using BrickOps.Core;
using BrickOps.Players;

/// <summary>
/// Controlador principal del jugador
/// Este componente debe estar en el root del prefab y coordina todos los sistemas
/// Usar [RequireComponent] asegura que todos los componentes necesarios existen
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(WeaponController))]
public class PlayerController : MonoBehaviour
{
    #region Inspector Variables
    [Header("Player Identity")]
    [Tooltip("ID del jugador (se asigna automáticamente)")]
    public int playerId = -1;

    [Tooltip("¿Es el jugador local?")]
    public bool isLocalPlayer = false;

    [Header("Components References")]
    [Tooltip("Referencia al InputManager (solo para jugador local)")]
    public InputManager inputManager;

    [Tooltip("Referencia al CameraController (solo para jugador local)")]
    public CameraController cameraController;

    [Tooltip("Referencia al RemotePlayerAnimator (solo para jugadores remotos)")]
    public RemotePlayerAnimator remoteAnimator;

    [Header("Camera")]
    [SerializeField] private Transform cameraTargetPoint; // GameObject vacío fijo
    #endregion

    #region Component Cache
    private Rigidbody rb;
    private Animator animator;
    private PlayerHealth health;
    private WeaponController weapon;
    private Renderer playerRenderer;
    #endregion

    #region Initialization
    void Awake()
    {
        CacheComponents();
    }

    void Start()
    {
        // Si no hay target point, créalo
        if (cameraTargetPoint == null)
        {
            GameObject targetObj = new GameObject("CameraTarget");
            targetObj.transform.SetParent(transform);
            targetObj.transform.localPosition = new Vector3(0f, 1.6f, 0f); // Altura de ojos
            targetObj.transform.localRotation = Quaternion.identity;
            cameraTargetPoint = targetObj.transform;
        }
    }

    void CacheComponents()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        health = GetComponent<PlayerHealth>();
        weapon = GetComponent<WeaponController>();
        playerRenderer = GetComponent<Renderer>();
    }

    /// <summary>
    /// Inicializa el jugador local
    /// </summary>
    public void InitializeAsLocal(int id)
    {
        playerId = id;
        isLocalPlayer = true;

        ConfigurePhysics(false);
        
        if (inputManager != null)
        {
            inputManager.Initialize(gameObject);
        }

        if (health != null)
        {
            health.Initialize(id, true);
        }

        if (cameraController != null && weapon != null)
        {
            Camera cam = cameraController.GetCamera();
            weapon.InitializeForLocalPlayer(cam);
        }

        // Desactivar RemotePlayerAnimator si existe
        if (remoteAnimator != null)
        {
            remoteAnimator.enabled = false;
        }

        Debug.Log($"[PlayerController] Initialized as LOCAL player {id}");
    }

    /// <summary>
    /// Inicializa el jugador remoto
    /// </summary>
    public void InitializeAsRemote(int id)
    {
        playerId = id;
        isLocalPlayer = false;

        ConfigurePhysics(true);

        if (health != null)
        {
            health.Initialize(id, false);
        }

        // Desactivar componentes de jugador local
        if (inputManager != null)
        {
            inputManager.enabled = false;
        }

        if (cameraController != null)
        {
            cameraController.enabled = false;
            cameraController.gameObject.SetActive(false);
        }

        if (weapon != null)
        {
            weapon.enabled = false;
        }

        // Activar RemotePlayerAnimator
        if (remoteAnimator != null)
        {
            remoteAnimator.enabled = true;
            remoteAnimator.Initialize();
        }

        Debug.Log($"[PlayerController] Initialized as REMOTE player {id}");
    }

    void ConfigurePhysics(bool isKinematic)
    {
        if (rb != null)
        {
            rb.isKinematic = isKinematic;
            // Freeze all rotation axes to prevent collision-induced spins
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }
    #endregion

    #region Public API
    public Rigidbody GetRigidbody() => rb;
    public Animator GetAnimator() => animator;
    public PlayerHealth GetHealth() => health;
    public WeaponController GetWeapon() => weapon;
    public InputManager GetInputManager() => inputManager;
    public CameraController GetCameraController() => cameraController;
    public RemotePlayerAnimator GetRemoteAnimator() => remoteAnimator;

    public void SetVisuals(Material material, Color color)
    {
        if (playerRenderer != null && material != null)
        {
            playerRenderer.material = new Material(material);
            playerRenderer.material.color = color;
        }
    }

    public PlayerState GetCurrentState()
    {
        if (inputManager != null && isLocalPlayer)
        {
            return inputManager.GetCurrentPlayerState(playerId);
        }
        return null;
    }
    #endregion
}
