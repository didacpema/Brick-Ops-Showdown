using UnityEngine;

[System.Serializable]
public class BarricadaState
{
    public int barricadaId;
    public int health;
    public bool[] pieceDestroyed;

    public BarricadaState() {}

    public BarricadaState(int barricadaId, int health, bool[] pieceDestroyed)
    {
        this.barricadaId = barricadaId;
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
    private bool[] pieceDestroyed;
    private int piecesPerSegment;

    void Start()
    {
        InitializeBarricada();
        BarricadaManager.Instance?.RegisterBarricada(barricadaId, this);
    }

    private void OnDestroy()
    {
        BarricadaManager.Instance?.UnregisterBarricada(barricadaId);
    }

    private void InitializeBarricada()
    {
        health = maxHealth;
        EnsurePieceArray();

        int segmentCount = Mathf.Max(1, pieceDestroyed.Length);
        piecesPerSegment = Mathf.Max(1, maxHealth / segmentCount);

        AssignPiecesFromChildren();
        ResetPieces();
        EnsureCollider();
    }

    private void EnsurePieceArray(int minLength = 0)
    {
        int targetLength = Mathf.Max(pieces.Length, minLength);
        if (pieceDestroyed == null || pieceDestroyed.Length != targetLength)
        {
            pieceDestroyed = new bool[targetLength];
        }
    }

    private void ResetPieces()
    {
        EnsurePieceArray();

        for (int i = 0; i < pieces.Length; i++)
        {
            pieceDestroyed[i] = false;
            if (pieces[i] != null)
                pieces[i].SetActive(true);
        }
    }

    private void AssignPiecesFromChildren()
    {
        Transform piecesParent = transform.Find("Pieces");
        if (piecesParent == null)
            return;

        int assignCount = Mathf.Min(pieces.Length, piecesParent.childCount);
        for (int i = 0; i < assignCount; i++)
        {
            if (pieces[i] == null)
                pieces[i] = piecesParent.GetChild(i).gameObject;
        }
    }

    private void EnsureCollider()
    {
        if (GetComponent<Collider>() != null) return;
        if (GetComponentInChildren<Collider>() != null) return;
        gameObject.AddComponent<BoxCollider>();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        int previousHealth = health;
        health = Mathf.Max(0, health - amount);

        if (health == previousHealth) return;

        OnHealthChanged();

        if (BarricadaManager.Instance?.IsServer() == true)
        {
            BarricadaManager.Instance.BroadcastBarricadaState(GetState());
        }
    }

    private void OnHealthChanged()
    {
        for (int i = 0; i < pieceDestroyed.Length; i++)
        {
            int pieceThreshold = maxHealth - (piecesPerSegment * (i + 1));
            if (health <= pieceThreshold)
            {
                DestroyPiece(i);
            }
        }

        if (health <= 0)
        {
            DestroySelf();
        }
    }

    private void DestroyPiece(int index)
    {
        if (index < 0 || index >= pieceDestroyed.Length || pieceDestroyed[index]) return;

        pieceDestroyed[index] = true;
        if (index < pieces.Length && pieces[index] != null)
            pieces[index].SetActive(false);
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }

    public BarricadaState GetState()
    {
        bool[] snapshot = pieceDestroyed != null ? (bool[])pieceDestroyed.Clone() : new bool[0];
        return new BarricadaState(barricadaId, health, snapshot);
    }

    public void ApplyState(BarricadaState state)
    {
        if (state == null) return;

        health = Mathf.Clamp(state.health, 0, maxHealth);

        bool[] incoming = state.pieceDestroyed ?? new bool[0];

        EnsurePieceArray(incoming.Length);

        for (int i = 0; i < pieceDestroyed.Length; i++)
        {
            bool destroyed = i < incoming.Length && incoming[i];
            if (pieceDestroyed[i] == destroyed) continue;
            pieceDestroyed[i] = destroyed;
            if (i < pieces.Length && pieces[i] != null)
                pieces[i].SetActive(!destroyed);
        }

        if (health <= 0)
        {
            DestroySelf();
        }
    }
}
