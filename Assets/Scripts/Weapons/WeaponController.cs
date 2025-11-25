using UnityEngine;
using System.Collections;
using BrickOps.Core;
/// <summary>
/// Controla el disparo de armas usando Raycast
/// Debe estar en el prefab del jugador
/// </summary>
public class WeaponController : MonoBehaviour
{
    #region Inspector Variables
    [Header("Referencias")]
    [Tooltip("Transform desde donde sale la bala (Muzzle)")]
    public Transform muzzlePoint;
    
    [Tooltip("Cámara para calcular dirección del disparo")]
    public Camera playerCamera;
    
    [Tooltip("GameObject que sirve como puntero para la dirección de disparo (se posiciona dinámicamente)")]
    public Transform aimPointer;

    [Header("Configuración del Arma")]
    [Tooltip("Daño por disparo")]
    public float damage = 25f;
    
    [Tooltip("Alcance máximo del raycast")]
    public float range = 100f;
    
    [Tooltip("Tiempo entre disparos (en segundos)")]
    public float fireRate = 0.15f;
    
    [Header("Spread Settings")]
    [Tooltip("Dispersión al disparar quieto sin apuntar")]
    public float standingSpread = 0.02f;
    
    [Tooltip("Dispersión al disparar quieto apuntando")]
    public float standingAimSpread = 0.005f;
    
    [Tooltip("Dispersión al disparar andando sin apuntar")]
    public float walkingSpread = 0.04f;
    
    [Tooltip("Dispersión al disparar andando apuntando")]
    public float walkingAimSpread = 0.015f;
    
    [Tooltip("Dispersión al disparar corriendo (no se puede apuntar)")]
    public float runningSpread = 0.08f;
    
    [Header("Aim Pointer Settings")]
    [Tooltip("Distancia por defecto del puntero si no hay obstáculos")]
    public float defaultPointerDistance = 50f;
    
    [Tooltip("Crear automáticamente el aim pointer si no está asignado")]
    public bool autoCreatePointer = false;

    [Header("Efectos Visuales")]
    [Tooltip("Prefab del efecto de disparo (muzzle flash)")]
    public GameObject muzzleFlashPrefab;
    
    [Tooltip("Prefab del efecto de impacto")]
    public GameObject impactEffectPrefab;
    
    [Tooltip("Prefab de la traza de bala (opcional)")]
    public LineRenderer bulletTracer;
    
    [Tooltip("Duración de la traza de bala")]
    public float tracerDuration = 0.05f;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip impactSound;
    
    [Header("Layers")]
    [Tooltip("Capas que pueden ser impactadas")]
    public LayerMask hitLayers;
    #endregion

    #region Private Variables
    private float nextFireTime = 0f;
    private AudioSource audioSource;
    private PlayerHealth playerHealth; // Para identificar al dueño
    private bool isLocalPlayer = false;
    private bool isAiming = false;
    private bool isMoving = false;
    private bool isRunning = false;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 3D sound
        
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Start()
    {
        // Validar referencias
        if (muzzlePoint == null)
        {
            Debug.LogError($"[WeaponController] Muzzle Point no asignado en {gameObject.name}!");
        }
        
        // Crear aim pointer si está habilitado y no existe
        if (autoCreatePointer && aimPointer == null)
        {
            GameObject pointer = new GameObject("AimPointer");
            aimPointer = pointer.transform;
            aimPointer.SetParent(transform);
            aimPointer.localPosition = Vector3.forward * defaultPointerDistance;
            Debug.Log($"[WeaponController] Aim Pointer creado automáticamente");
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Inicializa el arma para el jugador local
    /// </summary>
    public void InitializeForLocalPlayer(Camera cam)
    {
        playerCamera = cam;
        isLocalPlayer = true;
        Debug.Log($"[WeaponController] Inicializado para jugador local");
    }

    /// <summary>
    /// Actualiza el estado de apuntado
    /// </summary>
    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }
    
    /// <summary>
    /// Actualiza el estado de movimiento
    /// </summary>
    public void SetMovementState(bool moving, bool running)
    {
        isMoving = moving;
        isRunning = running;
    }

    /// <summary>
    /// Intenta disparar el arma
    /// </summary>
    public void TryShoot()
    {
        if (!isLocalPlayer) return;
        
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    /// <summary>
    /// Reproduce el efecto de disparo (para jugadores remotos)
    /// </summary>
    public void PlayShootEffect(Vector3 hitPoint, bool didHit)
    {
        // Efectos locales (sonido, muzzle flash)
        PlayMuzzleFlash();
        
        // Traza de bala
        if (bulletTracer != null && muzzlePoint != null)
        {
            StartCoroutine(ShowBulletTracer(muzzlePoint.position, hitPoint));
        }
        
        // Efecto de impacto
        if (didHit && impactEffectPrefab != null)
        {
            SpawnImpactEffect(hitPoint, Vector3.up);
        }
    }
    #endregion

    #region Shooting Logic
    void Shoot()
    {
        if (muzzlePoint == null) return;

        // Calcular dirección con dispersión
        Vector3 shootDirection = GetShootDirection();
          // Raycast
        RaycastHit hit;        bool didHit = Physics.Raycast(
            muzzlePoint.position, 
            shootDirection, 
            out hit, 
            range, 
            hitLayers
        );
        
        // Efectos visuales y sonoros locales
        PlayMuzzleFlash();
        PlayShootSound();
        
        Vector3 hitPoint;        if (didHit)
        {
            hitPoint = hit.point;
            
            // Primero buscar barricada (tiene prioridad porque no se mueven)
            Barricada barricada = hit.collider.GetComponentInParent<Barricada>();
              if (barricada != null)
            {
                // IMPACTO EN BARRICADA
                int barricadaDamage = Mathf.RoundToInt(damage / 2.5f); // Las barricadas reciben menos daño
                
                // Aplicar daño directamente (sin servidor en local)
                barricada.TakeDamage(barricadaDamage);
                
                // Si hay manager y somos servidor, sincronizar
                if (BarricadaManager.Instance?.IsServer() == true)
                {
                    BarricadaState state = barricada.GetState();
                    BarricadaManager.Instance.BroadcastBarricadaState(state);
                }
            }
            else
            {
                // No es barricada, buscar jugador
                PlayerHealth targetHealth = hit.collider.GetComponent<PlayerHealth>();
                if (targetHealth != null && targetHealth != playerHealth)
                {
                    // IMPACTO EN JUGADOR
                    int shooterId = playerHealth != null ? playerHealth.playerId : -1;                    int targetId = targetHealth.playerId;
                    
                    EventManager.Instance?.InvokePlayerHit(shooterId, targetId, damage, hitPoint);
                }
            }
            
            // Efecto de impacto
            SpawnImpactEffect(hit.point, hit.normal);
            PlayImpactSound(hit.point);
        }
        else
        {
            // No impactó nada, la bala va al infinito
            hitPoint = muzzlePoint.position + shootDirection * range;
        }

        // Traza de bala
        if (bulletTracer != null)
        {
            StartCoroutine(ShowBulletTracer(muzzlePoint.position, hitPoint));
        }

        // Debug visual (solo en editor)
        #if UNITY_EDITOR
        Debug.DrawRay(muzzlePoint.position, shootDirection * range, didHit ? Color.red : Color.green, 1f);
        #endif
    }

    Vector3 GetShootDirection()
    {
        Vector3 direction;
        
        // PRIORIDAD 1: Usar el aim pointer si existe
        if (aimPointer != null && muzzlePoint != null)
        {
            // Dirección desde el muzzle hacia el aim pointer
            direction = (aimPointer.position - muzzlePoint.position).normalized;
        }
        // PRIORIDAD 2: Dirección de la cámara
        else if (playerCamera != null)
        {
            direction = playerCamera.transform.forward;
        }
        // PRIORIDAD 3: Dirección del muzzle point
        else if (muzzlePoint != null)
        {
            direction = muzzlePoint.forward;
        }
        // PRIORIDAD 4: Dirección del transform del arma
        else
        {
            direction = transform.forward;
        }

        // Añadir dispersión (depende de movimiento y apuntado)
        float currentSpread = CalculateCurrentSpread();
        if (currentSpread > 0f)
        {
            direction += Random.insideUnitSphere * currentSpread;
            direction.Normalize();
        }

        return direction;
    }
    
    /// <summary>
    /// Calcula la dispersión actual según el estado del jugador
    /// </summary>
    float CalculateCurrentSpread()
    {
        // Corriendo: máxima dispersión (no se puede apuntar mientras corres)
        if (isRunning)
        {
            return runningSpread;
        }
        
        // Andando
        if (isMoving)
        {
            return isAiming ? walkingAimSpread : walkingSpread;
        }
        
        // Quieto (parado)
        return isAiming ? standingAimSpread : standingSpread;
    }
    #endregion

    #region Visual Effects
    void PlayMuzzleFlash()
    {
        if (muzzleFlashPrefab != null && muzzlePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            Destroy(flash, 0.1f);
        }
    }

    void SpawnImpactEffect(Vector3 position, Vector3 normal)
    {
        if (impactEffectPrefab != null)
        {
            Quaternion rotation = Quaternion.LookRotation(normal);
            GameObject impact = Instantiate(impactEffectPrefab, position, rotation);
            Destroy(impact, 2f);
        }
    }

    IEnumerator ShowBulletTracer(Vector3 startPos, Vector3 endPos)
    {
        if (bulletTracer == null) yield break;

        // Crear instancia del LineRenderer
        LineRenderer tracer = Instantiate(bulletTracer);
        tracer.positionCount = 2;
        tracer.SetPosition(0, startPos);
        tracer.SetPosition(1, endPos);

        // Esperar y destruir
        yield return new WaitForSeconds(tracerDuration);
        Destroy(tracer.gameObject);
    }
    #endregion

    #region Audio
    void PlayShootSound()
    {
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }

    void PlayImpactSound(Vector3 position)
    {
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, position);
        }
    }
    #endregion

    #region Gizmos
    void OnDrawGizmosSelected()
    {
        if (muzzlePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(muzzlePoint.position, 0.1f);
            Gizmos.DrawRay(muzzlePoint.position, muzzlePoint.forward * range);
        }
    }
    #endregion

    #region Server Communication

    #endregion
}