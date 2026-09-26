using System;
using System.IO;
using ColorSort.Core.Progression;

namespace ColorSort.Services.Save
{
    public sealed class FilePlayerDataStore : IPlayerDataStore
    {
        private readonly string _path;
        private readonly IPlayerDataSerializer _serializer;
        private readonly Action<string> _warn;

        public FilePlayerDataStore(string path, IPlayerDataSerializer serializer, Action<string> warn = null)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _warn = warn ?? (_ => { });
        }

        public string FilePath => _path;
        private string BackupPath => _path + ".bak";
        private string TempPath => _path + ".tmp";
        private string CorruptPath => _path + ".corrupt";

        public PlayerData Load()
        {
            if (TryRead(_path, out PlayerData data, keepIfCorrupt: true))
                return data;

            if (TryRead(BackupPath, out data, keepIfCorrupt: false))
            {
                _warn($"Save file missing or unreadable; restored from backup {BackupPath}.");
                return data;
            }

            return new PlayerData();
        }

        public void Save(PlayerData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            string directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(_serializer.Serialize(data));
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(_path))
            {
                File.Copy(_path, BackupPath, overwrite: true);
                File.Delete(_path);
            }
            File.Move(TempPath, _path);
        }

        public void Delete()
        {
            foreach (string file in new[] { _path, BackupPath, TempPath, CorruptPath })
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        }

        private bool TryRead(string file, out PlayerData data, bool keepIfCorrupt)
        {
            data = null;
            if (!File.Exists(file))
                return false;

            try
            {
                data = _serializer.Deserialize(File.ReadAllText(file));
                return data != null;
            }
            catch (Exception e) when (e is FormatException || e is IOException || e is UnauthorizedAccessException)
            {
                _warn($"Could not read save file {file}: {e.Message}");
                if (keepIfCorrupt && e is FormatException)
                    File.Copy(file, CorruptPath, overwrite: true);
                return false;
            }
        }
    }
}
