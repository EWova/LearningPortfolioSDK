using System;
using UnityEngine;

namespace EWova.LearningPortfolio.BasicAssets
{
    public static class ConnectBlocker
    {
        private static Action ReleaseEvent;

        [RuntimeInitializeOnLoadMethod]
        private static void SetupRuntimeSession()
        {
            LearningPortfolio.ConnectBlocker.Add(InternalBlockState);
            ReleaseEvent = () =>
            {
                LearningPortfolio.ConnectBlocker.Remove(InternalBlockState);
            };

#if UNITY_EDITOR
            Authoring.EditorDomainReleaseHelper.CleanupOneShot += () =>
            {
                ReleaseEvent?.Invoke();
                ReleaseEvent = null;
                IsLearningSessionActive = false;
            };
#endif
        }

        /// <summary>
        /// 判斷目前是否有學習歷程正在進行中，若是，則無法登入學習歷程。
        /// 當然，你也可以自定義 InternalBlockState 方法，來決定是否要阻擋登入學習歷程。
        /// </summary>
        public static bool IsLearningSessionActive { get; private set; } = false;

        private static (bool isBlocked, string bloackedMsg) InternalBlockState()
        {
            if (IsLearningSessionActive)
                return (true, "教材已經開始進行，無法登入學習歷程。\n請重新啟動遊戲應用程式");

            return (false, null);
        }
    }
}