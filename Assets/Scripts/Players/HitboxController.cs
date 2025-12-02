using UnityEngine;

namespace BrickOps.Players
{
    /// <summary>
    /// Tipos de hitbox para detectar diferentes zonas del jugador
    /// </summary>
    public enum HitboxType
    {
        Body,    // Pecho/Cuerpo
        Head     // Cabeza
    }

    /// <summary>
    /// Componente que se añade a los colliders específicos (cabeza, pecho)
    /// para identificar qué parte del jugador fue impactada
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
            // Buscar el PlayerHealth en el padre
            if (playerHealth == null)
            {
                playerHealth = GetComponentInParent<PlayerHealth>();
                
                if (playerHealth == null)
                {
                    Debug.LogError($"[HitboxController] No se encontró PlayerHealth en el padre de {gameObject.name}");
                }
            }
        }

        /// <summary>
        /// Obtiene el PlayerHealth asociado a esta hitbox
        /// </summary>
        public PlayerHealth GetPlayerHealth()
        {
            return playerHealth;
        }

        /// <summary>
        /// Obtiene el tipo de hitbox
        /// </summary>
        public HitboxType GetHitboxType()
        {
            return hitboxType;
        }
    }
}
