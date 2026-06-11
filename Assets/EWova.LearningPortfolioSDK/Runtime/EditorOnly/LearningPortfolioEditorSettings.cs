#if UNITY_EDITOR
namespace EWova.LearningPortfolio.Editor
{
    /// <summary>
    /// 提供學習歷程在編輯器相關的設定選項。與打包後的結果無關，僅在 Unity 編輯器中生效。開發者可以根據需要調整這些設定來簡化開發流程或測試。
    /// </summary>
    public static class LearningPortfolioEditorSettings
    {
        /// <summary>
        /// 如果曾經驗證過，則在瀏覽器驗證時跳過強制登入的步驟，但在某些情況下可能會要求使用者重新登入以確保安全性。
        /// </summary>
        public static bool SkipForceLoginForBrowserAuthorization = false;
    }
}
#endif