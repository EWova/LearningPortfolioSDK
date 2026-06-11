//using UnityEngine;
//using System.Collections;
//using System;

//namespace EWova.LearningPortfolio
//{
//    public partial class EWovaLoginPlane : MonoBehaviour
//    {
//        [Header("DeepLink")]
//        public bool EnabledDeepLinkLogin = true;

//        public event Action OnDeepLinkLoginStart;

//        private void InitDeepLink()
//        {
//            LearningPortfolio.DeepLinkBridge.OnLoginRequest += OnDeepLinkActivated;
//            if (LearningPortfolio.DeepLinkBridge.CurrentRequest != null)
//            {
//                OnDeepLinkActivated(LearningPortfolio.DeepLinkBridge.CurrentRequest.Value);
//            }
//        }
//        private void ReleaseDeepLink()
//        {
//            if (m_deepLinkDeeplinkCoro != null)
//            {
//                StopCoroutine(m_deepLinkDeeplinkCoro);
//                m_deepLinkDeeplinkCoro = null;
//            }
//            LearningPortfolio.DeepLinkBridge.OnLoginRequest -= OnDeepLinkActivated;
//        }

//        private Coroutine m_deepLinkDeeplinkCoro;
//        private void OnDeepLinkActivated(LearningPortfolio.DeepLinkBridge.Request req)
//        {
//            if (!gameObject.activeInHierarchy)
//                return;

//            if (LearningPortfolio.IsLoggedIn)
//            {
//                LearningPortfolio.Logger.Err("教材早已登入，請登出後重試。");
//                return;
//            }

//            if (!EnabledDeepLinkLogin)
//            {
//                LearningPortfolio.Logger.Warn("偵測到 DeepLink 快速登入，但功能未啟用。");
//                return;
//            }

//            LearningPortfolio.Logger.Warn("偵測到 DeepLink 快速登入。");
//            OnDeepLinkLoginStart?.InvokeSafely();
//            m_deepLinkDeeplinkCoro = StartCoroutine(DeepLinkLogin(req.Token));
//        }

//        private IEnumerator DeepLinkLogin(string token)
//        {
//            switch (CurrentStatus)
//            {
//                case Status.NetServiceConnecting:
//                    yield return new WaitUntil(() => CurrentStatus == EWovaLoginPlane.Status.NetServiceConnectedAndWaitForLogin);
//                    break;

//                case Status.NetServiceConnectedAndWaitForLogin:
//                    break;

//                case Status.LoginProcessing:
//                case Status.LoggedIn:
//                default:
//                    LearningPortfolio.Logger.Err("教材早已登入，請登出後重試。");
//                    yield break;
//            }

//            LearningPortfolio.Logger.Warn("開始進行登入");
//            LoginWithToken(token);
//        }
//    }
//}
