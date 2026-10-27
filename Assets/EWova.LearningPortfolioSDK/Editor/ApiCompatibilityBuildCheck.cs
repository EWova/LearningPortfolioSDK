using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using UnityEngine;

using EWova.Authoring;

namespace EWova.LearningPortfolio.Editor
{
    /// <summary>
    /// 打包 Windows 時檢查 Api Compatibility Level，.NET Standard 打包出來的程式無法使用 DeepLink 跳轉登入。
    /// </summary>
    internal class ApiCompatibilityBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            BuildTarget platform = report.summary.platform;
            if (platform != BuildTarget.StandaloneWindows && platform != BuildTarget.StandaloneWindows64)
                return;

            NamedBuildTarget target = NamedBuildTarget.Standalone;
            if (LearningPortfolioApiCompatibilityGuard.IsNetFramework(target))
                return;

            const string message = "Standalone 的 Api Compatibility Level 為 .NET Standard，打包出來的程式無法註冊 DeepLink，網頁登入後將無法跳轉回遊戲。";

            if (Application.isBatchMode)
            {
                Debug.LogWarning($"[EWova 學習歷程] {message} 請到 Player Settings 切換為 .NET Framework。");
                return;
            }

            int choice = EditorUtility.DisplayDialogComplex(
                LearningPortfolioApiCompatibilityGuard.DialogTitle,
                message + "\n\n是否切換為 .NET Framework？\n套用後會取消此次打包，待重新編譯完成後請再打包一次。",
                "套用並取消打包",
                "取消打包",
                "仍要打包");

            switch (choice)
            {
                case 0:
                    LearningPortfolioApiCompatibilityGuard.Apply(target);
                    throw new BuildFailedException("[EWova 學習歷程] 已切換為 .NET Framework，請重新打包。");
                case 1:
                    throw new BuildFailedException("[EWova 學習歷程] 已取消打包：Api Compatibility Level 為 .NET Standard。");
                default:
                    Debug.LogWarning($"[EWova 學習歷程] {message}");
                    break;
            }
        }
    }
}
