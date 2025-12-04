using System;
using UnityEngine;

namespace BrickOps.Core
{
    /// <summary>
    /// Estructura para sincronizar disparos por red
    /// </summary>
    [Serializable]
    public class ShootData
    {
        public int shooterId;      // ID del jugador que disparó
        public int targetId;       // ID del jugador impactado (-1 si no impactó a nadie)
        public float damage;       // Daño causado
        public float hitX;         // Posición del impacto
        public float hitY;
        public float hitZ;
        public bool didHit;        // Si impactó algo

        public ShootData() { }

        public ShootData(int shooter, int target, float dmg, Vector3 hitPoint, bool hit)
        {
            shooterId = shooter;
            targetId = target;
            damage = dmg;
            hitX = hitPoint.x;
            hitY = hitPoint.y;
            hitZ = hitPoint.z;
            didHit = hit;
        }

        public Vector3 GetHitPoint()
        {
            return new Vector3(hitX, hitY, hitZ);
        }
    }

    /// <summary>
    /// Estructura para sincronizar muertes por red
    /// </summary>
    [Serializable]
    public class DeathData
    {
        public int victimId;       // ID del jugador que murió
        public int killerId;       // ID del jugador que lo mató

        public DeathData() { }

        public DeathData(int victim, int killer)
        {
            victimId = victim;
            killerId = killer;
        }
    }
}