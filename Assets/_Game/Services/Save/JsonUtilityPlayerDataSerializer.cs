using System;
using ColorSort.Core.Progression;
using UnityEngine;

namespace ColorSort.Services.Save
{
    public sealed class JsonUtilityPlayerDataSerializer : IPlayerDataSerializer
    {
        private readonly bool _prettyPrint;

        public JsonUtilityPlayerDataSerializer(bool prettyPrint = true)
        {
            _prettyPrint = prettyPrint;
        }

        public string Serialize(PlayerData data) => JsonUtility.ToJson(data, _prettyPrint);

        public PlayerData Deserialize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new FormatException("Save text is empty.");
            try
            {
                return JsonUtility.FromJson<PlayerData>(text) ?? throw new FormatException("Save text holds no data.");
            }
            catch (ArgumentException e)
            {
                // JsonUtility reports malformed JSON as ArgumentException.
                throw new FormatException(e.Message, e);
            }
        }
    }
}
