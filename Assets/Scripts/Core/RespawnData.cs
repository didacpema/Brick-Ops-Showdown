using System;
using UnityEngine;

namespace BrickOps.Networking
{
    [Serializable]
    public class RespawnData
    {
        public int playerId;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;

        public RespawnData() { }

        public RespawnData(int playerId, Vector3 position, float rotation)
        {
            this.playerId = playerId;
            posX = position.x;
            posY = position.y;
            posZ = position.z;
            rotY = rotation;
        }

        public Vector3 GetPosition() => new Vector3(posX, posY, posZ);
    }
}
