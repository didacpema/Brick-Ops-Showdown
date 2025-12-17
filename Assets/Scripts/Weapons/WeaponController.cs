using UnityEngine;
using System.Collections;
using BrickOps.Core;
using BrickOps.Players;
/// <summary>
/// Controla el disparo de armas usando Raycast
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
    public Transform aimPointer;    [Header("Configuración del Arma")]
    [Tooltip("Daño por disparo al cuerpo")]
    public float bodyDamage = 25f;
    
    [Tooltip("Daño por disparo a la cabeza (headshot)")]
    public float headDamage = 75f;
    
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
    
    [Tooltip("Dispersión al disparar saltando/en el aire")]
    public float jumpingSpread = 0.1f;
    
    [Header("Ammo System")]
    [Tooltip("Munición actual en el cargador")]
    public int currentAmmo = 30;
    
    [Tooltip("Tamaño máximo del cargador")]
    public int maxMagazineSize = 30;
    
    [Tooltip("Munición de reserva actual")]
    public int reserveAmmo = 90;
    
    [Tooltip("Munición de reserva máxima")]
    public int maxReserveAmmo = 120;
    
    [Tooltip("Tiempo de recarga en segundos")]
    public float reloadTime = 2f;
    
    [Tooltip("Si es true, al recargar se pierde la munición restante del cargador")]
    public bool loseAmmoOnReload = false;
    
    [Header("Aim Pointer Settings")]
    [Tooltip("Distancia por defecto del puntero si no hay obstáculos")]
    public float defaultPointerDistance = 50f;
    
    [Tooltip("Distancia mínima del target desde el jugador (evita apuntar demasiado cerca)")]
    public float minTargetDistance = 2f;
    
    [Tooltip("Velocidad de suavizado del target (mayor = más responsivo)")]
    [Range(1f, 300f)]
    public float targetSmoothSpeed = 300f;
    
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
    private PlayerHealth playerHealth; 
    private bool isLocalPlayer = false;
    private bool isAiming = false;
    private bool isMoving = false;
    private bool isRunning = false;
    private bool isGrounded = true;
    private Vector3 lastTargetPosition;
    
    // Ammo System
    private bool isReloading = false;
    private float reloadStartTime = 0f;
    private Animator playerAnimator;
    private InputManager inputManager;
    private static readonly int HashReload = Animator.StringToHash("Reload");
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; 
        
        playerHealth = GetComponent<PlayerHealth>();
        
        // Buscar Animator y InputManager para el sistema de recarga
        playerAnimator = GetComponentInParent<Animator>();
        inputManager = GetComponentInParent<InputManager>();
    }

    void Start()
    {
        if (muzzlePoint == null)
        {
            Debug.LogError($"[WeaponController] Muzzle Point no asignado en {gameObject.name}!");
        }
        
        if (autoCreatePointer && aimPointer == null)
        {
            GameObject pointer = new GameObject("AimPointer");
            aimPointer = pointer.transform;
            aimPointer.SetParent(transform);
            aimPointer.localPosition = Vector3.forward * defaultPointerDistance;
            Debug.Log($"[WeaponController] Aim Pointer creado automáticamente");
        }
    }
    
    void Update()
    {
        if (isLocalPlayer && aimPointer != null && playerCamera != null)
        {
            UpdateAimPointerPosition();
        }
        
        // Monitorear recarga
        if (isReloading)
        {
            UpdateReload();
        }
    }
    #endregion

    #region Public Methods
    public void InitializeForLocalPlayer(Camera cam)
    {
        playerCamera = cam;
        isLocalPlayer = true;
        Debug.Log($"[WeaponController] Inicializado para jugador local");
    }

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }
    public void SetMovementState(bool moving, bool running)
    {
        isMoving = moving;
        isRunning = running;
    }
    
    public void SetGrounded(bool grounded)
    {
        isGrounded = grounded;
    }

    public void TryShoot()
    {
        if (!isLocalPlayer) return;
        
        // No se puede disparar mientras se recarga
        if (isReloading)
        {
            CancelReload();
            return;
        }
        
        // PRIORIDAD: Si no hay munición, NO DISPARAR - solo intentar recargar
        if (currentAmmo <= 0)
        {
            TryReload();
            return; // IMPORTANTE: No continuar con el disparo
        }
        
        // Solo disparar si hay munición Y ha pasado el cooldown
        if (Time.time >= nextFireTime && currentAmmo > 0)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    public void PlayShootEffect(Vector3 hitPoint, bool didHit)
    {
        PlayMuzzleFlash();
        
        if (bulletTracer != null && muzzlePoint != null)
        {
            StartCoroutine(ShowBulletTracer(muzzlePoint.position, hitPoint));
        }
        
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
        
        if (currentAmmo <= 0)
        {
            TryReload();
            return;
        }
        
        currentAmmo--;

        Vector3 shootDirection = GetShootDirection();
        RaycastHit hit;        bool didHit = Physics.Raycast(
            muzzlePoint.position, 
            shootDirection, 
            out hit, 
            range, 
            hitLayers
        );
        
        PlayMuzzleFlash();
        PlayShootSound();
          Vector3 hitPoint;        if (didHit)
        {
            hitPoint = hit.point;
            
            Barricada barricada = hit.collider.GetComponentInParent<Barricada>();
            if (barricada != null)
            {
                int barricadaDamage = Mathf.RoundToInt(bodyDamage / 2.5f); 
                
                // Solo el HOST aplica el daño localmente, los clientes esperan el broadcast
                if (GameController.Instance != null)
                {
                    GameController.Instance.SendBarricadeHit(barricada.BarricadaId, barricadaDamage);
                }
            }
            else
            {
                HitboxController hitbox = hit.collider.GetComponent<HitboxController>();
                
                if (hitbox != null)
                {
                    PlayerHealth targetHealth = hitbox.GetPlayerHealth();
                    
                    if (targetHealth != null && targetHealth != playerHealth)
                    {
                        float damageToApply = hitbox.GetHitboxType() == HitboxType.Head ? headDamage : bodyDamage;
                        
                        int shooterId = playerHealth != null ? playerHealth.playerId : -1;
                        int targetId = targetHealth.playerId;
                        
                        string hitType = hitbox.GetHitboxType() == HitboxType.Head ? "HEADSHOT" : "BODYSHOT";
                        Debug.Log($"<color=yellow>[WeaponController] {hitType}! Daño: {damageToApply}</color>");
                        
                        EventManager.Instance?.InvokePlayerHit(shooterId, targetId, damageToApply, hitPoint);
                    }
                }
                else
                {
                    PlayerHealth targetHealth = hit.collider.GetComponent<PlayerHealth>();
                    if (targetHealth != null && targetHealth != playerHealth)
                    {
                        int shooterId = playerHealth != null ? playerHealth.playerId : -1;
                        int targetId = targetHealth.playerId;
                        
                        EventManager.Instance?.InvokePlayerHit(shooterId, targetId, bodyDamage, hitPoint);
                    }
                }
            }
            
            SpawnImpactEffect(hit.point, hit.normal);
            PlayImpactSound(hit.point);
        }
        else
        {
            hitPoint = muzzlePoint.position + shootDirection * range;
        }

        if (bulletTracer != null)
        {
            StartCoroutine(ShowBulletTracer(muzzlePoint.position, hitPoint));
        }

        #if UNITY_EDITOR
        Debug.DrawRay(muzzlePoint.position, shootDirection * range, didHit ? Color.red : Color.green, 1f);
        #endif
    }

    Vector3 GetShootDirection()
    {
        Vector3 direction;
        
        if (aimPointer != null && muzzlePoint != null)
        {
            direction = (aimPointer.position - muzzlePoint.position).normalized;
        }
        else if (playerCamera != null)
        {
            direction = playerCamera.transform.forward;
        }
        else if (muzzlePoint != null)
        {
            direction = muzzlePoint.forward;
        }
        else
        {
            direction = transform.forward;
        }

        float currentSpread = CalculateCurrentSpread();
        if (currentSpread > 0f)
        {
            direction += Random.insideUnitSphere * currentSpread;
            direction.Normalize();
        }

        return direction;
    }

    float CalculateCurrentSpread()
    {
        if (!isGrounded)
        {
            return jumpingSpread;
        }
        
        if (isRunning)
        {
            return runningSpread;
        }
        
        if (isMoving)
        {
            return isAiming ? walkingAimSpread : walkingSpread;
        }
        
        return isAiming ? standingAimSpread : standingSpread;
    }
    
    void UpdateAimPointerPosition()
    {
        if (aimPointer == null || playerCamera == null) return;
        
        Ray cameraRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Vector3 targetPosition;
        
        if (Physics.Raycast(cameraRay, out RaycastHit hit, range, hitLayers))
        {
            float distanceFromCamera = Vector3.Distance(playerCamera.transform.position, hit.point);
            
            if (distanceFromCamera < minTargetDistance)
            {
                targetPosition = playerCamera.transform.position + playerCamera.transform.forward * minTargetDistance;
            }
            else
            {
                targetPosition = hit.point;
            }
        }
        else
        {
            targetPosition = playerCamera.transform.position + playerCamera.transform.forward * range;
        }
        
        if (lastTargetPosition == Vector3.zero)
        {
            lastTargetPosition = targetPosition;
        }
        
        lastTargetPosition = Vector3.Lerp(lastTargetPosition, targetPosition, Time.deltaTime * targetSmoothSpeed);
        aimPointer.position = lastTargetPosition;
    }
    
    public Vector3 GetActualBulletImpactPoint()
    {
        if (muzzlePoint == null || aimPointer == null) 
            return aimPointer != null ? aimPointer.position : transform.position + transform.forward * range;
        
        Vector3 directionToTarget = (aimPointer.position - muzzlePoint.position).normalized;
        float distanceToTarget = Vector3.Distance(muzzlePoint.position, aimPointer.position);
        
        if (Physics.Raycast(muzzlePoint.position, directionToTarget, out RaycastHit muzzleHit, distanceToTarget, hitLayers))
        {
            return muzzleHit.point;
        }
        
        return aimPointer.position;
    }
    #endregion

    #region Ammo System
    /// <summary>
    /// Intenta recargar el arma
    /// </summary>
    public void TryReload()
    {
        if (!isLocalPlayer) return;
        
        if (isReloading) return;
        
        if (currentAmmo >= maxMagazineSize) return;
        
        if (reserveAmmo <= 0) return;
        
        StartReload();
    }
    
    /// <summary>
    /// Inicia el proceso de recarga
    /// </summary>
    void StartReload()
    {
        isReloading = true;
        reloadStartTime = Time.time;
        
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(HashReload);
        }
        
        Debug.Log($"[WeaponController] Recarga iniciada. Munición: {currentAmmo}/{maxMagazineSize} | Reserva: {reserveAmmo}");
    }
    
    /// <summary>
    /// Actualiza el estado de la recarga cada frame
    /// </summary>
    void UpdateReload()
    {
        float elapsedTime = Time.time - reloadStartTime;
        
        if (elapsedTime >= reloadTime)
        {
            CompleteReload();
        }
    }
    
    /// <summary>
    /// Completa la recarga y rellena el cargador
    /// </summary>
    void CompleteReload()
    {
        if (!isReloading) return;
        
        int ammoNeeded = maxMagazineSize - currentAmmo;
        
        if (loseAmmoOnReload)
        {
            
            ammoNeeded = maxMagazineSize;
            currentAmmo = 0;
        }
        
        int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);
        
        currentAmmo += ammoToReload;
        reserveAmmo -= ammoToReload;
        
        isReloading = false;
        
        Debug.Log($"[WeaponController] Recarga completada. Munición: {currentAmmo}/{maxMagazineSize} | Reserva: {reserveAmmo}");
    }
    
    /// <summary>
    /// Cancela la recarga en progreso
    /// </summary>
    public void CancelReload()
    {
        if (!isReloading) return;
        
        isReloading = false;
        
       
        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(HashReload);
        }
        
        Debug.Log("[WeaponController] Recarga cancelada");
    }
    
    /// <summary>
    /// Añade munición de reserva (para pickups)
    /// </summary>
    public void AddReserveAmmo(int amount)
    {
        reserveAmmo = Mathf.Min(reserveAmmo + amount, maxReserveAmmo);
        Debug.Log($"[WeaponController] Munición añadida. Reserva: {reserveAmmo}/{maxReserveAmmo}");
    }
    
    /// <summary>
    /// Verifica si está recargando
    /// </summary>
    public bool IsReloading()
    {
        return isReloading;
    }
    
    /// <summary>
    /// Obtiene la munición actual
    /// </summary>
    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }
    
    /// <summary>
    /// Obtiene la munición de reserva
    /// </summary>
    public int GetReserveAmmo()
    {
        return reserveAmmo;
    }
    
    /// <summary>
    /// Obtiene el progreso de recarga (0-1)
    /// </summary>
    public float GetReloadProgress()
    {
        if (!isReloading) return 0f;
        return Mathf.Clamp01((Time.time - reloadStartTime) / reloadTime);
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

        LineRenderer tracer = Instantiate(bulletTracer);
        tracer.positionCount = 2;
        tracer.SetPosition(0, startPos);
        tracer.SetPosition(1, endPos);

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
    }    void PlayImpactSound(Vector3 position)
    {
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, position);
        }
    }
    #endregion

    #region Public API (Crosshair)
    public float GetCurrentSpread()
    {
        return CalculateCurrentSpread();
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