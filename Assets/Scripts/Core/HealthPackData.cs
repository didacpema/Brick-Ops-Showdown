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
        public int healthPackId;     
        public bool isActive;        
        public int collectorId;       
        
        public HealthPackData(int id, bool active, int collector = -1)
        {
            healthPackId = id;
            isActive = active;
            collectorId = collector;
        }
    }
}
