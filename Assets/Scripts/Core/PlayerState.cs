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
    }
}