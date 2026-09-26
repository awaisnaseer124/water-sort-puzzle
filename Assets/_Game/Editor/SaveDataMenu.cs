using System.IO;
using ColorSort.App;
using ColorSort.Services.Save;
using UnityEditor;
using UnityEngine;

namespace ColorSort.EditorTools
{
    public static class SaveDataMenu
    {
        [MenuItem("ColorSort/Save Data/Reveal Save File", priority = 40)]
        private static void Reveal()
        {
            string path = GameBootstrap.SavePath;
            EditorUtility.RevealInFinder(File.Exists(path) ? path : Path.GetDirectoryName(path));
        }

        [MenuItem("ColorSort/Save Data/Delete Save (reset progress)", priority = 41)]
        private static void Delete()
        {
            if (!EditorUtility.DisplayDialog("Delete save",
                    $"Delete all progress, coins and purchases stored in\n{GameBootstrap.SavePath}?", "Delete", "Cancel"))
                return;

            new FilePlayerDataStore(GameBootstrap.SavePath, new JsonUtilityPlayerDataSerializer()).Delete();
            Debug.Log("Save deleted.");
        }

        [MenuItem("ColorSort/Save Data/Delete Save (reset progress)", validate = true)]
        private static bool CanDelete() => !EditorApplication.isPlaying; // the running game would write it back

        [MenuItem("ColorSort/Save Data/Grant 1000 Coins", priority = 42)]
        private static void GrantCoins() => GameBootstrap.Services.Profile.Wallet.Add(1000);

        [MenuItem("ColorSort/Save Data/Grant 1000 Coins", validate = true)]
        private static bool CanGrantCoins() => EditorApplication.isPlaying && GameBootstrap.Services != null;
    }
}
