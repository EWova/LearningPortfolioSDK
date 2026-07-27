using System;
using System.IO;

using UnityEditor;

using UnityEngine;

namespace EWova.LearningPortfolio.Editor
{
    [InitializeOnLoad]
    internal static class PkgVerGen
    {
        static PkgVerGen() { }

        internal const string PackageJsonGuid = "5c458c6c22855214ba2a274fd2d799b2";
        internal const string PackageInfoGuid = "04b7fe9b5e0bdd0478d7c6dfdd9fe83a";
        internal static void Generate()
        {
            var packagePath = AssetDatabase.GUIDToAssetPath(PackageJsonGuid);

            if (string.IsNullOrEmpty(packagePath))
            {
                Debug.LogError($"[PkgVerGen] Cannot resolve package.json GUID: {PackageJsonGuid}");
                return;
            }

            var infoPath = AssetDatabase.GUIDToAssetPath(PackageInfoGuid);

            if (string.IsNullOrEmpty(infoPath))
            {
                Debug.LogError($"[PkgVerGen] Cannot resolve PackageInfo.cs GUID: {PackageInfoGuid}");
                return;
            }

            if (!File.Exists(packagePath))
            {
                Debug.LogError($"[PkgVerGen] package.json not found: {packagePath}");
                return;
            }

            var json = File.ReadAllText(packagePath);
            var package = JsonUtility.FromJson<PackageJson>(json);

            var code = $@"// auto generated
namespace EWova.LearningPortfolio
{{
    internal static class PackageInfo
    {{
        public const string Name = ""{package.name}"";
        public const string Version = ""{package.version}"";
    }}
}}";

            if (!File.Exists(infoPath))
            {
                var dir = Path.GetDirectoryName(infoPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }

            var existing = File.Exists(infoPath)
                ? File.ReadAllText(infoPath)
                : null;

            if (existing == code)
                return;

            File.WriteAllText(infoPath, code);

            AssetDatabase.Refresh();
        }

        [System.Serializable]
        private class PackageJson
        {
            public string name;
            public string version;
        }
    }
}
