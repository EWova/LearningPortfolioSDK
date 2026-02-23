#if (UNITY_STANDALONE_WIN || UNITY_EDITOR) && !ASSET_SUPPORT_DEEPLINKINGFORWINDOWS
using System;
using System.IO;
using UnityEngine;
using Microsoft.Win32;

namespace EWova.DeepLink
{
    public static class AutoUriSchemeBinder
    {
        const string REG_PATH_FMT = @"Software\Classes\{0}\shell\open\command";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoRegisterOnStartup()
        {
            var config = Config.LoadOrDefault();

            if(config == null)
            {
                UnityEngine.Debug.LogWarning("未找到 DeeplinkConfig 資源，無法自動註冊 URI Scheme。請確保已創建 EWova/Deeplink/Create Config");
                return;
            }

            string scheme = config.MyAppScheme;

            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string regExe = GetRegisteredExePath(scheme);

            if (string.IsNullOrEmpty(regExe) || !PathsEqual(regExe, exePath))
            {
                RegisterUriScheme(scheme, exePath);
                UnityEngine.Debug.Log($"URI Scheme '{scheme}://' 已更新為：{exePath}");
            }
            else
            {
                UnityEngine.Debug.Log($"URI Scheme '{scheme}://' 指向正確：{exePath}");
            }
        }

        static string GetRegisteredExePath(string scheme)
        {
            using (var cmdKey = Registry.CurrentUser.OpenSubKey(string.Format(REG_PATH_FMT, scheme)))
            {
                if (cmdKey == null) return null;
                var val = cmdKey.GetValue("") as string;
                if (string.IsNullOrEmpty(val)) return null;

                int i1 = val.IndexOf('"');
                int i2 = val.IndexOf('"', i1 + 1);
                if (i1 >= 0 && i2 > i1) return val.Substring(i1 + 1, i2 - i1 - 1);
                return val.Split(' ')[0];
            }
        }

        static void RegisterUriScheme(string scheme, string exePath)
        {
            using (var root = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{scheme}"))
            {
                root.SetValue("", $"URL:{scheme} Protocol");
                root.SetValue("URL Protocol", "");

                using (var cmd = root.CreateSubKey(@"shell\open\command"))
                {
                    cmd.SetValue("", $"\"{exePath}\" \"%1\"");
                }
            }
        }

        static bool PathsEqual(string a, string b)
        {
            try
            {
                return string.Equals(Path.GetFullPath(a).Trim('"'), Path.GetFullPath(b).Trim('"'),
                                     StringComparison.OrdinalIgnoreCase);
            }
            catch { return string.Equals(a, b, StringComparison.OrdinalIgnoreCase); }
        }
    }
}
#endif