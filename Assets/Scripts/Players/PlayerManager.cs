using System.Collections.Generic;
using UnityEngine;
using BrickOps.Core;

namespace BrickOps.Players
{
    /// <summary>
    /// Gestiona todos los jugadores en la partida
    /// Centraliza la lógica de spawn, tracking y actualización
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }

        #region Inspector Variables
        [Header("Prefabs")]
        [Tooltip("Prefab del jugador")]
        public GameObject playerPrefab;

        [Header("Spawn Configuration")]
        [Tooltip("Puntos de spawn")]
        public Transform[] spawnPoints;

        [Header("Visual Configuration")]
        public Material localPlayerMaterial;
        public Material remotePlayerMaterial;
        public Color localPlayerColor = Color.blue;
        public Color remotePlayerColor = Color.red;
        #endregion

        #region Private Variables
        private GameObject localPlayerObject;
        private int localPlayerId = -1;
        private Dictionary<int, GameObject> remotePlayers = new Dictionary<int, GameObject>();
        private Dictionary<int, PlayerState> playerStates = new Dictionary<int, PlayerState>();
        private List<Vector3> availableSpawnPoints = new List<Vector3>();
        #endregion

        #region Properties
        public GameObject LocalPlayer => localPlayerObject;
        public int LocalPlayerId => localPlayerId;
        public int RemotePlayerCount => remotePlayers.Count;
        public IReadOnlyDictionary<int, GameObject> RemotePlayers => remotePlayers;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeSpawnPoints();
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

        #region Initialization
        void InitializeSpawnPoints()
        {
            availableSpawnPoints.Clear();

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                foreach (var point in spawnPoints)
                {
                    if (point != null)
                    {
                        availableSpawnPoints.Add(point.position);
                    }
                }
            }

            // Spawn points por defecto si no hay configurados
            if (availableSpawnPoints.Count == 0)
            {
                availableSpawnPoints.Add(new Vector3(-5, 1, 0));
                availableSpawnPoints.Add(new Vector3(5, 1, 0));
                availableSpawnPoints.Add(new Vector3(0, 1, 5));
                availableSpawnPoints.Add(new Vector3(0, 1, -5));
            }

            Debug.Log($"[PlayerManager] Initialized with {availableSpawnPoints.Count} spawn points");
        }
        #endregion

        #region Local Player Management
        /// <summary>
        /// Crea el jugador local
        /// </summary>
        public GameObject SpawnLocalPlayer(int playerId)
        {
            if (localPlayerObject != null)
            {
                Debug.LogWarning("[PlayerManager] Local player already exists!");
                return localPlayerObject;
            }

            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerManager] PlayerPrefab is null!");
                return null;
            }

            localPlayerId = playerId;
            Vector3 spawnPos = GetSpawnPosition(playerId);
            
            localPlayerObject = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            localPlayerObject.name = $"Player_{playerId}_LOCAL";

            PlayerController controller = localPlayerObject.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.InitializeAsLocal(playerId);
                controller.SetVisuals(localPlayerMaterial, localPlayerColor);
            }
            else
            {
                Debug.LogError("[PlayerManager] PlayerController component missing on prefab! Please add it.");
                Destroy(localPlayerObject);
                return null;
            }

            EventManager.Instance?.InvokePlayerSpawned(playerId, true);

            Debug.Log($"[PlayerManager] Local player {playerId} spawned at {spawnPos}");
            return localPlayerObject;
        }
        #endregion

        #region Remote Player Management
        /// <summary>
        /// Crea un jugador remoto
        /// </summary>
        public GameObject SpawnRemotePlayer(int playerId, Vector3 position, float rotation)
        {
            if (remotePlayers.ContainsKey(playerId))
            {
                Debug.LogWarning($"[PlayerManager] Remote player {playerId} already exists!");
                return remotePlayers[playerId];
            }

            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerManager] PlayerPrefab is null!");
                return null;
            }

            GameObject remotePlayer = Instantiate(playerPrefab, position, Quaternion.Euler(0, rotation, 0));
            remotePlayer.name = $"Player_{playerId}_REMOTE";

            PlayerController controller = remotePlayer.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.InitializeAsRemote(playerId);
                controller.SetVisuals(remotePlayerMaterial, remotePlayerColor);
            }
            else
            {
                Debug.LogError("[PlayerManager] PlayerController component missing on prefab! Please add it.");
                Destroy(remotePlayer);
                return null;
            }

            remotePlayers[playerId] = remotePlayer;
            EventManager.Instance?.InvokePlayerSpawned(playerId, false);

            Debug.Log($"[PlayerManager] Remote player {playerId} spawned at {position}");
            return remotePlayer;
        }

        /// <summary>
        /// Elimina un jugador remoto
        /// </summary>
        public void RemoveRemotePlayer(int playerId)
        {
            if (remotePlayers.TryGetValue(playerId, out GameObject player))
            {
                Destroy(player);
                remotePlayers.Remove(playerId);
                playerStates.Remove(playerId);

                Debug.Log($"[PlayerManager] Remote player {playerId} removed");
            }
        }
        #endregion

        #region State Management        
        /// <summary>
        /// Actualiza el estado de un jugador remoto
        /// </summary>
        public void UpdatePlayerState(int playerId, PlayerState state)
        {
            if (playerId == localPlayerId)
                return;

            playerStates[playerId] = state;

            // Crear jugador si no existe
            if (!remotePlayers.ContainsKey(playerId))
            {
                SpawnRemotePlayer(playerId, state.GetPosition(), state.rotY);
            }
            
            // Aplicar animaciones al jugador remoto
            if (remotePlayers.TryGetValue(playerId, out GameObject player))
            {
                RemotePlayerAnimator remoteAnimator = player.GetComponent<RemotePlayerAnimator>();
                if (remoteAnimator != null)
                {
                    remoteAnimator.ApplyAnimationState(state);
                }
            }
        }

        /// <summary>
        /// Actualiza las posiciones de todos los jugadores remotos
        /// </summary>
        public void UpdateRemotePlayers()
        {
            foreach (var kvp in playerStates)
            {
                int playerId = kvp.Key;
                PlayerState state = kvp.Value;

                if (remotePlayers.TryGetValue(playerId, out GameObject player))
                {
                    if (player != null && player.activeSelf)
                    {
                        // Interpolación suave
                        Vector3 targetPos = state.GetPosition();
                        Quaternion targetRot = Quaternion.Euler(0, state.rotY, 0);

                        player.transform.position = Vector3.Lerp(
                            player.transform.position,
                            targetPos,
                            Time.deltaTime * 10f
                        );

                        player.transform.rotation = Quaternion.Lerp(
                            player.transform.rotation,
                            targetRot,
                            Time.deltaTime * 10f
                        );
                    }
                }
            }
        }
        #endregion

        #region Spawn Position
        /// <summary>
        /// Obtiene una posición de spawn para un jugador
        /// </summary>
        public Vector3 GetSpawnPosition(int playerId)
        {
            if (availableSpawnPoints.Count == 0)
                return Vector3.up;

            int index = (playerId - 1) % availableSpawnPoints.Count;
            return availableSpawnPoints[index];
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Obtiene el GameObject de un jugador por ID
        /// </summary>
        public GameObject GetPlayer(int playerId)
        {
            if (playerId == localPlayerId)
                return localPlayerObject;

            remotePlayers.TryGetValue(playerId, out GameObject player);
            return player;
        }

        /// <summary>
        /// Verifica si un jugador existe
        /// </summary>
        public bool PlayerExists(int playerId)
        {
            return playerId == localPlayerId || remotePlayers.ContainsKey(playerId);
        }

        /// <summary>
        /// Limpia todos los jugadores
        /// </summary>
        public void ClearAllPlayers()
        {
            if (localPlayerObject != null)
            {
                Destroy(localPlayerObject);
                localPlayerObject = null;
            }

            foreach (var player in remotePlayers.Values)
            {
                if (player != null)
                {
                    Destroy(player);
                }
            }

            remotePlayers.Clear();
            playerStates.Clear();
            localPlayerId = -1;

            Debug.Log("[PlayerManager] All players cleared");
        }
        #endregion
    }
}