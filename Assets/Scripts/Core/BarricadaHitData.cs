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

        public static BarricadaHitData FromJson(string json) => JsonUtility.FromJson<BarricadaHitData>(json);
    }
}