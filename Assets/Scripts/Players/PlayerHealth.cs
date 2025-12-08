using BrickOps.Core;
using BrickOps.Players;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
    private Coroutine _healingRoutine = null;
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
        
        if (healthBarCanvas != null)
        {
            healthBarCanvas.worldCamera = Camera.main;
        }
    }

    void Update()
    {
        if (healthBarCanvas != null && Camera.main != null)
        {
            healthBarCanvas.transform.LookAt(Camera.main.transform);
            healthBarCanvas.transform.Rotate(0, 180, 0); 
        }
    }
    #endregion

    #region Public Methods
    public void Initialize(int id, bool isLocal)
    {
        playerId = id;
        isLocalPlayer = isLocal;
        
        if (isLocal && healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(false);
        }
        
        Debug.Log($"[PlayerHealth] Inicializado - ID: {playerId}, Local: {isLocal}");
    }

    public void TakeDamage(float damage, int attackerId)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"<color=red>[PlayerHealth] Player {playerId} recibió {damage} daño de Player {attackerId}. Vida: {currentHealth}/{maxHealth}</color>");

        UpdateHealthBar();
        NotifyHealthChanged();
        PlayDamageEffect();

        if (currentHealth <= 0)
        {
            Die(attackerId);
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        if (_healingRoutine == null)
        _healingRoutine = StartCoroutine(HealOverTime());
        
    }
    IEnumerator HealOverTime()
    {
       while (!isDead && currentHealth < maxHealth)
        { 
            float healAmount = currentHealth * 0.20f;

            currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);

            UpdateHealthBar();
            NotifyHealthChanged();
            Debug.Log($"[PlayerHealth] Player {playerId} curado. Vida: {currentHealth}/{maxHealth}");

            yield return new WaitForSeconds(0.5f);
        }

        _healingRoutine = null;
    }

    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    public bool IsAlive()
    {
        return !isDead;
    }

    public void ApplyRemoteDamage(float damage)
    {
        if (isLocalPlayer || isDead || damage <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        UpdateHealthBar();
    }

    public void ResetHealthState()
    {
        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthBar();
        NotifyHealthChanged();
    }

    public void MarkDeadLocally()
    {
        if (isDead)
            return;

        isDead = true;
        currentHealth = 0f;
        UpdateHealthBar();
        NotifyHealthChanged();
    }
    #endregion

    #region Private Methods
    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = GetHealthPercentage();
            
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

    void NotifyHealthChanged()
    {
        EventManager.Instance?.InvokePlayerHealthChanged(playerId, currentHealth, maxHealth);
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

        EventManager.Instance?.InvokePlayerDied(playerId, killerId);

        gameObject.SetActive(false);

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

        Vector3 spawnPos = PlayerManager.Instance?.GetSpawnPosition(playerId) ?? Vector3.zero;

        gameObject.SetActive(true);
        transform.position = spawnPos;
        transform.rotation = Quaternion.identity;

        EventManager.Instance?.InvokePlayerRespawned(playerId, spawnPos);

        UpdateHealthBar();
        NotifyHealthChanged();
        Debug.Log($"<color=green>✓ Player {playerId} respawneado en {spawnPos}</color>");
    }
    #endregion
}