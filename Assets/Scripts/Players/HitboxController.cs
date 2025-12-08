using UnityEngine;

namespace BrickOps.Players
{
    /// <summary>
    /// Tipos de hitbox para detectar diferentes zonas del jugador
    /// </summary>
    public enum HitboxType
    {
        Body,    
        Head    
    }

    /// <summary>
    /// Componente que se añade a los colliders específicos
    /// </summary>
    public class HitboxController : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("Tipo de hitbox (Body o Head)")]
        public HitboxType hitboxType = HitboxType.Body;

        [Tooltip("Referencia al PlayerHealth del jugador (se asigna automáticamente)")]
        public PlayerHealth playerHealth;

        void Awake()
        {
            if (playerHealth == null)
            {
                playerHealth = GetComponentInParent<PlayerHealth>();
                
                if (playerHealth == null)
                {
                    Debug.LogError($"[HitboxController] No se encontró PlayerHealth en el padre de {gameObject.name}");
                }
            }
        }
        public PlayerHealth GetPlayerHealth()
        {
            return playerHealth;
        }
        public HitboxType GetHitboxType()
        {
            return hitboxType;
        }
    }
}
