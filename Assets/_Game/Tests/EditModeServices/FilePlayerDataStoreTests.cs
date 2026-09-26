using System;
using System.Collections.Generic;
using System.IO;
using ColorSort.Core.Progression;
using ColorSort.Services.Save;
using NUnit.Framework;

namespace ColorSort.Services.Tests
{
    public class FilePlayerDataStoreTests
    {
        private string _directory;
        private string _path;
        private List<string> _warnings;
        private FilePlayerDataStore _store;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "ColorSortTests", Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_directory, "player.json");
            _warnings = new List<string>();
            _store = new FilePlayerDataStore(_path, new CoinsOnlySerializer(), _warnings.Add);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }

        [Test]
        public void NoFile_LoadsFreshData()
        {
            PlayerData data = _store.Load();

            Assert.AreEqual(0, data.coins);
            Assert.IsEmpty(_warnings);
        }

        [Test]
        public void SaveThenLoad_RoundTrips_AndCreatesDirectory()
        {
            _store.Save(new PlayerData { coins = 42 });

            Assert.AreEqual(42, _store.Load().coins);
            Assert.IsFalse(File.Exists(_path + ".tmp"), "Temp file is moved into place");
        }

        [Test]
        public void SecondSave_KeepsPreviousAsBackup()
        {
            _store.Save(new PlayerData { coins = 1 });
            _store.Save(new PlayerData { coins = 2 });

            Assert.AreEqual("coins=1", File.ReadAllText(_path + ".bak"));
            Assert.AreEqual(2, _store.Load().coins);
        }

        [Test]
        public void CorruptMainFile_FallsBackToBackup_AndKeepsCorruptCopy()
        {
            _store.Save(new PlayerData { coins = 1 });
            _store.Save(new PlayerData { coins = 2 });
            File.WriteAllText(_path, "garbage");

            PlayerData data = _store.Load();

            Assert.AreEqual(1, data.coins);
            Assert.AreEqual("garbage", File.ReadAllText(_path + ".corrupt"));
            Assert.AreEqual(2, _warnings.Count, "One for the corrupt file, one for the restore");
        }

        [Test]
        public void MissingMainFile_FallsBackToBackup()
        {
            _store.Save(new PlayerData { coins = 1 });
            _store.Save(new PlayerData { coins = 2 });
            File.Delete(_path); // e.g. crash between removing the old file and moving the new one in

            Assert.AreEqual(1, _store.Load().coins);
        }

        [Test]
        public void EverythingUnreadable_StartsFresh()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_path, "garbage");
            File.WriteAllText(_path + ".bak", "garbage");

            Assert.AreEqual(0, _store.Load().coins);
        }

        [Test]
        public void Delete_RemovesAllFiles()
        {
            _store.Save(new PlayerData { coins = 1 });
            _store.Save(new PlayerData { coins = 2 });

            _store.Delete();

            Assert.IsFalse(File.Exists(_path));
            Assert.IsFalse(File.Exists(_path + ".bak"));
            Assert.AreEqual(0, _store.Load().coins);
        }

        [Test]
        public void InMemoryStore_CopiesData()
        {
            var store = new InMemoryPlayerDataStore(new CoinsOnlySerializer());
            var data = new PlayerData { coins = 5 };

            store.Save(data);
            data.coins = 99;

            Assert.AreEqual(5, store.Load().coins);
            Assert.AreEqual(1, store.SaveCount);
        }

        private sealed class CoinsOnlySerializer : IPlayerDataSerializer
        {
            public string Serialize(PlayerData data) => "coins=" + data.coins;

            public PlayerData Deserialize(string text)
            {
                if (text == null || !text.StartsWith("coins=") || !int.TryParse(text.Substring(6), out int coins))
                    throw new FormatException("Not a coins record.");
                return new PlayerData { coins = coins };
            }
        }
    }
}
