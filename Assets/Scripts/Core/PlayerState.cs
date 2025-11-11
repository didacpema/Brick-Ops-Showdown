using System;
using UnityEngine;

namespace BrickOps.Core
{
    [Serializable]
    public class PlayerState
    {
        public int playerId;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;

        // Constructor vacío requerido por JsonUtility
        public PlayerState() { }

        public PlayerState(int id, Vector3 pos, float rotation)
        {
            playerId = id;
            posX = pos.x;
            posY = pos.y;
            posZ = pos.z;
            rotY = rotation;
        }

        public Vector3 GetPosition()
        {
            return new Vector3(posX, posY, posZ);
        }

        // Actualiza los campos desde un Transform (útil desde InputManager)
        public void UpdateFromTransform(Transform t)
        {
            Vector3 p = t.position;
            posX = p.x;
            posY = p.y;
            posZ = p.z;
            rotY = t.eulerAngles.y;
        }

        // Aplica los valores guardados a un Transform (útil para otherPlayer)
        public void ApplyToTransform(Transform t)
        {
            t.position = GetPosition();
            t.rotation = Quaternion.Euler(0f, rotY, 0f);
        }

        // Serialización/Deserialización helpers
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static PlayerState FromJson(string json)
        {
            return JsonUtility.FromJson<PlayerState>(json);
        }
    }
}