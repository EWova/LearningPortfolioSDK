using System;

using UnityEngine;
using UnityEngine.Networking;

using UnityEditor;

using EWova.Authoring;

namespace EWova.LearningPortfolio.Editor
{
    [InitializeOnLoad]
    public class WelcomeWindow : EditorWindow
    {
        private const string SHOW_ON_STARTUP_KEY = "LP_EditorShowWelcomeWindow";
        private const string NEVER_SHOW_AGAIN_KEY = "LP_EditorNeverShowWelcomeWindow";
        private const string LATEST_PACKAGE_JSON_URL = "https://raw.githubusercontent.com/EWova/LearningPortfolioSDK/master/Assets/EWova.LearningPortfolioSDK/package.json";
        private static readonly TimeSpan VersionCheckInterval = TimeSpan.FromHours(1);
        private static bool showOnStartup;
        private static bool neverShowAgain;

        private enum VersionCheckState
        {
            Checking,
            UpToDate,
            UpdateAvailable,
            Error,
        }

        [Serializable]
        private class RemotePackageInfo
        {
            public string version;
        }

        private VersionCheckState versionCheckState;
        private Version latestVersionParsed;
        private UnityWebRequest versionCheckRequest;

        static WelcomeWindow()
        {
            EditorApplication.delayCall += InitOnLoad;
        }

        private static void InitOnLoad()
        {
            neverShowAgain = EWovaEditorPrefs.GetBool(NEVER_SHOW_AGAIN_KEY, false);
            showOnStartup = SessionState.GetBool(SHOW_ON_STARTUP_KEY, true);
            if (showOnStartup && !neverShowAgain)
            {
                ShowWindow();
                SessionState.SetBool(SHOW_ON_STARTUP_KEY, false);
            }
        }

        [MenuItem("EWova/Editor/Learning Portfolio/Welcome Window", false, 0)]
        public static void ShowWindow()
        {
            WelcomeWindow window = GetWindow<WelcomeWindow>(true, "EWova LearningPortfolio", true);
            window.minSize = new Vector2(380, 440);
            window.Show();
        }

        private void OnEnable()
        {
            StartVersionCheck();
        }

        private void OnDisable()
        {
            EditorApplication.update -= PollVersionCheckRequest;
            versionCheckRequest?.Abort();
            versionCheckRequest?.Dispose();
            versionCheckRequest = null;
        }

        private void StartVersionCheck()
        {
            string cachedLatestVersion = LearningPortfolioEditorPrefs.CachedLatestVersion;
            if (!string.IsNullOrEmpty(cachedLatestVersion) && DateTime.UtcNow - LearningPortfolioEditorPrefs.LastVersionCheckUtc < VersionCheckInterval)
            {
                try
                {
                    ApplyLatestVersion(cachedLatestVersion);
                    return;
                }
                catch
                {
                    // 快取的版本號格式異常，改為重新向 GitHub 檢查。
                }
            }

            versionCheckState = VersionCheckState.Checking;
            versionCheckRequest = UnityWebRequest.Get(LATEST_PACKAGE_JSON_URL);
            versionCheckRequest.SendWebRequest();
            EditorApplication.update += PollVersionCheckRequest;
        }

        private void PollVersionCheckRequest()
        {
            if (versionCheckRequest == null || !versionCheckRequest.isDone)
            {
                return;
            }

            EditorApplication.update -= PollVersionCheckRequest;

            if (versionCheckRequest.result != UnityWebRequest.Result.Success)
            {
                versionCheckState = VersionCheckState.Error;
            }
            else
            {
                try
                {
                    RemotePackageInfo remotePackageInfo = JsonUtility.FromJson<RemotePackageInfo>(versionCheckRequest.downloadHandler.text);
                    ApplyLatestVersion(remotePackageInfo?.version);
                    LearningPortfolioEditorPrefs.CachedLatestVersion = remotePackageInfo?.version ?? string.Empty;
                }
                catch
                {
                    versionCheckState = VersionCheckState.Error;
                }
            }

            LearningPortfolioEditorPrefs.LastVersionCheckUtc = DateTime.UtcNow;
            versionCheckRequest.Dispose();
            versionCheckRequest = null;
            Repaint();
        }

        private void ApplyLatestVersion(string version)
        {
            latestVersionParsed = !string.IsNullOrEmpty(version) ? new Version(version) : null;
            versionCheckState = latestVersionParsed != null && latestVersionParsed > new Version(PackageInfo.Version)
                ? VersionCheckState.UpdateAvailable
                : VersionCheckState.UpToDate;
        }

        private void OnGUI()
        {
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("EWova LearningPortfolio", new GUIStyle(EditorStyles.boldLabel) { fontSize = 24 });
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, richText = true };
            GUILayout.Label($"SDK Version v{PackageInfo.Version}", style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            DrawVersionCheckStatus();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            Divider();
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("透過 EWova 帳號，即可輕鬆將學習歷程資料同步至資料庫");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("高效管理學員的進度，並進入後台進行全方位的數據分析");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            Divider();
            GUILayout.Space(15);

            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("新功能新增或是錯誤修正，請持續追蹤說明文件");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("說明文件 Documentation", GUILayout.Height(30), GUILayout.Width(250)))
            {
                Application.OpenURL("https://wiki.ewova.com/zh-tw/LearningPortfolio");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("GitHub", GUILayout.Height(30), GUILayout.Width(250)))
            {
                Application.OpenURL("https://github.com/EWova/LearningPortfolioSDK");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("官方網站 Official Website", GUILayout.Height(30), GUILayout.Width(250)))
            {
                Application.OpenURL("https://ewova.com");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            if (GUILayout.Button("關閉 Close", GUILayout.Height(30), GUILayout.Width(220)))
            {
                Close();
            }
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool neverShow = GUILayout.Toggle(neverShowAgain, "不再顯示此頁面 Don't show again", GUILayout.Height(30), GUILayout.Width(220));
            if (neverShowAgain != neverShow)
            {
                neverShowAgain = neverShow;
                EWovaEditorPrefs.SetBool(NEVER_SHOW_AGAIN_KEY, neverShowAgain);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private const string DOT_COLOR_CHECKING = "#9E9E9E";
        private const string DOT_COLOR_UP_TO_DATE = "#4CAF50";
        private const string DOT_COLOR_PATCH_UPDATE = "#FFD54F";
        private const string DOT_COLOR_MINOR_OR_ABOVE_UPDATE = "#FF9800";
        private const string DOT_COLOR_ERROR = "#9E9E9E";

        private void DrawVersionCheckStatus()
        {
            string dotColor = versionCheckState switch
            {
                VersionCheckState.UpToDate => DOT_COLOR_UP_TO_DATE,
                VersionCheckState.UpdateAvailable => IsMinorOrAboveUpdate() ? DOT_COLOR_MINOR_OR_ABOVE_UPDATE : DOT_COLOR_PATCH_UPDATE,
                VersionCheckState.Error => DOT_COLOR_ERROR,
                _ => DOT_COLOR_CHECKING,
            };

            var temp = GUI.enabled;
            GUI.enabled = versionCheckState is VersionCheckState.UpdateAvailable or VersionCheckState.Error;
            GUILayout.Space(6);
            GUIStyle style = new(EditorStyles.miniButton) { richText = true, alignment = TextAnchor.MiddleCenter };

            string text = versionCheckState switch
            {
                VersionCheckState.Checking => $"<color={dotColor}>●</color> 正在檢查版本中...",
                VersionCheckState.UpToDate => $"<color={dotColor}>●</color> 已是最新或更新版本 v{latestVersionParsed}",
                VersionCheckState.UpdateAvailable => IsMinorOrAboveUpdate()
                    ? $"<color={dotColor}>●</color> 有新版本 建議更新 v{latestVersionParsed}"
                    : $"<color={dotColor}>●</color> 有新版本 需要更新 v{latestVersionParsed}",
                VersionCheckState.Error => $"<color={dotColor}>●</color> 無法取得版本資訊",
                _ => string.Empty,
            };

            if (GUILayout.Button(text, style))
            {
                UnityEditor.PackageManager.UI.Window.Open(PackageInfo.Name);
            }
            GUI.enabled = temp;
        }

        private bool IsMinorOrAboveUpdate()
        {
            Version currentVersion = new Version(PackageInfo.Version);
            return latestVersionParsed.Major > currentVersion.Major || latestVersionParsed.Minor > currentVersion.Minor;
        }

        private void Divider()
        {
            Rect rect = GUILayoutUtility.GetRect(10, 1, GUILayout.ExpandWidth(true));
            Color lineColor = EditorGUIUtility.isProSkin ? new Color(0.15f, 0.15f, 0.15f) : new Color(0.6f, 0.6f, 0.6f);
            EditorGUI.DrawRect(rect, lineColor);
        }
    }
}
