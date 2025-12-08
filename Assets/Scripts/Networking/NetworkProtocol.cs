using System;
using System.Text;
using UnityEngine;

namespace BrickOps.Networking
{
    /// <summary>
    /// Maneja el protocolo de comunicación de red
    /// </summary>
    public static class NetworkProtocol
    {
        #region Message Types
        public const string PLAYER_DATA = "PLAYER_DATA";
        public const string SHOOT_DATA = "SHOOT_DATA";
        public const string DEATH_DATA = "DEATH_DATA";
        public const string PLAYER_RESPAWN = "PLAYER_RESPAWN";
        public const string PLAYER_ID = "PLAYER_ID";
        public const string READY_TO_START = "READY_TO_START";
        public const string GAME_START = "GAME_START";
        public const string START_GAME = "START_GAME";
        public const string SERVER_CLOSED = "SERVER_CLOSED";
        public const string BARRICADA_HIT = "BARRICADA_HIT";
        public const string PLAYER_NAME = "PLAYER_NAME";
        public const string HEALTH_PACK_PICKUP = "HEALTH_PACK_PICKUP";
        public const string OBJECT_TRANSFORM = "OBJECT_TRANSFORM";
        #endregion

        #region Message Building
        public static string BuildMessage(string messageType, string data = "")
        {
            return string.IsNullOrEmpty(data) ? messageType : $"{messageType}:{data}";
        }

        public static string BuildMessage<T>(string messageType, T data)
        {
            string json = SerializeToJson(data);
            return $"{messageType}:{json}";
        }

         public static byte[] MessageToBytes(string message)
        {
            return Encoding.UTF8.GetBytes(message);
        }

        public static string BytesToMessage(byte[] buffer, int length)
        {
            return Encoding.UTF8.GetString(buffer, 0, length);
        }
        #endregion

        #region Message Parsing
        public static bool TryParseMessage(string message, out string messageType, out string data)
        {
            messageType = string.Empty;
            data = string.Empty;

            if (string.IsNullOrEmpty(message))
                return false;

            int separatorIndex = message.IndexOf(':');
            
            if (separatorIndex == -1)
            {
                messageType = message;
                return true;
            }

            messageType = message.Substring(0, separatorIndex);
            data = message.Substring(separatorIndex + 1);
            return true;
        }

        public static bool TryParseMessage<T>(string message, out string messageType, out T data)
        {
            data = default;
            messageType = string.Empty;

            if (!TryParseMessage(message, out messageType, out string jsonData))
                return false;

            if (string.IsNullOrEmpty(jsonData))
                return false;

            try
            {
                data = DeserializeFromJson<T>(jsonData);
                return data != null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkProtocol] Failed to deserialize {typeof(T).Name}: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region JSON Serialization
        public static string SerializeToJson<T>(T obj)
        {
            try
            {
                return JsonUtility.ToJson(obj);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkProtocol] Serialization failed: {ex.Message}");
                return string.Empty;
            }
        }

        public static T DeserializeFromJson<T>(string json)
        {
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkProtocol] Deserialization failed: {ex.Message}");
                return default;
            }
        }
        #endregion

        #region Validation
        public static bool IsValidMessage(string message)
        {
            return !string.IsNullOrEmpty(message) && message.Length < 2048;
        }

        public static bool IsValidPlayerId(int playerId)
        {
            return playerId > 0 && playerId < 100;
        }
        #endregion
    }

    [Serializable]
    public class NetworkMessage
    {
        public string type;
        public string data;
        public long timestamp;
        public int senderId;

        public NetworkMessage(string messageType, string messageData, int sender = -1)
        {
            type = messageType;
            data = messageData;
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            senderId = sender;
        }
    }
}