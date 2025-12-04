using UnityEngine;

/// <summary>
/// Script de diagnóstico para verificar la configuración del Health Pack
/// Añádelo temporalmente al HealthPack para debuggear problemas
/// </summary>
public class HealthPackDebugger : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== HEALTH PACK DEBUGGER ===");
        
     
        HealthPack healthPack = GetComponent<HealthPack>();
        if (healthPack == null)
        {
            Debug.LogError("[Debug] ¡HealthPack script NO encontrado!");
            return;
        }
        
        Debug.Log($"[Debug] HealthPack encontrado - ID: {healthPack.healthPackId}");
        Debug.Log($"[Debug] Heal Amount: {healthPack.healAmount}");
        Debug.Log($"[Debug] Respawn Time: {healthPack.respawnTime}");
        
    
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("[Debug] ¡NO hay Collider!");
        }
        else
        {
            Debug.Log($"[Debug] Collider encontrado: {col.GetType().Name}");
            Debug.Log($"[Debug] Is Trigger: {col.isTrigger}");
            if (!col.isTrigger)
            {
                Debug.LogError("[Debug] ¡PROBLEMA! El Collider NO es Trigger. Debe activarse 'Is Trigger'");
            }
        }
        
      
        Debug.Log($"[Debug] Tag: {gameObject.tag}");
        Debug.Log($"[Debug] Layer: {LayerMask.LayerToName(gameObject.layer)}");
        
        Debug.Log("=== FIN DEBUGGER ===");
    }
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Debug] ¡TRIGGER DETECTADO EN DEBUGGER! Objeto: {other.gameObject.name}");
        Debug.Log($"[Debug] Tag del objeto: {other.tag}");
        Debug.Log($"[Debug] Layer del objeto: {LayerMask.LayerToName(other.gameObject.layer)}");
        
 
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            Debug.Log($"[Debug] ¡PlayerHealth encontrado! PlayerId: {health.playerId}, IsLocal: {health.isLocalPlayer}");
        }
        else
        {
            Debug.LogWarning($"[Debug] PlayerHealth NO encontrado en {other.gameObject.name}");
            
           
            PlayerHealth parentHealth = other.GetComponentInParent<PlayerHealth>();
            if (parentHealth != null)
            {
                Debug.Log($"[Debug] ¡PlayerHealth encontrado en PADRE! PlayerId: {parentHealth.playerId}");
            }
        }
    }
}
