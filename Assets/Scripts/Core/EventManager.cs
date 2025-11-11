using System;
using System.Collections.Generic;
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

        #region Event Delegates
        // Eventos de jugador
        public delegate void PlayerSpawnedHandler(int playerId, bool isLocal);
        public delegate void PlayerDiedHandler(int victimId, int killerId);
        public delegate void PlayerRespawnedHandler(int playerId, Vector3 position);
        public delegate void PlayerHealthChangedHandler(int playerId, float currentHealth, float maxHealth);

        // Eventos de combate
        public delegate void WeaponFiredHandler(int shooterId, Vector3 origin, Vector3 direction);
        public delegate void PlayerHitHandler(int shooterId, int targetId, float damage, Vector3 hitPoint);

        // Eventos de red
        public delegate void NetworkMessageReceivedHandler(string messageType, string data);
        public delegate void PlayerConnectedHandler(int playerId);
        public delegate void PlayerDisconnectedHandler(int playerId);
        public delegate void NetworkErrorHandler(string error);

        // Eventos de UI
        public delegate void UIUpdateRequestedHandler();
        public delegate void KillFeedMessageHandler(string message);
        #endregion

        #region Events
        public event PlayerSpawnedHandler OnPlayerSpawned;
        public event PlayerDiedHandler OnPlayerDied;
        public event PlayerRespawnedHandler OnPlayerRespawned;
        public event PlayerHealthChangedHandler OnPlayerHealthChanged;

        public event WeaponFiredHandler OnWeaponFired;
        public event PlayerHitHandler OnPlayerHit;

        public event NetworkMessageReceivedHandler OnNetworkMessageReceived;
        public event PlayerConnectedHandler OnPlayerConnected;
        public event PlayerDisconnectedHandler OnPlayerDisconnected;
        public event NetworkErrorHandler OnNetworkError;

        public event UIUpdateRequestedHandler OnUIUpdateRequested;
        public event KillFeedMessageHandler OnKillFeedMessage;
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
                Instance = null;
            }
        }
        #endregion

        #region Event Invokers
        public void InvokePlayerSpawned(int playerId, bool isLocal)
        {
            OnPlayerSpawned?.Invoke(playerId, isLocal);
        }

        public void InvokePlayerDied(int victimId, int killerId)
        {
            OnPlayerDied?.Invoke(victimId, killerId);
        }

        public void InvokePlayerRespawned(int playerId, Vector3 position)
        {
            OnPlayerRespawned?.Invoke(playerId, position);
        }

        public void InvokePlayerHealthChanged(int playerId, float current, float max)
        {
            OnPlayerHealthChanged?.Invoke(playerId, current, max);
        }

        public void InvokeWeaponFired(int shooterId, Vector3 origin, Vector3 direction)
        {
            OnWeaponFired?.Invoke(shooterId, origin, direction);
        }

        public void InvokePlayerHit(int shooterId, int targetId, float damage, Vector3 hitPoint)
        {
            OnPlayerHit?.Invoke(shooterId, targetId, damage, hitPoint);
        }

        public void InvokeNetworkMessageReceived(string messageType, string data)
        {
            OnNetworkMessageReceived?.Invoke(messageType, data);
        }

        public void InvokePlayerConnected(int playerId)
        {
            OnPlayerConnected?.Invoke(playerId);
        }

        public void InvokePlayerDisconnected(int playerId)
        {
            OnPlayerDisconnected?.Invoke(playerId);
        }

        public void InvokeNetworkError(string error)
        {
            OnNetworkError?.Invoke(error);
        }

        public void InvokeUIUpdateRequested()
        {
            OnUIUpdateRequested?.Invoke();
        }

        public void InvokeKillFeedMessage(string message)
        {
            OnKillFeedMessage?.Invoke(message);
        }
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