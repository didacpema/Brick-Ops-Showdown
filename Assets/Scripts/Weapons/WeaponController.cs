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

    [Header("Configuración del Arma")]
    [Tooltip("Daño por disparo")]
    public float damage = 25f;
    
    [Tooltip("Alcance máximo del raycast")]
    public float range = 100f;
    
    [Tooltip("Tiempo entre disparos (en segundos)")]
    public float fireRate = 0.15f;
    
    [Tooltip("Dispersión del arma (0 = preciso, 0.1 = impreciso)")]
    public float spread = 0.02f;

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
        RaycastHit hit;
        bool didHit = Physics.Raycast(
            muzzlePoint.position, 
            shootDirection, 
            out hit, 
            range, 
            hitLayers
        );

        // Efectos visuales y sonoros locales
        PlayMuzzleFlash();
        PlayShootSound();        Vector3 hitPoint;
        if (didHit)
        {
            hitPoint = hit.point;
            
            Debug.Log($"<color=white>[WeaponController] Raycast hit: {hit.collider.gameObject.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}</color>");
            
            // Aplicar daño si impactó a un jugador
            PlayerHealth targetHealth = hit.collider.GetComponent<PlayerHealth>();
            if (targetHealth != null && targetHealth != playerHealth)
            {
                // Paso 4: Usar EventManager en lugar de GameController
                int shooterId = playerHealth != null ? playerHealth.playerId : -1;
                int targetId = targetHealth.playerId;
                
                EventManager.Instance?.InvokePlayerHit(shooterId, targetId, damage, hitPoint);
                
                Debug.Log($"<color=yellow>¡Impacto! {shooterId} disparó a {targetId} por {damage} daño</color>");
            }
            
            // Aplicar daño si impactó a una barricada
            Barricada barricada = hit.collider.GetComponent<Barricada>();
            if (barricada == null)
            {
                // Intentar buscar en el padre
                barricada = hit.collider.GetComponentInParent<Barricada>();
                if (barricada != null)
                {
                    Debug.Log($"<color=magenta>[WeaponController] Barricada encontrada en el padre: {hit.collider.transform.parent.name}</color>");
                }
            }
            else
            {
                Debug.Log($"<color=magenta>[WeaponController] Barricada encontrada directamente en: {hit.collider.gameObject.name}</color>");
            }
            
            if (barricada != null)
            {
                int barricadaDamage = Mathf.RoundToInt(damage / 2.5f); // Las barricadas reciben menos daño
                
                Debug.Log($"<color=cyan>¡Impacto en Barricada {barricada.GetBarricadaId()}! Daño calculado: {barricadaDamage}</color>");
                
                if (BarricadaManager.Instance != null)
                {
                    Debug.Log($"<color=green>[WeaponController] BarricadaManager encontrado. IsServer: {BarricadaManager.Instance.IsServer()}</color>");
                    
                    // Si somos servidor, aplicar daño directamente
                    if (BarricadaManager.Instance.IsServer())
                    {
                        BarricadaManager.Instance.ApplyDamageToBarricada(barricada.GetBarricadaId(), barricadaDamage);
                        Debug.Log($"<color=green>[WeaponController] Daño aplicado por servidor</color>");
                    }
                    else
                    {
                        // Si somos cliente, enviar mensaje al servidor
                        SendBarricadaDamageToServer(barricada.GetBarricadaId(), barricadaDamage);
                        Debug.Log($"<color=green>[WeaponController] Mensaje enviado al servidor</color>");
                    }
                }
                else
                {
                    Debug.LogWarning($"<color=orange>[WeaponController] BarricadaManager no encontrado. Aplicando daño localmente</color>");
                    // Fallback: aplicar daño localmente
                    barricada.TakeDamage(barricadaDamage);
                }
            }
            else
            {
                Debug.Log($"<color=gray>[WeaponController] No hay componente Barricada en {hit.collider.gameObject.name}</color>");
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
        
        if (muzzlePoint != null)
        {
            // Fallback: disparar en la dirección del muzzle
            direction = muzzlePoint.forward;
        }
        else if (playerCamera != null)
        {
            // Disparar desde el centro de la pantalla (más preciso para FPS)
            direction = playerCamera.transform.forward;
        }
        else
        {
            direction = transform.forward;
        }

        // Añadir dispersión
        if (spread > 0f)
        {
            direction += Random.insideUnitSphere * spread;
            direction.Normalize();
        }

        return direction;
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
    void SendBarricadaDamageToServer(int barricadaId, int damage)
    {
        // Buscar el cliente UDP y enviar mensaje
        UDPClient_Select client = FindFirstObjectByType<UDPClient_Select>();
        if (client != null)
        {
            string msg = $"BARRICADA_DAMAGE:{barricadaId}:{damage}";
            
            // Usar reflexión para acceder al método SendToServer
            var sendMethod = client.GetType().GetMethod("SendToServer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (sendMethod != null)
            {
                sendMethod.Invoke(client, new object[] { msg });
                Debug.Log($"[WeaponController] Enviando daño al servidor: {msg}");
            }
            else
            {
                Debug.LogWarning("[WeaponController] No se encontró el método SendToServer");
            }
        }
        else
        {
            Debug.LogWarning("[WeaponController] No se encontró UDPClient_Select");
        }
    }
    #endregion
}