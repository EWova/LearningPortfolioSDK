using UnityEngine;
using UnityEditor;
using EWova.Authoring;

namespace EWova.LearningPortfolio.Editor
{
    [InitializeOnLoad]
    public class WelcomeWindow : EditorWindow
    {
        private const string SHOW_ON_STARTUP_KEY = "LP_EditorShowWelcomeWindow";
        private const string NEVER_SHOW_AGAIN_KEY = "LP_EditorNeverShowWelcomeWindow";
        private static bool showOnStartup;
        private static bool neverShowAgain;
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
            WelcomeWindow window = GetWindow<WelcomeWindow>(true, "Welcome", true);
            window.minSize = new Vector2(380, 440);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("EWova LearningPortfolio SDK", new GUIStyle(EditorStyles.boldLabel) { fontSize = 24 });
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label($"version {PackageInfo.Version}", EditorStyles.miniLabel);
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
            bool neverShow = GUILayout.Toggle(!neverShowAgain, "不再顯示此頁面 Don't show again", GUILayout.Height(30), GUILayout.Width(220));
            if (neverShowAgain != !neverShow)
            {
                neverShowAgain = !neverShow;
                EWovaEditorPrefs.SetBool(NEVER_SHOW_AGAIN_KEY, neverShowAgain);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void Divider()
        {
            Rect rect = GUILayoutUtility.GetRect(10, 1, GUILayout.ExpandWidth(true));
            Color lineColor = EditorGUIUtility.isProSkin ? new Color(0.15f, 0.15f, 0.15f) : new Color(0.6f, 0.6f, 0.6f);
            EditorGUI.DrawRect(rect, lineColor);
        }
    }
}
