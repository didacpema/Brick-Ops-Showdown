using UnityEngine;  
using BrickOps.Core;

[System.Serializable]
public class GameState
{
    public PlayerState player1;
    public PlayerState player2;
    public float gameTime;
    
    public GameState(PlayerState p1, PlayerState p2, float time)
    {
        player1 = p1;
        player2 = p2;
        gameTime = time;
    }
}
