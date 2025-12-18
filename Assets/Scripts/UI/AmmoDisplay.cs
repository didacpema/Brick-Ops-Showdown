using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Muestra la munición actual y de reserva en el HUD
/// </summary>
public class AmmoDisplay : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Referencia al WeaponController del jugador local")]
    public WeaponController weaponController;
    
    [Tooltip("Texto para mostrar la munición actual/cargador")]
    public TextMeshProUGUI currentAmmoText;
    
    [Tooltip("Texto para mostrar la munición de reserva")]
    public TextMeshProUGUI reserveAmmoText;
    
    [Tooltip("Texto para mostrar el estado de recarga")]
    public TextMeshProUGUI reloadText;
    
    [Tooltip("Imagen de fondo de la barra de recarga (opcional)")]
    public Image reloadBarBackground;
    
    [Tooltip("Imagen de la barra de recarga (opcional)")]
    public Image reloadBarFill;
    
    [Header("Configuración")]
    [Tooltip("Color cuando hay munición suficiente")]
    public Color normalColor = Color.white;
    
    [Tooltip("Color cuando la munición es baja")]
    public Color lowAmmoColor = Color.yellow;
    
    [Tooltip("Color cuando no hay munición")]
    public Color noAmmoColor = Color.red;
    
    [Tooltip("Porcentaje considerado como munición baja (0-1)")]
    [Range(0f, 1f)]
    public float lowAmmoThreshold = 0.3f;
    
    [Header("Auto-Find")]
    [Tooltip("Buscar automáticamente el WeaponController del jugador local")]
    public bool autoFindWeapon = true;

    private void Start()
    {
        if (autoFindWeapon && weaponController == null)
        {
            FindWeaponController();
        }
        
        // Ocultar elementos de recarga al inicio
        if (reloadText != null)
            reloadText.gameObject.SetActive(false);
            
        if (reloadBarBackground != null)
            reloadBarBackground.gameObject.SetActive(false);
            
        if (reloadBarFill != null)
            reloadBarFill.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (weaponController == null)
        {
            if (autoFindWeapon)
                FindWeaponController();
            return;
        }

        UpdateAmmoDisplay();
        UpdateReloadDisplay();
    }

    void FindWeaponController()
    {
        // Buscar el jugador local
        GameObject localPlayer = GameObject.FindGameObjectWithTag("Player");
        if (localPlayer != null)
        {
            weaponController = localPlayer.GetComponentInChildren<WeaponController>();
        }
        
        if (weaponController == null)
        {
            WeaponController[] weapons = FindObjectsByType<WeaponController>(FindObjectsSortMode.None);
            foreach (var weapon in weapons)
            {
                // Verificar si es del jugador local 
                if (weapon.gameObject.activeInHierarchy)
                {
                    weaponController = weapon;
                    break;
                }
            }
        }
    }

    void UpdateAmmoDisplay()
    {
        int currentAmmo = weaponController.GetCurrentAmmo();
        int reserveAmmo = weaponController.GetReserveAmmo();
        int maxMagazine = weaponController.maxMagazineSize;

        // Actualizar textos
        if (currentAmmoText != null)
        {
            currentAmmoText.text = currentAmmo.ToString();
            
            float ammoPercent = (float)currentAmmo / maxMagazine;
            if (currentAmmo == 0)
                currentAmmoText.color = noAmmoColor;
            else if (ammoPercent <= lowAmmoThreshold)
                currentAmmoText.color = lowAmmoColor;
            else
                currentAmmoText.color = normalColor;
        }

        if (reserveAmmoText != null)
        {
            reserveAmmoText.text = reserveAmmo.ToString();
        }
    }

    void UpdateReloadDisplay()
    {
        bool isReloading = weaponController.IsReloading();
        
        if (reloadText != null)
        {
            reloadText.gameObject.SetActive(isReloading);
            if (isReloading)
            {
                float progress = weaponController.GetReloadProgress();
                reloadText.text = $"RECARGANDO... {Mathf.RoundToInt(progress * 100)}%";
            }
        }
        
        if (reloadBarBackground != null)
        {
            reloadBarBackground.gameObject.SetActive(isReloading);
        }
        
        if (reloadBarFill != null)
        {
            reloadBarFill.gameObject.SetActive(isReloading);
            if (isReloading)
            {
                float progress = weaponController.GetReloadProgress();
                reloadBarFill.fillAmount = progress;
            }
        }
    }

    public void SetWeaponController(WeaponController weapon)
    {
        weaponController = weapon;
    }
}
