using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace EWova.DeepLink.Editor
{
    public class CreateDeeplinkConfigEditor
    {
        private const string ResourceFolderPath = "Assets/Resources";
        private const string AssetPath = "Assets/Resources/DeeplinkConfig.asset";

        [MenuItem("EWova/Deeplink/Create Config")]
        public static void CreateConfig()
        {
            // Ensure Resources folder exists
            if (!Directory.Exists(ResourceFolderPath))
            {
                Directory.CreateDirectory(ResourceFolderPath);
                AssetDatabase.Refresh();
            }

            // Create asset
            Config config = ScriptableObject.CreateInstance<Config>();
            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ConfigUtility.EnsurePreloaded(config);

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;
        }

        // 控制 Menu 是否可點擊
        [MenuItem("EWova/Deeplink/Create Config", true)]
        public static bool ValidateCreateConfig()
        {
            return ConfigUtility.FindConfig() == null;
        }
    }

    [InitializeOnLoad]
    public static class ConfigPreloadValidator
    {
        static ConfigPreloadValidator()
        {
            EditorApplication.delayCall += Check;
        }

        private static void Check()
        {
            if (BuildPipeline.isBuildingPlayer)
                return;

            var config = ConfigUtility.FindConfig();
            if (config == null)
                return;

            ConfigUtility.EnsurePreloaded(config);
        }
    }

    internal static class ConfigUtility
    {
        public static void EnsurePreloaded(Config config)
        {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            var list = preloadedAssets.ToList();

            bool exists = list.Any(a => a == config);
            if (exists)
                return;

            // 移除舊的 Config
            list.RemoveAll(a => a is Config);

            list.Add(config);

            PlayerSettings.SetPreloadedAssets(list.ToArray());

            Debug.Log("Config 已加入 Preloaded Assets");
        }

        public static Config FindConfig()
        {
            return Resources.Load<Config>("DeeplinkConfig");
        }
    }
}