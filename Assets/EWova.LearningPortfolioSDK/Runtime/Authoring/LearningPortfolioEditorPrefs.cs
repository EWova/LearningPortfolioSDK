#if UNITY_EDITOR
using System;
using System.Globalization;

using UnityEditor;

namespace EWova.Authoring
{
    /// <summary>
    /// 學習歷程編輯器相關的偏好設定。這些設定將影響在 Unity 編輯器中使用學習歷程相關功能的行為。 (Unity Editor Only)
    /// </summary>
    public static class LearningPortfolioEditorPrefs
    {
        public const bool DefaultDisableForceLogin = false;
        public static void ResetToDefault()
        {
            DisableForceLogin = DefaultDisableForceLogin;
        }

        /// <summary>
        /// 是否在瀏覽器驗證時跳過強制登入的步驟。true 時，如瀏覽器已驗證過，則將跳過驗證，但在某些情況下可能會要求使用者重新登入。false 時，瀏覽器跳轉驗證時將強制要求使用者登入。
        /// </summary>
        /// <remarks>這個設定只會在 Unity 編輯器中生效，打包後的遊戲仍然會使用預設行為</remarks>
        public static bool DisableForceLogin
        {
            get
            {
                return EWovaEditorPrefs.GetBool(DisableForceLoginPrefKey, DefaultDisableForceLogin);
            }
            set
            {
                EWovaEditorPrefs.SetBool(DisableForceLoginPrefKey, value);
            }
        }

        #region DisableForceLogin
        private const string ForceLoginDisableMenuPath = "EWova/Editor/Learning Portfolio/Force Login/Disable";
        private const string ForceLoginEnableMenuPath = "EWova/Editor/Learning Portfolio/Force Login/Enable";
        private const string DisableForceLoginPrefKey = "LP_EditorDisableForceLogin";
        [MenuItem(ForceLoginDisableMenuPath, false, 1)]
        private static void DisableForceLoginMenuItem()
        {
            DisableForceLogin = true;
            EditorLogger.Info("關閉強制登入，若瀏覽器驗證過，則在瀏覽器驗證時將跳過強制登入的步驟，但在某些情況下可能會要求使用者重新登入以確保安全性。");
        }
        [MenuItem(ForceLoginDisableMenuPath, true)]
        private static bool DisableForceLoginMenuItemValidate()
        {
            Menu.SetChecked(ForceLoginDisableMenuPath, DisableForceLogin);
            return true;
        }
        [MenuItem(ForceLoginEnableMenuPath, false, 2)]
        private static void EnableForceLoginMenuItem()
        {
            DisableForceLogin = false;
            EditorLogger.Info("恢復強制登入，瀏覽器跳轉驗證時將強制要求使用者登入。");
        }
        [MenuItem(ForceLoginEnableMenuPath, true)]
        private static bool EnableForceLoginMenuItemValidate()
        {
            Menu.SetChecked(ForceLoginEnableMenuPath, !DisableForceLogin);
            return true;
        }
        #endregion

        #region VersionCheckCache
        private const string LastVersionCheckTicksKey = "LP_EditorLastVersionCheckTicks";
        private const string CachedLatestVersionKey = "LP_EditorCachedLatestVersion";

        /// <summary>
        /// 上次向 GitHub 檢查最新版本的時間 (UTC)。
        /// </summary>
        public static DateTime LastVersionCheckUtc
        {
            get
            {
                string raw = EWovaEditorPrefs.GetString(LastVersionCheckTicksKey, "0");
                return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks) ? new DateTime(ticks, DateTimeKind.Utc) : DateTime.MinValue;
            }
            set
            {
                EWovaEditorPrefs.SetString(LastVersionCheckTicksKey, value.Ticks.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// 快取的最新版本號（從 GitHub 上取得）。
        /// </summary>
        public static string CachedLatestVersion
        {
            get => EWovaEditorPrefs.GetString(CachedLatestVersionKey, string.Empty);
            set => EWovaEditorPrefs.SetString(CachedLatestVersionKey, value);
        }
        #endregion
    }
}
#endif