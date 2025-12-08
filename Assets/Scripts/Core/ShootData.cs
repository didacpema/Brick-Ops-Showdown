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
        public int shooterId;      
        public int targetId;       
        public float damage;   
        public float hitX;      
        public float hitY;
        public float hitZ;
        public bool didHit;        

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
        public int victimId;       
        public int killerId;      
        public DeathData() { }

        public DeathData(int victim, int killer)
        {
            victimId = victim;
            killerId = killer;
        }
    }
}