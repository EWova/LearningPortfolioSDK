using EWova.Auth;

using System.Collections.Generic;

using UnityEngine;

namespace EWova.LearningPortfolio
{
    public class LearningPortfolioEWovaAuth : AuthProvider
    {
        internal LearningPortfolioEWovaAuth()
            : base(EWovaAuthConfigFactory.Create(options =>
        {
            options.ClientId = "learning-portfolio-sdk";
            options.Scopes = new List<string> { "openid", "profile", "email", "roles", "organization", "offline_access" };
        }), new Logger("[EWova]LPEWovaAuth ", LogLevel.Full))
        { }

        public bool LoadProjectSettings(out string errorMessage)
        {
            LearningPortfolioProfile loadedProfile = null;

            if (loadedProfile == null)
                loadedProfile = Resources.Load<LearningPortfolioProfile>("EWova/LearningPortfolioProfile");

            if (loadedProfile == null)
            {
                errorMessage = "無法從 Resources 中載入 ProjectSettings，請確認 LearningPortfolioProfile 是否存在於 Resources 資料夾中，並且已正確設定。";
                return false;
            }
            else if (!loadedProfile.ProjectSettings.IsValid(out string innerErrorMessage))
            {
                errorMessage = $"載入的 ProjectSettings 不符合規範: {innerErrorMessage}";
                return false;
            }
            else
            {
                errorMessage = null;
                CurrentProjectSettings = loadedProfile.ProjectSettings;
                return true;
            }
        }

        /// <summary>
        /// 儲存當前的 API 設定，供實例或擴充方法內部使用
        /// </summary>
        public ProjectSettings? CurrentProjectSettings { get; private set; }

        /// <summary>
        /// 檢查當前的 API 設定是否有效，若無效則無法進行 API 請求
        /// </summary>
        public bool IsProjectSettingsValid => CurrentProjectSettings?.IsValid(out _) ?? false;
    }
}
