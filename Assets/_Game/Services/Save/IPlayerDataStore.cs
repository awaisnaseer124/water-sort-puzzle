using ColorSort.Core.Progression;

namespace ColorSort.Services.Save
{
    public interface IPlayerDataStore
    {
        PlayerData Load();
        void Save(PlayerData data);
    }

    public interface IPlayerDataSerializer
    {
        string Serialize(PlayerData data);

        PlayerData Deserialize(string text);
    }

    public sealed class InMemoryPlayerDataStore : IPlayerDataStore
    {
        private readonly IPlayerDataSerializer _serializer;
        private string _saved;

        public InMemoryPlayerDataStore(IPlayerDataSerializer serializer)
        {
            _serializer = serializer;
        }

        public int SaveCount { get; private set; }

        public PlayerData Load() => _saved == null ? new PlayerData() : _serializer.Deserialize(_saved);

        public void Save(PlayerData data)
        {
            _saved = _serializer.Serialize(data);
            SaveCount++;
        }
    }
}
