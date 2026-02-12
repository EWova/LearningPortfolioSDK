using System;
using System.Collections.Concurrent;

using UnityEngine;

namespace EWova
{
    public sealed class UnityMainThreadDispatcher : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitializeOnLoadBefore()
        {
            var go = new GameObject("[EWova] MainThread Dispatcher");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<UnityMainThreadDispatcher>();

#if UNITY_EDITOR
            static void PlayModeStateChanged(UnityEditor.PlayModeStateChange mode)
            {
                if (mode == UnityEditor.PlayModeStateChange.EnteredEditMode)
                {
                    UnityEditor.EditorApplication.playModeStateChanged -= PlayModeStateChanged;
                    _instance = null;
                }
            }
            ;
            UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChanged;
#endif
        }

        private readonly ConcurrentQueue<Action> _mainThreadActions = new();
        private static UnityMainThreadDispatcher _instance;

        public static void Enqueue(Action action)
        {
            _instance._mainThreadActions.Enqueue(action);
        }

        private void OnDestroy()
        {
            _instance = null;
        }

        private void Update()
        {
            while (_mainThreadActions.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }
    }
}
