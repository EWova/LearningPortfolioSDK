//#define ASSET_SUPPORT_DEEPLINKINGFORWINDOWS

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
#if ASSET_SUPPORT_DEEPLINKINGFORWINDOWS
using Assets.DeepLinkingForWindows;
#endif
#endif

using EWova.NetService;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace EWova.DeepLink
{
    public class DeepLinkHandler
    {
        // 目前使用 Case-insensitive 來比較 Scheme，強烈建議將所有 Scheme 都使用小寫字母
        private const StringComparison SchemeStringComparison = StringComparison.OrdinalIgnoreCase;

        public static Debug Debug = new("DeepLink", Debug.Level.Full);
        public void Log(object msg) => Debug.Log($"[{Scheme}://] {msg}");
        public void LogWarning(object msg) => Debug.LogWarning($"[{Scheme}://] {msg}");
        public void LogError(object msg) => Debug.LogError($"[{Scheme}://] {msg}");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void AfterAssembliesLoaded()
        {
            s_schemeNamePool = new(StringComparer.FromComparison(SchemeStringComparison)); // editor 跳過 reload domain 不會自動釋放 static 變數
            Default = Registry(Config.MyAppScheme);
        }


#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

#elif UNITY_ANDROID || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            s_androidDeepLinkActivatedAfterSceneLoad?.Invoke(Application.absoluteURL);
        }
#endif


        /// <summary>
        /// 透過 Config.MyAppScheme 預先定義的 Scheme 建立的 DeepLink 處理器
        /// </summary>
        public static DeepLinkHandler Default { get; private set; }
        public IReadOnlyDictionary<string, string> Query => m_query;
        public string ActiveURL { get; private set; } = string.Empty;
        private bool IsActivated => !string.IsNullOrEmpty(ActiveURL);
        public string Scheme { get; private set; }
        public DateTime LastedUpdate { get; private set; }

        private readonly Dictionary<string, string> m_query = new();
        private static Dictionary<string, DeepLinkHandler> s_schemeNamePool;
        private Action<DeepLinkHandler> m_onActivated;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

#elif UNITY_ANDROID || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        private static Action<string> s_androidDeepLinkActivatedAfterSceneLoad;
#endif
        private DeepLinkHandler(string scheme)
        {
            this.Scheme = scheme;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
#if ASSET_SUPPORT_DEEPLINKINGFORWINDOWS
            WindowsDeepLinking.Initialize(scheme);
            WindowsDeepLinking.DeepLinkActivated += OnDeepLinkActivated;
#endif
#elif UNITY_ANDROID || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            UnityEngine.Application.deepLinkActivated += OnDeepLinkActivated;
            s_androidDeepLinkActivatedAfterSceneLoad += OnDeepLinkActivated;
#endif
        }
        ~DeepLinkHandler()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
#if ASSET_SUPPORT_DEEPLINKINGFORWINDOWS
            WindowsDeepLinking.DeepLinkActivated -= OnDeepLinkActivated;
#endif
#elif UNITY_ANDROID || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            UnityEngine.Application.deepLinkActivated -= OnDeepLinkActivated;
            s_androidDeepLinkActivatedAfterSceneLoad -= OnDeepLinkActivated;
#endif
            m_onActivated = null;
        }

        public static DeepLinkHandler Registry(string scheme)
        {
            if (string.IsNullOrEmpty(scheme))
                throw new ArgumentNullException(nameof(scheme), "Scheme cannot be null or empty.");

            if (!IsCurrentPlatformSupport)
            {
                //Debug.LogWarning("Current platform not support deep link.");
                return null;
            }

            Debug.Log($"Registry deep link scheme: {scheme}");
            if (!s_schemeNamePool.TryGetValue(scheme, out DeepLinkHandler handler))
            {
                handler = new DeepLinkHandler(scheme);
                s_schemeNamePool.Add(scheme, handler);
            }
            return handler;
        }

        public static bool IsCurrentPlatformSupport
        {
            get
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
#if ASSET_SUPPORT_DEEPLINKINGFORWINDOWS
                return true;
#else
                return false;
#endif
#elif UNITY_ANDROID || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                return true;
#else
                return false;
#endif
            }
        }
        public void ContinueWith(Action<DeepLinkHandler> action)
        {
            if (action == null)
                return;

            if (IsActivated)
                action.Invoke(this);

            m_onActivated += action;
        }

        public void Remove(Action<DeepLinkHandler> action)
        {
            if (action == null)
                return;

            m_onActivated -= action;
        }

        private void OnDeepLinkActivated(string uriText)
        {
            Log($"OnDeepLinkActivated: {uriText}");

            if (string.IsNullOrEmpty(uriText))
            {
                return;
            }

            UriBuilder uri;
            try
            {
                uri = new UriBuilder(uriText);
            }
            catch (UriFormatException)
            {
                return;
            }

            if (!string.Equals(uri.Scheme, Scheme, SchemeStringComparison))
                return;

            ActiveURL = uriText;
            LastedUpdate = DateTime.Now;

            var query = HttpUtility.ParseQueryString(uri.Query);
            m_query.Clear();
            string[] keys = query.AllKeys;
            for (int i = 0; i < keys.Length; i++)
            {
                var k = keys[i];
                var v = query[k];
                m_query.Add(k ?? string.Empty, v);
            }

            m_onActivated?.Invoke(this);
        }
    }
}