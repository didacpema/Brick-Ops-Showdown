using UnityEngine;
using BrickOps.Core;
using BrickOps.Networking;
using BrickOps.Players;

/// <summary>
/// Health pack que se sincroniza en la red. Cura 50 HP y respawnea después de 30 segundos.
/// </summary>
public class HealthPack : MonoBehaviour
{
    #region Inspector Variables
    [Header("Health Pack Settings")]
    [Tooltip("Cantidad de vida que cura")]
    public float healAmount = 50f;
    
    [Tooltip("Tiempo de respawn en segundos")]
    public float respawnTime = 30f;
    
    [Tooltip("ID único de este health pack (debe ser único en el mapa)")]
    public int healthPackId = 0;
    
    [Header("Visual Feedback")]
    [Tooltip("Renderer del health pack")]
    public Renderer packRenderer;
    
    [Tooltip("Collider del health pack")]
    public Collider packCollider;
    
    [Tooltip("Efecto de partículas al recoger (opcional)")]
    public ParticleSystem pickupEffect;
    
    [Tooltip("Rotación por segundo cuando está activo")]
    public float rotationSpeed = 90f;
    #endregion

    #region Private Variables
    private bool isActive = true;
    private float respawnTimer = 0f;
    private Material originalMaterial;
    private Color originalColor;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
       
        if (packRenderer == null)
            packRenderer = GetComponent<Renderer>();
        
        if (packCollider == null)
            packCollider = GetComponent<Collider>();
        
        
        if (packRenderer != null)
        {
            originalMaterial = packRenderer.material;
            originalColor = packRenderer.material.color;
        }
        
  
        if (healthPackId == 0)
        {
            healthPackId = GenerateIdFromPosition();
        }
        
        Debug.Log($"[HealthPack] Inicializado con ID: {healthPackId}");
    }

    void Update()
    {
        
        if (isActive)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
        
        
        if (!isActive)
        {
            respawnTimer -= Time.deltaTime;
            
            if (respawnTimer <= 0f)
            {
                Respawn();
            }
        }
    }    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[HealthPack] ¡Trigger detectado! Objeto: {other.gameObject.name}, Tag: {other.tag}");
        
       
        if (!isActive)
        {
            Debug.Log($"[HealthPack] Health pack inactivo, ignorando colisión");
            return;
        }
        
       
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = other.GetComponentInParent<PlayerHealth>();
        }
        
        if (playerHealth == null)
        {
            Debug.Log($"[HealthPack] No se encontró PlayerHealth en '{other.gameObject.name}' ni en sus padres. Tag: '{other.tag}'");
            return;
        }
        
        Debug.Log($"[HealthPack] PlayerHealth encontrado. PlayerId: {playerHealth.playerId}, IsLocal: {playerHealth.isLocalPlayer}");
        
   
        if (!playerHealth.isLocalPlayer)
        {
            Debug.Log($"[HealthPack] Jugador remoto (ID: {playerHealth.playerId}), ignorando");
            return;
        }
        
     
        float healthPercentage = playerHealth.GetHealthPercentage();
        Debug.Log($"[HealthPack] Vida del jugador: {healthPercentage * 100}%");
        
        if (healthPercentage >= 1f)
        {
            Debug.Log($"[HealthPack] Player {playerHealth.playerId} ya tiene vida completa");
            return;
        }
        
    
        Debug.Log($"[HealthPack] ¡Recogiendo health pack!");
        PickupHealthPack(playerHealth.playerId);
    }
    #endregion

    #region Health Pack Logic
  
    void PickupHealthPack(int playerId)
    {
        Debug.Log($"<color=green>[HealthPack] Player {playerId} recogió health pack {healthPackId}</color>");
        
       
        GameObject localPlayer = PlayerManager.Instance?.LocalPlayer;
        if (localPlayer != null)
        {
            PlayerHealth health = localPlayer.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.Heal(healAmount);
            }
        }
        
       
        SendHealthPackPickup(playerId);
        
      
        SetActive(false);
        
      
        PlayPickupEffect();
        
       
        respawnTimer = respawnTime;
    }
    
   
    public void ProcessNetworkPickup(int playerId)
    {
        Debug.Log($"[HealthPack] Procesando pickup de red - Player {playerId} recogió health pack {healthPackId}");
        
       
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null && health.playerId == playerId)
            {
                health.Heal(healAmount);
                break;
            }
        }
        
       
        SetActive(false);
        
       
        PlayPickupEffect();
        
        
        respawnTimer = respawnTime;
    }
    
 
    void SetActive(bool active)
    {
        isActive = active;
        
        if (packRenderer != null)
            packRenderer.enabled = active;
        
        if (packCollider != null)
            packCollider.enabled = active;
    }
    
    void Respawn()
    {
        Debug.Log($"[HealthPack] Health pack {healthPackId} respawneado");
        SetActive(true);
    }
    
     void PlayPickupEffect()
    {
        if (pickupEffect != null)
        {
            pickupEffect.Play();
        }
    }
    #endregion

    #region Network

    void SendHealthPackPickup(int playerId)
    {
        HealthPackData data = new HealthPackData(healthPackId, false, playerId);
        string message = NetworkProtocol.BuildMessage(NetworkProtocol.HEALTH_PACK_PICKUP, data);
        
        GameController controller = GameController.Instance;
        if (controller != null)
        {
            controller.SendMessageToNetwork(message);
        }
    }
    

    int GenerateIdFromPosition()
    {
        Vector3 pos = transform.position;
        return Mathf.Abs((pos.x + pos.y * 1000 + pos.z * 1000000).GetHashCode() % 10000);
    }
    #endregion

    #region Debug
    void OnDrawGizmos()
    {
        Gizmos.color = isActive ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
    #endregion
}
