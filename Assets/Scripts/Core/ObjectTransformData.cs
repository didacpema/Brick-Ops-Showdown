using System;
using UnityEngine;

namespace BrickOps.Core
{
    /// Estructura para sincronizar posición y rotación de objetos
    [Serializable]
    public class ObjectTransformData
    {
        public int objectId;      
        public float posX;         
        public float posY;      
        public float posZ;         
        public float rotX;         
        public float rotY;         
        public float rotZ;         

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
