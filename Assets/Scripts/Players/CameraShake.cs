using UnityEngine;

namespace BrickOps.Players
{
    /// <summary>
    /// Sistema de camera shake con múltiples perfiles de intensidad
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        #region Shake Profile
        [System.Serializable]
        public class ShakeProfile
        {
            public float duration = 0.2f;
            public float magnitude = 0.1f;
            public float frequency = 25f;
            public AnimationCurve dampingCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        }
        #endregion

        #region Inspector Variables
        [Header("Shake Profiles")]
        [Tooltip("Perfil para disparo normal")]
        public ShakeProfile shootShake = new ShakeProfile 
        { 
            duration = 0.15f, 
            magnitude = 0.08f, 
            frequency = 30f 
        };

        [Tooltip("Perfil para impactos")]
        public ShakeProfile impactShake = new ShakeProfile 
        { 
            duration = 0.25f, 
            magnitude = 0.15f, 
            frequency = 20f 
        };

        [Tooltip("Perfil para explosiones")]
        public ShakeProfile explosionShake = new ShakeProfile 
        { 
            duration = 0.5f, 
            magnitude = 0.3f, 
            frequency = 15f 
        };

        [Header("Settings")]
        [Tooltip("Multiplicador global de intensidad")]
        [Range(0f, 2f)]
        public float globalIntensity = 1f;
        #endregion

        #region Private Variables
        private Vector3 originalLocalPosition;
        private Vector3 currentShakeOffset;
        private float shakeTimer;
        private float shakeDuration;
        private float shakeMagnitude;
        private float shakeFrequency;
        private AnimationCurve currentDampingCurve;
        private bool isShaking;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            originalLocalPosition = transform.localPosition;
        }

        void Update()
        {
            if (isShaking)
            {
                UpdateShake();
            }
        }
        #endregion

        #region Shake Logic
        void UpdateShake()
        {
            if (shakeTimer > 0)
            {
                shakeTimer -= Time.deltaTime;

                float progress = 1f - (shakeTimer / shakeDuration);
                float damping = currentDampingCurve.Evaluate(progress);
                
                float x = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2f;
                float y = (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) - 0.5f) * 2f;
                float z = (Mathf.PerlinNoise(Time.time * shakeFrequency, Time.time * shakeFrequency) - 0.5f) * 2f;

                currentShakeOffset = new Vector3(x, y, z) * shakeMagnitude * damping * globalIntensity;
                // NO aplicamos la posición aquí, la aplica CameraController
            }
            else
            {
                isShaking = false;
                currentShakeOffset = Vector3.zero;
            }
        }

        void TriggerShake(ShakeProfile profile)
        {
            if (profile == null) return;

            shakeDuration = profile.duration;
            shakeMagnitude = profile.magnitude;
            shakeFrequency = profile.frequency;
            currentDampingCurve = profile.dampingCurve;
            shakeTimer = shakeDuration;
            isShaking = true;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Activa el shake de disparo
        /// </summary>
        public void ShakeOnShoot()
        {
            TriggerShake(shootShake);
        }

        /// <summary>
        /// Activa el shake de impacto
        /// </summary>
        public void ShakeOnImpact()
        {
            TriggerShake(impactShake);
        }

        /// <summary>
        /// Activa el shake de explosión
        /// </summary>
        public void ShakeOnExplosion()
        {
            TriggerShake(explosionShake);
        }

        /// <summary>
        /// Shake personalizado
        /// </summary>
        public void CustomShake(float duration, float magnitude, float frequency = 25f)
        {
            ShakeProfile custom = new ShakeProfile
            {
                duration = duration,
                magnitude = magnitude,
                frequency = frequency
            };
            TriggerShake(custom);
        }

        /// <summary>
        /// Detiene el shake inmediatamente
        /// </summary>
        public void StopShake()
        {
            isShaking = false;
            shakeTimer = 0f;
            currentShakeOffset = Vector3.zero;
        }

        /// <summary>
        /// Reinicia la posición original
        /// </summary>
        public void ResetOriginalPosition()
        {
            originalLocalPosition = transform.localPosition;
        }

        /// <summary>
        /// Obtiene el offset actual del shake (para otros sistemas que también muevan la cámara)
        /// </summary>
        public Vector3 GetCurrentShakeOffset()
        {
            return currentShakeOffset;
        }

        /// <summary>
        /// Obtiene la posición original guardada
        /// </summary>
        public Vector3 GetOriginalPosition()
        {
            return originalLocalPosition;
        }
        #endregion
    }
}
