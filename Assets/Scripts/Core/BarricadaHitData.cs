using System;
using UnityEngine;

namespace BrickOps.Core
{
    [Serializable]
    public class BarricadaHitData
    {
        public int barricadaId;
        public int damage;

        public BarricadaHitData(int id, int dmg)
        {
            barricadaId = id;
            damage = dmg;
        }
    }
}