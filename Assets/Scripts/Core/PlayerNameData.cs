using System;
using UnityEngine;

namespace BrickOps.Core
{
    [Serializable]
    public class PlayerNameData
    {
        public int playerId;
        public string playerName;

        public PlayerNameData(int id, string name)
        {
            playerId = id;
            playerName = name;
        }

        public static PlayerNameData FromJson(string json) => JsonUtility.FromJson<PlayerNameData>(json);
    }
}