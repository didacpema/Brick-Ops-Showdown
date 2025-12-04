using System.Collections.Generic;
using UnityEngine;
using TMPro;
using BrickOps.Core;
using BrickOps.Players;

namespace BrickOps.UI
{
    /// <summary>
    /// Gestiona toda la UI del juego
    /// Separado del GameController para mejor organización
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        #region Inspector Variables
        [Header("HUD")]
        public TMP_Text playerInfoText;
        public TMP_Text healthText;
        public TMP_Text ammoText;
        public TMP_Text fpsText;

        [Header("Kill Feed")]
        public TMP_Text killFeedText;
        public int maxKillFeedLines = 5;
        public float killFeedDuration = 5f;

        [Header("Scoreboard")]
        public GameObject scoreboardPanel;
        public TMP_Text scoreboardText;

        [Header("Debug")]
        public TMP_Text debugInfoText;
        public bool showDebugInfo = false;
        #endregion

        #region Private Variables
        private List<KillFeedEntry> killFeedEntries = new List<KillFeedEntry>();
        private float lastFPSUpdate = 0f;
        private int frameCount = 0;
        private float fps = 0f;
        // ===== Network HUD throttling =====
        private float lastNetUpdateTime = 0f;
        private float netUpdateInterval = 1f; // actualizar cada 1s
        private string cachedNetLine = "NET: N/A";
        private int smoothedPing = -1;
        #endregion

        #region Nested Classes
        private class KillFeedEntry
        {
            public string message;
            public float timestamp;

            public KillFeedEntry(string msg)
            {
                message = msg;
                timestamp = Time.time;
            }

            public bool IsExpired(float duration)
            {
                return Time.time - timestamp > duration;
            }
        }
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            SetupEventListeners();
            InitializeUI();
        }

        void Update()
        {
            UpdateFPS();
            UpdateKillFeed();
            UpdatePlayerInfo();
            
            if (showDebugInfo)
            {
                UpdateDebugInfo();
            }

            // Scoreboard toggle
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleScoreboard(true);
            }
            else if (Input.GetKeyUp(KeyCode.Tab))
            {
                ToggleScoreboard(false);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                RemoveEventListeners();
                Instance = null;
            }
        }
        #endregion

        #region Initialization
        void InitializeUI()
        {
            if (killFeedText != null)
            {
                killFeedText.text = "";
            }

            if (scoreboardPanel != null)
            {
                scoreboardPanel.SetActive(false);
            }

            if (fpsText != null)
            {
                fpsText.gameObject.SetActive(true);
            }

            if (debugInfoText != null)
            {
                debugInfoText.gameObject.SetActive(showDebugInfo);
            }

            Debug.Log("[UIManager] Initialized");
        }

        void SetupEventListeners()
        {
            if (EventManager.Instance == null)
                return;

            EventManager.Instance.OnKillFeedMessage += AddKillFeedMessage;
            EventManager.Instance.OnPlayerHealthChanged += UpdateHealth;
        }

        void RemoveEventListeners()
        {
            if (EventManager.Instance == null)
                return;

            EventManager.Instance.OnKillFeedMessage -= AddKillFeedMessage;
            EventManager.Instance.OnPlayerHealthChanged -= UpdateHealth;
        }
        #endregion

        #region Player Info
        void UpdatePlayerInfo()
        {
            if (playerInfoText == null || PlayerManager.Instance == null)
                return;

            int playerId = PlayerManager.Instance.LocalPlayerId;
            int otherPlayers = PlayerManager.Instance.RemotePlayerCount;
            
            GameObject localPlayer = PlayerManager.Instance.LocalPlayer;
            if (localPlayer == null)
                return;

            Vector3 pos = localPlayer.transform.position;
            string status = otherPlayers > 0 ? $"CONNECTED ({otherPlayers} other)" : "SOLO";

            // Actualizar stats de red solo cada intervalo
            if (GameController.Instance != null && Time.time - lastNetUpdateTime >= netUpdateInterval)
            {
                GameController.Instance.GetNetworkStats(out int pingMs, out int sent, out int recv, out float pps);
                // Suavizado de ping
                if (pingMs >= 0)
                {
                    if (smoothedPing < 0)
                        smoothedPing = pingMs;
                    else
                        smoothedPing = Mathf.RoundToInt(Mathf.Lerp(smoothedPing, pingMs, 0.3f));
                }
                else
                {
                    smoothedPing = -1;
                }
                string pingStr = smoothedPing < 0 ? "N/A" : $"{smoothedPing}ms";
                cachedNetLine = $"NET: ping {pingStr} | {sent}/{recv} pkts | {pps:F1} pps";
                lastNetUpdateTime = Time.time;
            }

            playerInfoText.text = $"Player {playerId} [{status}]\n" +
                                 $"Pos: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})\n" +
                                 cachedNetLine + "\n\n" +
                                 "WASD: Move | Mouse: Look | Click: Shoot\n" +
                                 "Space: Jump | Tab: Scoreboard | ESC: Exit";
        }

        void UpdateHealth(int playerId, float currentHealth, float maxHealth)
        {
            if (healthText == null || playerId != PlayerManager.Instance?.LocalPlayerId)
                return;

            float percentage = (currentHealth / maxHealth) * 100f;
            healthText.text = $"HP: {percentage:F0}%";

            // Color según vida
            if (percentage > 60f)
                healthText.color = Color.green;
            else if (percentage > 30f)
                healthText.color = Color.yellow;
            else
                healthText.color = Color.red;
        }
        #endregion

        #region Kill Feed
        void AddKillFeedMessage(string message)
        {
            killFeedEntries.Add(new KillFeedEntry(message));
            
            // Limitar número de entradas
            while (killFeedEntries.Count > maxKillFeedLines)
            {
                killFeedEntries.RemoveAt(0);
            }

            UpdateKillFeedDisplay();
        }

        void UpdateKillFeed()
        {
            // Eliminar mensajes expirados
            killFeedEntries.RemoveAll(entry => entry.IsExpired(killFeedDuration));

            // Actualizar display si cambió
            if (killFeedEntries.Count == 0 && !string.IsNullOrEmpty(killFeedText?.text))
            {
                UpdateKillFeedDisplay();
            }
        }

        void UpdateKillFeedDisplay()
        {
            if (killFeedText == null)
                return;

            if (killFeedEntries.Count == 0)
            {
                killFeedText.text = "";
                return;
            }

            string[] messages = new string[killFeedEntries.Count];
            for (int i = 0; i < killFeedEntries.Count; i++)
            {
                messages[i] = killFeedEntries[i].message;
            }

            killFeedText.text = string.Join("\n", messages);
        }
        #endregion

        #region Scoreboard
        void ToggleScoreboard(bool show)
        {
            if (scoreboardPanel == null)
                return;

            scoreboardPanel.SetActive(show);

            if (show)
            {
                UpdateScoreboard();
            }
        }

        void UpdateScoreboard()
        {
            if (scoreboardText == null || PlayerManager.Instance == null)
                return;

            string content = "=== SCOREBOARD ===\n\n";
            
            // Jugador local
            int localId = PlayerManager.Instance.LocalPlayerId;
            GameObject localPlayer = PlayerManager.Instance.LocalPlayer;
            
            if (localPlayer != null)
            {
                PlayerHealth health = localPlayer.GetComponent<PlayerHealth>();
                float hp = health != null ? (health.GetHealthPercentage() * 100f) : 100f;
                
                content += $"[YOU] Player {localId} - HP: {hp:F0}%\n";
            }

            // Jugadores remotos
            foreach (var kvp in PlayerManager.Instance.RemotePlayers)
            {
                int playerId = kvp.Key;
                GameObject player = kvp.Value;

                if (player != null && player.activeSelf)
                {
                    PlayerHealth health = player.GetComponent<PlayerHealth>();
                    float hp = health != null ? (health.GetHealthPercentage() * 100f) : 100f;
                    
                    content += $"Player {playerId} - HP: {hp:F0}%\n";
                }
                else
                {
                    content += $"Player {playerId} - DEAD\n";
                }
            }

            scoreboardText.text = content;
        }
        #endregion

        #region FPS Counter
        void UpdateFPS()
        {
            if (fpsText == null)
                return;

            frameCount++;
            
            if (Time.time - lastFPSUpdate >= 0.5f)
            {
                fps = frameCount / (Time.time - lastFPSUpdate);
                frameCount = 0;
                lastFPSUpdate = Time.time;

                fpsText.text = $"FPS: {fps:F0}";

                // Color según rendimiento
                if (fps >= 60f)
                    fpsText.color = Color.green;
                else if (fps >= 30f)
                    fpsText.color = Color.yellow;
                else
                    fpsText.color = Color.red;
            }
        }
        #endregion

        #region Debug Info
        void UpdateDebugInfo()
        {
            if (debugInfoText == null || GameController.Instance == null)
                return;

            string info = GameController.Instance.GetDebugInfo();
            debugInfoText.text = $"[DEBUG]\n{info}";
        }

        public void ToggleDebugInfo()
        {
            showDebugInfo = !showDebugInfo;
            
            if (debugInfoText != null)
            {
                debugInfoText.gameObject.SetActive(showDebugInfo);
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Muestra un mensaje temporal en pantalla
        /// </summary>
        public void ShowNotification(string message, float duration = 3f)
        {
            // Implementar sistema de notificaciones temporales
            Debug.Log($"[Notification] {message}");
        }

        /// <summary>
        /// Actualiza el contador de munición
        /// </summary>
        public void UpdateAmmo(int current, int max)
        {
            if (ammoText != null)
            {
                ammoText.text = $"Ammo: {current}/{max}";
            }
        }
        #endregion
    }
}