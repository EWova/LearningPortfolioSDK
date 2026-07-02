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
                    string link = EWovaApp.GetDeepLink(LaunchViaDeepLinkOption.Default);
                    UnityEngine.Debug.Log($"已點擊開啟 ({link})。 ( Editor 印出測試訊息，不觸發 Application.OpenURL )");
                }
                else
                {
                    EWovaApp.LaunchViaDeepLink(LaunchViaDeepLinkOption.Default);
                }
            });
        }
    }
}