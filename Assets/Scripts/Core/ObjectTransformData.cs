using System;
using UnityEngine;

namespace BrickOps.Core
{
    /// <summary>
    /// Estructura para sincronizar posición y rotación de objetos por red
    /// Usado para objetos con animaciones como RotationAnimation
    /// </summary>
    [Serializable]
    public class ObjectTransformData
    {
        public int objectId;       // ID único del objeto
        public float posX;         // Posición X
        public float posY;         // Posición Y
        public float posZ;         // Posición Z
        public float rotX;         // Rotación X (Euler)
        public float rotY;         // Rotación Y (Euler)
        public float rotZ;         // Rotación Z (Euler)

        public ObjectTransformData() { }

        public ObjectTransformData(int id, Vector3 position, Vector3 rotation)
        {
            objectId = id;
            posX = position.x;
            posY = position.y;
            posZ = position.z;
            rotX = rotation.x;
            rotY = rotation.y;
            rotZ = rotation.z;
        }

        public Vector3 GetPosition()
        {
            return new Vector3(posX, posY, posZ);
        }

        public Vector3 GetRotation()
        {
            return new Vector3(rotX, rotY, rotZ);
        }
    }
}
