using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sistema de vida para los jugadores
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    #region Inspector Variables
    [Header("Stats")]
    [Tooltip("Vida máxima del jugador")]
    public float maxHealth = 100f;
    
    [Header("UI (Opcional)")]
    [Tooltip("Barra de vida sobre el jugador (WorldSpace)")]
    public Slider healthBar;
    
    [Tooltip("Canvas que contiene la barra de vida")]
    public Canvas healthBarCanvas;
    
    [Header("Visual Feedback")]
    [Tooltip("Material del jugador al recibir daño")]
    public Material damageMaterial;
    
    [Tooltip("Duración del efecto de daño")]
    public float damageFlashDuration = 0.1f;
    
    [Header("Respawn")]
    [Tooltip("Tiempo antes de respawnear tras morir")]
    public float respawnDelay = 3f;
    #endregion

    #region Public Variables
    [HideInInspector] public int playerId = -1;
    [HideInInspector] public bool isLocalPlayer = false;
    #endregion

    #region Private Variables
    private float currentHealth;
    private Renderer playerRenderer;
    private Material originalMaterial;
    private bool isDead = false;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        currentHealth = maxHealth;
        playerRenderer = GetComponent<Renderer>();
        
        if (playerRenderer != null)
        {
            originalMaterial = playerRenderer.material;
        }
    }

    void Start()
    {
        UpdateHealthBar();
        
        // Configurar la barra de vida para que mire siempre a la cámara
        if (healthBarCanvas != null)
        {
            healthBarCanvas.worldCamera = Camera.main;
        }
    }

    void Update()
    {
        // Hacer que la barra de vida mire a la cámara
        if (healthBarCanvas != null && Camera.main != null)
        {
            healthBarCanvas.transform.LookAt(Camera.main.transform);
            healthBarCanvas.transform.Rotate(0, 180, 0); // Voltear para que no esté invertida
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Inicializa el componente de salud
    /// </summary>
    public void Initialize(int id, bool isLocal)
    {
        playerId = id;
        isLocalPlayer = isLocal;
        
        // Ocultar la barra de vida si es el jugador local
        if (isLocal && healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(false);
        }
        
        Debug.Log($"[PlayerHealth] Inicializado - ID: {playerId}, Local: {isLocal}");
    }

    /// <summary>
    /// Aplica daño al jugador
    /// </summary>
    public void TakeDamage(float damage, int attackerId)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"<color=red>[PlayerHealth] Player {playerId} recibió {damage} daño de Player {attackerId}. Vida: {currentHealth}/{maxHealth}</color>");

        UpdateHealthBar();
        PlayDamageEffect();

        if (currentHealth <= 0)
        {
            Die(attackerId);
        }
    }

    /// <summary>
    /// Cura al jugador
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        
        UpdateHealthBar();
        Debug.Log($"[PlayerHealth] Player {playerId} curado. Vida: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Obtiene el porcentaje de vida actual
    /// </summary>
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    /// <summary>
    /// Verifica si el jugador está vivo
    /// </summary>
    public bool IsAlive()
    {
        return !isDead;
    }
    #endregion

    #region Private Methods
    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = GetHealthPercentage();
            
            // Cambiar color según la vida
            Image fillImage = healthBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                if (GetHealthPercentage() > 0.6f)
                    fillImage.color = Color.green;
                else if (GetHealthPercentage() > 0.3f)
                    fillImage.color = Color.yellow;
                else
                    fillImage.color = Color.red;
            }
        }
    }

    void PlayDamageEffect()
    {
        if (playerRenderer != null && damageMaterial != null)
        {
            StartCoroutine(FlashDamage());
        }
    }

    System.Collections.IEnumerator FlashDamage()
    {
        if (playerRenderer != null)
        {
            playerRenderer.material = damageMaterial;
            yield return new WaitForSeconds(damageFlashDuration);
            playerRenderer.material = originalMaterial;
        }
    }

    void Die(int killerId)
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log($"<color=red>☠ Player {playerId} eliminado por Player {killerId}</color>");

        // Notificar al GameController
        if (GameController.instance != null)
        {
            GameController.instance.OnPlayerDied(playerId, killerId);
        }

        // Desactivar el jugador
        gameObject.SetActive(false);

        // Programar respawn
        if (isLocalPlayer)
        {
            Invoke(nameof(Respawn), respawnDelay);
        }
    }

    void Respawn()
    {
        if (!isLocalPlayer) return;

        currentHealth = maxHealth;
        isDead = false;
        
        // Reactivar el jugador
        gameObject.SetActive(true);
        
        // Solicitar respawn al GameController
        if (GameController.instance != null)
        {
            GameController.instance.RequestRespawn(playerId);
        }
        
        UpdateHealthBar();
        Debug.Log($"<color=green>♻ Player {playerId} respawneado</color>");
    }
    #endregion
}