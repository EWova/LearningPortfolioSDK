using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

namespace EWova.LearningPortfolio
{
    public class GoEWova : MonoBehaviour
    {
        public Button Button;
        private void Awake()
        {
            Button.onClick?.AddListener(() =>
            {
                if (Application.isEditor)
                {
                    LogPreviewDeepLinkAsync().Forget();
                }
                else
                {
                    EWovaApp.LaunchViaDeepLink(EWovaDeepLinkLaunchOption.Default, LearningPortfolio.EWovaAuth);
                }
            });
        }

        private async UniTaskVoid LogPreviewDeepLinkAsync()
        {
            string link = await EWovaApp.GetDeepLink(EWovaDeepLinkLaunchOption.Default, LearningPortfolio.EWovaAuth);
            UnityEngine.Debug.Log($"已點擊開啟 ({link})。 ( Editor 印出測試訊息，不觸發 Application.OpenURL )");
        }
    }
}