using System;
using UnityEngine;

namespace BrickOps.Core
{
    /// <summary>
    /// Sistema centralizado de eventos para desacoplar componentes
    /// Evita dependencias circulares y facilita la escalabilidad
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }

        #region Events
        // Eventos de jugador
        public event Action<int, bool> OnPlayerSpawned;
        public event Action<int, int> OnPlayerDied;
        public event Action<int, Vector3> OnPlayerRespawned;
        public event Action<int, float, float> OnPlayerHealthChanged;

        // Eventos de combate
        public event Action<int, Vector3, Vector3> OnWeaponFired;
        public event Action<int, int, float, Vector3> OnPlayerHit;

        // Eventos de red
        public event Action<string, string> OnNetworkMessageReceived;
        public event Action<int> OnPlayerConnected;
        public event Action<int> OnPlayerDisconnected;
        public event Action<string> OnNetworkError;

        // Eventos de UI
        public event Action OnUIUpdateRequested;
        public event Action<string> OnKillFeedMessage;
        #endregion

        #region Singleton Setup
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[EventManager] Initialized");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                ClearAllEvents();
                Instance = null;
            }
        }
        #endregion

        #region Event Invokers
        public void InvokePlayerSpawned(int playerId, bool isLocal) => OnPlayerSpawned?.Invoke(playerId, isLocal);
        public void InvokePlayerDied(int victimId, int killerId) => OnPlayerDied?.Invoke(victimId, killerId);
        public void InvokePlayerRespawned(int playerId, Vector3 position) => OnPlayerRespawned?.Invoke(playerId, position);
        public void InvokePlayerHealthChanged(int playerId, float current, float max) => OnPlayerHealthChanged?.Invoke(playerId, current, max);
        public void InvokeWeaponFired(int shooterId, Vector3 origin, Vector3 direction) => OnWeaponFired?.Invoke(shooterId, origin, direction);
        public void InvokePlayerHit(int shooterId, int targetId, float damage, Vector3 hitPoint) => OnPlayerHit?.Invoke(shooterId, targetId, damage, hitPoint);
        public void InvokeNetworkMessageReceived(string messageType, string data) => OnNetworkMessageReceived?.Invoke(messageType, data);
        public void InvokePlayerConnected(int playerId) => OnPlayerConnected?.Invoke(playerId);
        public void InvokePlayerDisconnected(int playerId) => OnPlayerDisconnected?.Invoke(playerId);
        public void InvokeNetworkError(string error) => OnNetworkError?.Invoke(error);
        public void InvokeUIUpdateRequested() => OnUIUpdateRequested?.Invoke();
        public void InvokeKillFeedMessage(string message) => OnKillFeedMessage?.Invoke(message);
        #endregion

        #region Utility Methods
        /// <summary>
        /// Limpia todos los suscriptores de eventos (útil al cambiar de escena)
        /// </summary>
        public void ClearAllEvents()
        {
            OnPlayerSpawned = null;
            OnPlayerDied = null;
            OnPlayerRespawned = null;
            OnPlayerHealthChanged = null;
            OnWeaponFired = null;
            OnPlayerHit = null;
            OnNetworkMessageReceived = null;
            OnPlayerConnected = null;
            OnPlayerDisconnected = null;
            OnNetworkError = null;
            OnUIUpdateRequested = null;
            OnKillFeedMessage = null;

            Debug.Log("[EventManager] All events cleared");
        }
        #endregion
    }
}