#if UNITY_EDITOR
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
        private const string DisableForceLoginMenuPath = "EWova/Editor/Learning Portfolio/Disable Force Login";
        private const string DisableForceLoginPrefKey = "LP_EditorDisableForceLogin";
        [MenuItem(DisableForceLoginMenuPath, false, 1)]
        private static void ToggleForceLogin()
        {
            DisableForceLogin = !DisableForceLogin;

            if (DisableForceLogin)
            {
                EditorLogger.Info("關閉強制登入，若瀏覽器驗證過，則在瀏覽器驗證時將跳過強制登入的步驟，但在某些情況下可能會要求使用者重新登入以確保安全性。");
            }
            else
            {
                EditorLogger.Info("恢復強制登入，瀏覽器跳轉驗證時將強制要求使用者登入。");
            }
        }
        [MenuItem(DisableForceLoginMenuPath, true)]
        private static bool ToggleForceLoginValidate()
        {
            Menu.SetChecked(DisableForceLoginMenuPath, DisableForceLogin);
            return true;
        }
        #endregion
    }
}
#endif