using System.Collections.Generic;
using UnityEngine;
using TMPro;
using BrickOps.Core;
using BrickOps.Players;

namespace BrickOps.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        #region Inspector Variables
        [Header("--- Textos Principales")]
        [Tooltip("Texto de ID y Conexión (F1)")]
        public TMP_Text statusText; 

        [Tooltip("Texto de Vida (HUD Combate)")]
        public TMP_Text healthText;
        [Tooltip("Texto de Munición (HUD Combate)")]
        public TMP_Text ammoText;

        [Tooltip("Texto de Red y FPS")]
        public TMP_Text networkText;
        
        [Tooltip("Texto de Posición")]
        public TMP_Text positionText;

        [Tooltip("Texto de Ayuda")]
        public TMP_Text helpText;

        [Header("Kill Feed")]
        public TMP_Text killFeedText;
        public int maxKillFeedLines = 5;
        public float killFeedDuration = 5f;

        [Header("Scoreboard")]
        public GameObject scoreboardPanel;
        public TMP_Text scoreboardText;

        [Header("Debug")]
        public TMP_Text debugInfoText;

        [Header("Debug Visuals")]
        [Tooltip("Script que renderiza los colliders (Arrastrar aquí)")]
        public MonoBehaviour colliderVisualizer;
        #endregion

        #region Private Variables
        private List<KillFeedEntry> killFeedEntries = new List<KillFeedEntry>();
        private float lastFPSUpdate = 0f;
        private int frameCount = 0;
        private float fps = 0f;
        private float lastNetUpdateTime = 0f;
        private float netUpdateInterval = 1f;
        private string cachedNetLine = "Calculando...";
        private int smoothedPing = -1;
        #endregion

        #region Nested Classes
        private class KillFeedEntry
        {
            public string message;
            public float timestamp;
            public KillFeedEntry(string msg) { message = msg; timestamp = Time.time; }
            public bool IsExpired(float duration) => Time.time - timestamp > duration;
        }
        #endregion

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

void Start()
        {
            SetupEventListeners();
            InitializeUI();

            SetTextVisibility(helpText, false);      
            SetTextVisibility(statusText, true);     
            SetTextVisibility(networkText, false);   
            SetTextVisibility(positionText, false);  
    
            SetTextVisibility(healthText, true);
            SetTextVisibility(ammoText, true);

            if (colliderVisualizer != null) colliderVisualizer.enabled = false;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) ToggleTextVisibility(statusText);
            if (Input.GetKeyDown(KeyCode.F2)) ToggleTextVisibility(positionText);
            if (Input.GetKeyDown(KeyCode.F3)) ToggleTextVisibility(helpText);
            if (Input.GetKeyDown(KeyCode.F4)) ToggleTextVisibility(networkText);
            if (Input.GetKeyDown(KeyCode.F6))
                {
                    BrickOps.Utils.ColliderVisualizer.ShowGizmos = !BrickOps.Utils.ColliderVisualizer.ShowGizmos;
                    
                    Debug.Log($"Colliders: {(BrickOps.Utils.ColliderVisualizer.ShowGizmos ? "ON" : "OFF")}");
                }

            if (networkText != null && networkText.gameObject.activeSelf) UpdateNetworkAndFPS();
            
            UpdateKillFeed();
            UpdateDynamicText();

            if (Input.GetKeyDown(KeyCode.Tab)) ToggleScoreboard(true);
            else if (Input.GetKeyUp(KeyCode.Tab)) ToggleScoreboard(false);
        }

        void OnDestroy()
        {
            if (Instance == this) RemoveEventListeners();
        }

        #region UI Logic & Toggles
        public void ToggleTextVisibility(TMP_Text textComponent)
        {
            if (textComponent != null) 
                textComponent.gameObject.SetActive(!textComponent.gameObject.activeSelf);
        }

        public void SetTextVisibility(TMP_Text textComponent, bool visible)
        {
            if (textComponent != null) 
                textComponent.gameObject.SetActive(visible);
        }

        void InitializeUI()
        {
            if (killFeedText != null) killFeedText.text = "";
            if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
            if (helpText != null) 
                helpText.text = "F1: Ayuda | F2: ID | F3: Red | F4: Pos | F5: HUD\nTAB: Scoreboard | ESC: Menú | F6: Colliders";
        }
        #endregion

        #region Text Updates
        void UpdateDynamicText()
        {
            if (PlayerManager.Instance == null) return;
            
            if (IsVisible(statusText))
            {
                int playerId = PlayerManager.Instance.LocalPlayerId;
                int otherPlayers = PlayerManager.Instance.RemotePlayerCount;
                string connectionStatus = otherPlayers > 0 ? $"ONLINE ({otherPlayers + 1})" : "SOLO";
                
                statusText.text = $"ID: {playerId} | {connectionStatus}";
            }
            if (IsVisible(positionText))
            {
                GameObject localPlayer = PlayerManager.Instance.LocalPlayer;
                if (localPlayer != null)
                {
                    Vector3 pos = localPlayer.transform.position;
                    positionText.text = $"{pos.x:F0}, {pos.y:F0}, {pos.z:F0}";
                }
            }
            if (IsVisible(networkText))
            {
                networkText.text = cachedNetLine;
            }

            if (IsVisible(healthText))
            {
                GameObject localPlayer = PlayerManager.Instance.LocalPlayer;
                if (localPlayer != null)
                {
                    // Obtenemos el componente PlayerHealth (es rápido, pero podrías cachearlo si quieres optimizar al máximo)
                    PlayerHealth pHealth = localPlayer.GetComponent<PlayerHealth>();
                    
                    if (pHealth != null)
                    {
                        float pct = pHealth.GetHealthPercentage() * 100f;
                        healthText.text = $"{pct:F0}%";
                        
                        // Lógica de color
                        healthText.color = pct > 60f ? Color.green : (pct > 30f ? Color.yellow : Color.red);
                    }
                }
            }
        }

        bool IsVisible(TMP_Text text)
        {
            return text != null && text.gameObject.activeSelf;
        }

        void UpdateNetworkAndFPS()
        {
            frameCount++;
            if (Time.time - lastFPSUpdate >= 0.5f)
            {
                fps = frameCount / (Time.time - lastFPSUpdate);
                frameCount = 0;
                lastFPSUpdate = Time.time;
            }

            if (GameController.Instance != null && Time.time - lastNetUpdateTime >= netUpdateInterval)
            {
                GameController.Instance.GetNetworkStats(out int pingMs, out int sent, out int recv, out float pps);
                
                if (pingMs >= 0)
                    smoothedPing = smoothedPing < 0 ? pingMs : Mathf.RoundToInt(Mathf.Lerp(smoothedPing, pingMs, 0.3f));
                
                string pingStr = smoothedPing < 0 ? "--" : $"{smoothedPing}";

                string fpsColor = fps >= 60 ? "green" : (fps >= 30 ? "yellow" : "red");
                
                cachedNetLine = $"FPS: <color={fpsColor}>{fps:F0}</color>\n" +
                                $"PING: {pingStr}ms\n" +
                                $"UP: {sent} | DW: {recv}";
                                
                lastNetUpdateTime = Time.time;
            }
        }

        void UpdateHealth(int playerId, float currentHealth, float maxHealth)
        {
            if (healthText == null || playerId != PlayerManager.Instance?.LocalPlayerId) return;

            float percentage = (currentHealth / maxHealth) * 100f;
            healthText.text = $"{percentage:F0}%";
            healthText.color = percentage > 60f ? Color.green : (percentage > 30f ? Color.yellow : Color.red);
        }

        public void UpdateAmmo(int current, int max)
        {
            if (ammoText != null) 
                ammoText.text = $"{current}/{max}";
        }
        #endregion

        #region Events & KillFeed (Sin cambios)
        void SetupEventListeners()
        {
            if (EventManager.Instance == null) return;
            EventManager.Instance.OnKillFeedMessage += AddKillFeedMessage;
            EventManager.Instance.OnPlayerHealthChanged += UpdateHealth;
        }

        void RemoveEventListeners()
        {
            if (EventManager.Instance == null) return;
            EventManager.Instance.OnKillFeedMessage -= AddKillFeedMessage;
            EventManager.Instance.OnPlayerHealthChanged -= UpdateHealth;
        }

        void AddKillFeedMessage(string message)
        {
            killFeedEntries.Add(new KillFeedEntry(message));
            while (killFeedEntries.Count > maxKillFeedLines) killFeedEntries.RemoveAt(0);
            UpdateKillFeedDisplay();
        }

        void UpdateKillFeed()
        {
            if (killFeedEntries.Count > 0)
            {
                killFeedEntries.RemoveAll(entry => entry.IsExpired(killFeedDuration));
                UpdateKillFeedDisplay();
            }
        }

        void UpdateKillFeedDisplay()
        {
            if (killFeedText == null) return;
            string content = "";
            foreach (var entry in killFeedEntries) content += entry.message + "\n";
            killFeedText.text = content;
        }
        
        void ToggleScoreboard(bool show)
        {
            if (scoreboardPanel != null) scoreboardPanel.SetActive(show);
            if (show) UpdateScoreboard();
        }

        void UpdateScoreboard()
        {
            if (scoreboardText == null || PlayerManager.Instance == null) return;
            string content = "<size=110%>SCOREBOARD</size>\n\n";
            
            GameObject localPlayer = PlayerManager.Instance.LocalPlayer;
            if (localPlayer != null)
            {
                float hp = localPlayer.GetComponent<PlayerHealth>()?.GetHealthPercentage() * 100f ?? 0;
                content += $"YOU (ID {PlayerManager.Instance.LocalPlayerId}) - {hp:F0}%\n";
            }

            foreach (var kvp in PlayerManager.Instance.RemotePlayers)
            {
                if (kvp.Value != null)
                {
                     float hp = kvp.Value.GetComponent<PlayerHealth>()?.GetHealthPercentage() * 100f ?? 0;
                     content += $"P{kvp.Key} - {hp:F0}%\n";
                }
            }
            scoreboardText.text = content;
        }

        public void ShowNotification(string message, float duration = 3f) { Debug.Log($"[Notif] {message}"); }
        #endregion
    }
}