using UnityEngine;

/// <summary>
/// Componente para detectar colisiones con proyectiles y aplicar daño a la barricada
/// Agregar este script al GameObject de la barricada
/// </summary>
[RequireComponent(typeof(Barricada))]
public class BarricadaCollision : MonoBehaviour
{
    [SerializeField] private int damagePerHit = 10;
    private Barricada barricada;
    private int barricadaId;
    
    void Start()
    {
        barricada = GetComponent<Barricada>();
        
        // Obtener el ID de esta barricada desde el manager
        if (BarricadaManager.Instance != null)
        {
            // Buscar el ID basado en el índice en el manager
            for (int i = 0; i < 100; i++) // máximo 100 barricadas
            {
                if (BarricadaManager.Instance.GetBarricada(i) == barricada)
                {
                    barricadaId = i;
                    Debug.Log($"[BarricadaCollision] Barricada ID found: {barricadaId}");
                    break;
                }
            }
        }
        
        // Asegurar que tiene un collider
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"[BarricadaCollision] No collider found on {gameObject.name}. Adding BoxCollider.");
            gameObject.AddComponent<BoxCollider>();
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Verificar si es un proyectil
        if (collision.gameObject.CompareTag("Bullet") || collision.gameObject.CompareTag("Projectile"))
        {
            Debug.Log($"[BarricadaCollision] Hit by projectile!");
            ApplyDamage(damagePerHit);
            
            // Destruir el proyectil
            Destroy(collision.gameObject);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Alternativa para trigger colliders
        if (other.CompareTag("Bullet") || other.CompareTag("Projectile"))
        {
            Debug.Log($"[BarricadaCollision] Hit by projectile (trigger)!");
            ApplyDamage(damagePerHit);
            
            // Destruir el proyectil
            Destroy(other.gameObject);
        }
    }
    
    private void ApplyDamage(int damage)
    {
        if (BarricadaManager.Instance != null)
        {
            // Si somos servidor, aplicar daño directamente
            if (BarricadaManager.Instance.IsServer())
            {
                BarricadaManager.Instance.ApplyDamageToBarricada(barricadaId, damage);
            }
            else
            {
                // Si somos cliente, enviar mensaje al servidor
                SendDamageToServer(barricadaId, damage);
            }
        }
        else
        {
            // Fallback: aplicar daño localmente
            barricada.TakeDamage(damage);
        }
    }
      private void SendDamageToServer(int id, int damage)
    {
        // Buscar el cliente UDP y enviar mensaje
        UDPClient_Select client = FindFirstObjectByType<UDPClient_Select>();
        if (client != null)
        {
            string msg = $"BARRICADA_DAMAGE:{id}:{damage}";
            
            // Usar reflexión para acceder al método SendToServer
            var sendMethod = client.GetType().GetMethod("SendToServer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (sendMethod != null)
            {
                sendMethod.Invoke(client, new object[] { msg });
                Debug.Log($"[BarricadaCollision] Sent damage to server: {msg}");
            }
        }
    }
}
