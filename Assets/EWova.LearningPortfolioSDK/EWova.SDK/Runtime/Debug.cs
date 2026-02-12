using System;

using UnityEngine;

namespace EWova
{
#if !UNITY_6000_0_OR_NEWER
    [System.Diagnostics.Conditional("UNITY_EDTIOR")]
    public sealed class HideInCallstackAttribute : Attribute {}
#endif
    [Serializable]
    public class Debug
    {
        [Flags]
        public enum Level
        {
            Info = 1,
            Warn = 2,
            Error = 4,

            None = 0,
            Full = Info | Warn | Error,
        }
        public Level PrintLevel;
        [NonSerialized] public string Prefix = "";

        public Debug(string prefix = "", Level printLevel = Level.Warn | Level.Error)
        {
            Prefix = prefix;
            PrintLevel = printLevel;
        }

        [HideInCallstack]
        public void Log(object msg)
        {
            if (PrintLevel.HasFlag(Level.Info))
                UnityEngine.Debug.Log(Prefix + msg);
        }
        [HideInCallstack]
        public void LogWarning(object msg)
        {
            if (PrintLevel.HasFlag(Level.Warn))
                UnityEngine.Debug.LogWarning(Prefix + msg);
        }
        [HideInCallstack]
        public void LogError(object msg)
        {
            if (PrintLevel.HasFlag(Level.Error))
                UnityEngine.Debug.LogError(Prefix + msg);
        }
        [HideInCallstack]
        public void LogException(Exception exception, object msg = null)
        {
            if (exception != null)
            {
                UnityEngine.Debug.LogError($"{Prefix} {msg} {exception.GetType().Name}:{exception.Message}");
                UnityEngine.Debug.LogException(exception);
            }
        }
    }
}