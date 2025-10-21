using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public string playerName;
    public float posX;
    public float posY;
    public float posZ;
    public int health;
    public int score;
    
    public PlayerData(string name, Vector3 position, int hp, int score)
    {
        this.playerName = name;
        this.posX = position.x;
        this.posY = position.y;
        this.posZ = position.z;
        this.health = hp;
        this.score = score;
    }
}