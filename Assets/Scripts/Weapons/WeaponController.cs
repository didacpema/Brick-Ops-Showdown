using UnityEngine;
using System.Collections;
using BrickOps.Core;
using BrickOps.Players;
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
    private PlayerHealth playerHealth; // Para identificar al dueño
    private bool isLocalPlayer = false;
    private bool isAiming = false;
    private bool isMoving = false;
    private bool isRunning = false;
    private bool isGrounded = true;
    private Vector3 lastTargetPosition; // Para suavizar movimiento del target
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
    
    void Update()
    {
        // Actualizar posición del aim pointer dinámicamente
        if (isLocalPlayer && aimPointer != null && playerCamera != null)
        {
            UpdateAimPointerPosition();
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
    /// Actualiza el estado de si está en el suelo
    /// </summary>
    public void SetGrounded(bool grounded)
    {
        isGrounded = grounded;
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
                int barricadaDamage = Mathf.RoundToInt(bodyDamage / 2.5f); // Las barricadas reciben menos daño
                
                // Aplicar daño directamente (sin servidor en local)
                barricada.TakeDamage(barricadaDamage);
                
                // Si hay manager y somos servidor, sincronizar
                if (GameController.Instance != null)
                {
                    // Usamos la propiedad pública que creamos en el Paso 1
                    GameController.Instance.SendBarricadeHit(barricada.BarricadaId, barricadaDamage);
                }
            }
            else
            {
                // Buscar hitbox específica (cabeza o cuerpo)
                HitboxController hitbox = hit.collider.GetComponent<HitboxController>();
                
                if (hitbox != null)
                {
                    // IMPACTO EN HITBOX ESPECÍFICA (cabeza o cuerpo)
                    PlayerHealth targetHealth = hitbox.GetPlayerHealth();
                    
                    if (targetHealth != null && targetHealth != playerHealth)
                    {
                        // Determinar daño según tipo de hitbox
                        float damageToApply = hitbox.GetHitboxType() == HitboxType.Head ? headDamage : bodyDamage;
                        
                        int shooterId = playerHealth != null ? playerHealth.playerId : -1;
                        int targetId = targetHealth.playerId;
                        
                        // Log para debug
                        string hitType = hitbox.GetHitboxType() == HitboxType.Head ? "HEADSHOT" : "BODYSHOT";
                        Debug.Log($"<color=yellow>[WeaponController] {hitType}! Daño: {damageToApply}</color>");
                        
                        EventManager.Instance?.InvokePlayerHit(shooterId, targetId, damageToApply, hitPoint);
                    }
                }
                else
                {
                    // Fallback: buscar PlayerHealth directamente (por si acaso no tiene hitboxes)
                    PlayerHealth targetHealth = hit.collider.GetComponent<PlayerHealth>();
                    if (targetHealth != null && targetHealth != playerHealth)
                    {
                        // IMPACTO EN JUGADOR (sin hitbox específica, usar daño de cuerpo)
                        int shooterId = playerHealth != null ? playerHealth.playerId : -1;
                        int targetId = targetHealth.playerId;
                        
                        EventManager.Instance?.InvokePlayerHit(shooterId, targetId, bodyDamage, hitPoint);
                    }
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
        // Prioridad 1: Saltando (en el aire) - máxima dispersión
        if (!isGrounded)
        {
            return jumpingSpread;
        }
        
        // Prioridad 2: Corriendo - alta dispersión (no se puede apuntar mientras corres)
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
    
    /// <summary>
    /// Actualiza la posición del aim pointer: siempre en el centro de la pantalla (desde la cámara)
    /// Este es el punto "justo" donde el jugador está apuntando
    /// </summary>
    void UpdateAimPointerPosition()
    {
        if (aimPointer == null || playerCamera == null) return;
        
        // El target SIEMPRE está en el centro de la pantalla (dirección de la cámara)
        // Esto garantiza disparos justos y predecibles
        Ray cameraRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Vector3 targetPosition;
        
        if (Physics.Raycast(cameraRay, out RaycastHit hit, range, hitLayers))
        {
            // Hay un objeto en el centro de la pantalla
            // Verificar que esté a distancia mínima de la cámara
            float distanceFromCamera = Vector3.Distance(playerCamera.transform.position, hit.point);
            
            if (distanceFromCamera < minTargetDistance)
            {
                // Está muy cerca, usar distancia mínima desde la cámara
                targetPosition = playerCamera.transform.position + playerCamera.transform.forward * minTargetDistance;
            }
            else
            {
                targetPosition = hit.point;
            }
        }
        else
        {
            // No hay obstáculos, posicionar a distancia máxima
            targetPosition = playerCamera.transform.position + playerCamera.transform.forward * range;
        }
        
        // Suavizar movimiento del target para evitar sacudidas durante shake de cámara
        if (lastTargetPosition == Vector3.zero)
        {
            lastTargetPosition = targetPosition;
        }
        
        lastTargetPosition = Vector3.Lerp(lastTargetPosition, targetPosition, Time.deltaTime * targetSmoothSpeed);
        aimPointer.position = lastTargetPosition;
    }
    
    /// <summary>
    /// Obtiene el punto real donde impactará la bala considerando obstáculos desde el muzzle
    /// </summary>
    public Vector3 GetActualBulletImpactPoint()
    {
        if (muzzlePoint == null || aimPointer == null) 
            return aimPointer != null ? aimPointer.position : transform.position + transform.forward * range;
        
        // Calcular dirección desde el muzzle hacia el target (centro de pantalla)
        Vector3 directionToTarget = (aimPointer.position - muzzlePoint.position).normalized;
        float distanceToTarget = Vector3.Distance(muzzlePoint.position, aimPointer.position);
        
        // Raycast de comprobación desde el muzzle hacia el target
        if (Physics.Raycast(muzzlePoint.position, directionToTarget, out RaycastHit muzzleHit, distanceToTarget, hitLayers))
        {
            // Hay un obstáculo entre el muzzle y el target - la bala impactará aquí
            return muzzleHit.point;
        }
        
        // No hay obstáculos, la bala llegará al target
        return aimPointer.position;
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
    }    void PlayImpactSound(Vector3 position)
    {
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, position);
        }
    }
    #endregion

    #region Public API (Crosshair)
    /// <summary>
    /// Obtiene el spread actual del arma para el crosshair dinámico
    /// </summary>
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