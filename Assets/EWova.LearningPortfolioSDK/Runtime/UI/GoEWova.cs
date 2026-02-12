using UnityEngine;
using UnityEngine.UI;

namespace EWova.LearningPortfolio {
    public class GoEWova : MonoBehaviour
    {
        public Button Button;
        private void Awake()
        {
            Button.onClick?.AddListener(() =>
            {
                if (Application.isEditor)
                {
                    UnityEngine.Debug.Log($"已點擊開啟 EWova ({EWova.GetDeepLink()})。 Editor 中不會真的打開，僅測試用。");
                }
                else
                {
                    EWova.LaunchApp();
                }
            });
        }
    }
}