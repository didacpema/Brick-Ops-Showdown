using System;
using UnityEngine;

namespace BrickOps.Core
{
    /// <summary>
    /// Datos de un health pack para sincronización en red
    /// </summary>
    [Serializable]
    public class HealthPackData
    {
        public int healthPackId;      // ID único del health pack
        public bool isActive;          // Si está disponible para recoger
        public int collectorId;        // ID del jugador que lo recogió
        
        public HealthPackData(int id, bool active, int collector = -1)
        {
            healthPackId = id;
            isActive = active;
            collectorId = collector;
        }
    }
}
