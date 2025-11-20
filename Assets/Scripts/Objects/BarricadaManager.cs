using UnityEngine;
using System.Collections.Generic;

public class BarricadaManager : MonoBehaviour
{
    public static BarricadaManager Instance { get; private set; }

    private Dictionary<int, Barricada> barricadas = new Dictionary<int, Barricada>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }    public void RegisterBarricada(int id, Barricada barricada)
    {
        if (!barricadas.ContainsKey(id))
        {
            barricadas.Add(id, barricada);
        }
    }

    public void UnregisterBarricada(int id)
    {
        if (barricadas.ContainsKey(id))
        {
            barricadas.Remove(id);
        }
    }

    public Barricada GetBarricada(int id)
    {
        barricadas.TryGetValue(id, out Barricada barricada);
        return barricada;
    }    public bool IsServer()
    {
        UDPServer_Select server = FindFirstObjectByType<UDPServer_Select>();
        return server != null;
    }    public void BroadcastBarricadaState(int barricadaId, BarricadaState state)
    {
        if (!IsServer()) return;

        Barricada barricada = GetBarricada(barricadaId);
        if (barricada != null)
        {
            string stateMsg = "BARRICADA_STATE:" + barricada.StateToString(barricadaId);
            
            UDPServer_Select server = FindFirstObjectByType<UDPServer_Select>();
            if (server != null)
            {
                var method = server.GetType().GetMethod("Broadcast", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(server, new object[] { stateMsg, null });
                }
            }
        }
    }    public void ApplyBarricadaStateFromNetwork(int barricadaId, BarricadaState state)
    {
        Barricada barricada = GetBarricada(barricadaId);
        if (barricada != null)
        {
            barricada.ApplyState(state);
        }
    }

    public void ApplyDamageToBarricada(int barricadaId, int damage)
    {
        Barricada barricada = GetBarricada(barricadaId);
        if (barricada != null)
        {
            barricada.TakeDamage(damage);
        }
    }

    // Método para procesar mensajes de estado desde el cliente UDP
    public void ProcessBarricadaStateMessage(string message)
    {
        try
        {
            // Formato esperado: "BARRICADA_STATE:BARRICADA|id|health|piece0,piece1,piece2"
            if (message.StartsWith("BARRICADA_STATE:"))
            {
                string data = message.Substring("BARRICADA_STATE:".Length);
                string[] parts = data.Split('|');
                
                if (parts.Length >= 4 && parts[0] == "BARRICADA")
                {
                    int barricadaId = int.Parse(parts[1]);
                    BarricadaState state = Barricada.ParseStateFromString(data);
                    
                    if (state != null)
                    {
                        ApplyBarricadaStateFromNetwork(barricadaId, state);
                        Debug.Log($"[BarricadaManager] Processed barricada state for ID {barricadaId}");
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[BarricadaManager] Error processing barricada state message: {ex.Message}");
        }
    }
}