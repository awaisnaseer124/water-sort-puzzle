using System.Linq;
using ColorSort.App;
using ColorSort.Game.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorSort.EditorTools
{
    public static class ProjectSetup
    {
        private const string ResourcesFolder = "Assets/_Game/App/Resources";
        private const string ConfigPath = ResourcesFolder + "/" + GameConfig.ResourceName + ".asset";

        [InitializeOnLoadMethod]
        private static void EnsureGameConfigOnLoad() => EditorApplication.delayCall += EnsureGameConfig;

        [MenuItem("ColorSort/Setup/Ensure Game Config", priority = 200)]
        public static void EnsureGameConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath) != null)
                return;

            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                AssetDatabase.CreateFolder("Assets/_Game/App", "Resources");

            var config = ScriptableObject.CreateInstance<GameConfig>();
            string catalogGuid = AssetDatabase.FindAssets("t:" + nameof(LevelCatalog)).FirstOrDefault();
            if (catalogGuid != null)
                config.EditorSetLevelCatalog(AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(catalogGuid)));

            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {ConfigPath}.", config);
        }

        [MenuItem("ColorSort/Tools/Remove Missing Scripts In Open Scenes", priority = 300)]
        private static void RemoveMissingScripts()
        {
            int removed = 0;
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded)
                    continue;

                int before = removed;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform transform in root.GetComponentsInChildren<Transform>(includeInactive: true))
                    {
                        int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                        if (count == 0)
                            continue;
                        Undo.RegisterCompleteObjectUndo(transform.gameObject, "Remove missing scripts");
                        removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
                    }
                }
                if (removed > before)
                    EditorSceneManager.MarkSceneDirty(scene);
            }
            Debug.Log(removed == 0 ? "No missing scripts found." : $"Removed {removed} missing-script components. Save the scene to keep the change.");
        }
    }
}
