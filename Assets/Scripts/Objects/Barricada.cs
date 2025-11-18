using UnityEngine;

[System.Serializable]
public class BarricadaState
{
    public int health;
    public bool[] pieceDestroyed;

    public BarricadaState(int health, bool[] pieceDestroyed)
    {
        this.health = health;
        this.pieceDestroyed = pieceDestroyed;
    }
}

public class Barricada : MonoBehaviour
{
    [Header("Barricada Settings")]
    [SerializeField] private int barricadaId = 0;
    [SerializeField] private int maxHealth = 300;
    [SerializeField] private GameObject[] pieces = new GameObject[3];
    
    private int health;
    private bool[] pieceDestroyed = new bool[3];
    private int piecesPerSegment;

    void Start()
    {
        InitializeBarricada();
        
        // Registrarse en el manager
        if (BarricadaManager.Instance != null)
        {
            BarricadaManager.Instance.RegisterBarricada(barricadaId, this);
        }
    }

    private void OnDestroy()
    {
        // Desregistrarse del manager
        if (BarricadaManager.Instance != null)
        {
            BarricadaManager.Instance.UnregisterBarricada(barricadaId);
        }
    }

    private void InitializeBarricada()
    {
        health = maxHealth;
        piecesPerSegment = maxHealth / pieces.Length;
        
        // Si pieces está vacío, intentar obtener los hijos
        if (pieces[0] == null || pieces[1] == null || pieces[2] == null)
        {
            Transform piecesParent = transform.Find("Pieces");
            if (piecesParent != null && piecesParent.childCount >= 3)
            {
                for (int i = 0; i < 3 && i < piecesParent.childCount; i++)
                {
                    pieces[i] = piecesParent.GetChild(i).gameObject;
                }
            }
        }
        
        // Verificar que las piezas están asignadas
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] == null)
            {
                Debug.LogWarning($"[Barricada {barricadaId}] Piece {i} is not assigned!");
            }
            else
            {
                pieces[i].SetActive(true);
                pieceDestroyed[i] = false;
            }
        }
        
        Debug.Log($"[Barricada {barricadaId}] Initialized with {maxHealth} health");
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        health = Mathf.Max(0, health - amount);
        Debug.Log($"[Barricada {barricadaId}] TakeDamage({amount}), Health: {health}/{maxHealth}");
        OnHealthChanged();
        
        // Si somos servidor, notificar a través del manager
        if (BarricadaManager.Instance != null && BarricadaManager.Instance.IsServer())
        {
            BarricadaState state = GetState();
            BarricadaManager.Instance.BroadcastBarricadaState(barricadaId, state);
            Debug.Log($"[Barricada {barricadaId}] Broadcasting state after damage");
        }
    }

    private void OnHealthChanged()
    {
        // Calcular qué piezas deben estar destruidas
        for (int i = 0; i < pieces.Length; i++)
        {
            int pieceThreshold = maxHealth - (piecesPerSegment * (i + 1));
            if (health <= pieceThreshold && !pieceDestroyed[i])
            {
                DestroyPiece(i);
            }
        }

        // Si la salud es 0, destruir toda la barricada
        if (health <= 0)
        {
            DestroySelf();
        }
    }

    private void DestroyPiece(int index)
    {
        if (index < 0 || index >= pieces.Length || pieceDestroyed[index]) return;
        
        pieceDestroyed[index] = true;
        
        if (pieces[index] != null)
        {
            pieces[index].SetActive(false);
            Debug.Log($"[Barricada {barricadaId}] Destroyed piece {index}");
        }
    }

    private void DestroySelf()
    {
        Debug.Log($"[Barricada {barricadaId}] Destroying barricada");
        Destroy(gameObject);
    }

    public BarricadaState GetState()
    {
        return new BarricadaState(health, (bool[])pieceDestroyed.Clone());
    }

    public void ApplyState(BarricadaState state)
    {
        if (state == null) return;
        
        // Actualizar salud
        health = state.health;
        
        // Actualizar piezas destruidas
        for (int i = 0; i < pieceDestroyed.Length && i < state.pieceDestroyed.Length; i++)
        {
            if (state.pieceDestroyed[i] && !pieceDestroyed[i])
            {
                pieceDestroyed[i] = true;
                DestroyPiece(i);
            }
        }
        
        // Verificar si debe autodestruirse
        if (health <= 0)
        {
            DestroySelf();
        }
        
        Debug.Log($"[Barricada {barricadaId}] Applied state: Health {health}/{maxHealth}");
    }

    public string StateToString(int id)
    {
        string piecesStr = string.Join(",", pieceDestroyed);
        return $"BARRICADA|{id}|{health}|{piecesStr}";
    }

    public static BarricadaState ParseStateFromString(string data)
    {
        // Formato: "BARRICADA|id|health|piece0,piece1,piece2"
        string[] parts = data.Split('|');
        if (parts.Length < 4) return null;

        int health = int.Parse(parts[2]);
        string[] piecesStr = parts[3].Split(',');
        bool[] pieces = new bool[piecesStr.Length];
        
        for (int i = 0; i < piecesStr.Length; i++)
        {
            pieces[i] = bool.Parse(piecesStr[i]);
        }

        return new BarricadaState(health, pieces);
    }

    public int GetBarricadaId()
    {
        return barricadaId;
    }

    public int GetHealth()
    {
        return health;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }
}