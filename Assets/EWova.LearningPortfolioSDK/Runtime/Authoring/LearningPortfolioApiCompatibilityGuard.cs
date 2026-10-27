#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;

using UnityEngine;

namespace EWova.Authoring
{
    /// <summary>
    /// Windows DeepLink 需要 .NET Framework，.NET Standard 下無法註冊 URL Scheme 到註冊表，網頁登入後跳轉回 Unity 會沒有反應。
    /// <list type="bullet">
    /// <item>Windows 編輯器：由 Editor Assemblies Compatibility Level 決定（com.ewova.core 1.10.1 起，與目前平台的 Api Compatibility Level 無關）</item>
    /// <item>打包 Windows：由 Standalone 的 Api Compatibility Level 決定</item>
    /// </list>
    /// 此類別負責檢查並提示使用者切換。 (Unity Editor Only)
    /// </summary>
    public static class LearningPortfolioApiCompatibilityGuard
    {
        public const ApiCompatibilityLevel RequiredLevel = ApiCompatibilityLevel.NET_Unity_4_8;
        internal const string DialogTitle = "EWova 學習歷程";

        public static bool IsNetFramework(NamedBuildTarget target)
            => PlayerSettings.GetApiCompatibilityLevel(target) == RequiredLevel;

        /// <summary>
        /// Default 等同 .NET Framework，只有 .NET Standard 不支援
        /// </summary>
        public static bool IsEditorAssembliesNetFramework
            => PlayerSettings.GetEditorAssembliesCompatibilityLevel() != EditorAssembliesCompatibilityLevel.NET_Standard;

        public static void Apply(NamedBuildTarget target)
        {
            PlayerSettings.SetApiCompatibilityLevel(target, RequiredLevel);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EWova 學習歷程] 已將 {target.TargetName} 的 Api Compatibility Level 切換為 .NET Framework");
        }

        public static void ApplyEditorAssemblies()
        {
            PlayerSettings.SetEditorAssembliesCompatibilityLevel(EditorAssembliesCompatibilityLevel.NET_Unity_4_8);
            AssetDatabase.SaveAssets();
            Debug.Log("[EWova 學習歷程] 已將 Editor Assemblies Compatibility Level 切換為 .NET Framework");
        }

        /// <summary>
        /// Windows 編輯器按下登入時呼叫，Editor Assemblies Compatibility Level 為 .NET Standard 就提示。
        /// </summary>
        /// <returns>是否有顯示提示</returns>
        internal static bool PromptOnLogin()
        {
            if (Application.isBatchMode || IsEditorAssembliesNetFramework)
                return false;

            int choice = EditorUtility.DisplayDialogComplex(
                DialogTitle,
                "Editor Assemblies Compatibility Level 為 .NET Standard，Windows 編輯器無法註冊 DeepLink，網頁登入後跳轉回 Unity 將沒有反應。\n\n" +
                "是否切換為 .NET Framework？\n套用後會停止 Play 並重新編譯，完成後再次進入 Play 即可登入。",
                "套用並停止 Play",
                "略過",
                "開啟 Player Settings");

            switch (choice)
            {
                case 0:
                    ApplyEditorAssemblies();
                    EditorApplication.isPlaying = false;
                    break;
                case 2:
                    SettingsService.OpenProjectSettings("Project/Player");
                    break;
            }
            return true;
        }
    }
}
#endif
