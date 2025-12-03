using System;
using UnityEngine;

namespace BrickOps.Core
{    [Serializable]
    public class PlayerState
    {
        // Transform Data
        public int playerId;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;

        // Animation Data
        public bool isWalking;
        public bool isRunning;
        public bool isAiming;
        public bool isCrouching;
        public bool isGrounded;
        public int shootCount;
        public int jumpCount;

        // Constructor vacío requerido por JsonUtility
        public PlayerState() { }

        public PlayerState(int id, Vector3 pos, float rotation)
        {
            playerId = id;
            posX = pos.x;
            posY = pos.y;
            posZ = pos.z;
            rotY = rotation;
            
            // Valores por defecto para animaciones
            isWalking = false;
            isRunning = false;
            isAiming = false;
            isCrouching = false;
            isGrounded = true;
            shootCount = 0;
            jumpCount = 0;
        }

        // Constructor completo con datos de animación
        public PlayerState(int id, Vector3 pos, float rotation, bool walking, bool running, bool aiming, bool crouching, bool grounded, int sCount, int jCount)
        {
            playerId = id;
            posX = pos.x;
            posY = pos.y;
            posZ = pos.z;
            rotY = rotation;
            isWalking = walking;
            isRunning = running;
            isAiming = aiming;
            isCrouching = crouching;
            isGrounded = grounded;
            shootCount = sCount;
            jumpCount = jCount;
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