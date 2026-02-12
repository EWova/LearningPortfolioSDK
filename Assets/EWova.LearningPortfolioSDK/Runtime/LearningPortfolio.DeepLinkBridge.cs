#define DEEPLINK //EWova.DeepLink

using System;

using UnityEngine;
using EWova.NetService;


#if DEEPLINK
using EWova.DeepLink;
#endif

namespace EWova.LearningPortfolio
{
    public partial class LearningPortfolio
    {
        public static class DeepLinkBridge
        {
            public struct Request
            {
                public string Token;
                public string FromWorld;
                public string FromSpace;
            }

            public static event Action<Request> OnLoginRequest;
            public static Request? CurrentRequest;

            private static bool s_isInitialized = false;

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
            private static void ReloadDomain()
            {
                if (!s_isInitialized)
                    return;
                s_isInitialized = false;

                OnLoginRequest = null;
                CurrentRequest = null;
#if DEEPLINK
                if (DeepLinkHandler.IsCurrentPlatformSupport)
                    DeepLinkHandler.Default.Remove(OnDeepLinkActivated);
#endif
            }

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
            private static void Initialize()
            {
                s_isInitialized = true;
#if DEEPLINK
                CurrentRequest = null;
                if (DeepLinkHandler.IsCurrentPlatformSupport)
                    DeepLinkHandler.Default.ContinueWith(OnDeepLinkActivated);
#endif
            }

#if DEEPLINK
            private static void OnDeepLinkActivated(DeepLinkHandler handler)
            {
                Request result = default;

                bool fromEWovaApp = false;
                // 從哪一個 EWova 課程世界過來的
                if (handler.Query.TryGetValue(EWovaUriPath.Query.WorldID.Key, out string worldId) && !string.IsNullOrEmpty(worldId))
                {
                    result.FromWorld = worldId;
                    fromEWovaApp = true;
                }
                // 承上 從哪一個 EWova 課程空間下的空間過來的
                if (handler.Query.TryGetValue(EWovaUriPath.Query.SpaceID.Key, out string spaceId) && !string.IsNullOrEmpty(spaceId))
                {
                    result.FromSpace = spaceId;
                    fromEWovaApp = true;
                }

                if (fromEWovaApp)
                {
                    LearningPortfolio.Debug.LogWarning($"來自 EWova 的快速登入。 World: {result.FromWorld}, SpaceID: {result.FromSpace}");
                }

                bool loginRequest = false;
                if (handler.Query.TryGetValue(EWovaUriPath.Query.Token.Key, out string token))
                {
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        loginRequest = true;
                        result.Token = token;
                    }
                }

                CurrentRequest = result;

                if (loginRequest)
                {
                    try
                    {
                        OnLoginRequest?.Invoke(CurrentRequest.Value);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogException(ex);
                    }
                }
            }
#endif
        }
    }
}