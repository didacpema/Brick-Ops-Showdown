using UnityEngine;
using System.Collections.Generic;

public class BarricadaManager : MonoBehaviour
{
    private const string BarricadaStatePrefix = "BARRICADA_STATE:";

    public static BarricadaManager Instance { get; private set; }

    private Dictionary<int, Barricada> barricadas = new Dictionary<int, Barricada>();
    private ServerSceneController serverInstance;

    private ServerSceneController Server => serverInstance ??= FindFirstObjectByType<ServerSceneController>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterBarricada(int id, Barricada barricada)
    {
        barricadas[id] = barricada;
    }

    public void UnregisterBarricada(int id)
    {
        barricadas.Remove(id);
    }

    public Barricada GetBarricada(int id)
    {
        barricadas.TryGetValue(id, out Barricada barricada);
        return barricada;
    }

    public bool IsServer()
    {
        return Server != null;
    }

    public void BroadcastBarricadaState(BarricadaState state)
    {
        if (state == null || !IsServer()) return;

        string payload = JsonUtility.ToJson(state);
        Server.BroadcastToClients(BarricadaStatePrefix + payload);
    }

    public void ApplyBarricadaStateFromNetwork(BarricadaState state)
    {
        if (state == null) return;
        Barricada barricada = GetBarricada(state.barricadaId);
        barricada?.ApplyState(state);
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
        if (string.IsNullOrEmpty(message) || !message.StartsWith(BarricadaStatePrefix)) return;

        string payload = message.Substring(BarricadaStatePrefix.Length);

        try
        {
            BarricadaState state = JsonUtility.FromJson<BarricadaState>(payload);
            ApplyBarricadaStateFromNetwork(state);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[BarricadaManager] Invalid barricada state payload: {ex.Message}");
        }
    }
}