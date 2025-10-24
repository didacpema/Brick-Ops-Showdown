using UnityEngine;

[System.Serializable]
public class GameState
{
    public PlayerData player1;
    public PlayerData player2;
    public float gameTime;
    
    public GameState(PlayerData p1, PlayerData p2, float time)
    {
        player1 = p1;
        player2 = p2;
        gameTime = time;
    }
}
