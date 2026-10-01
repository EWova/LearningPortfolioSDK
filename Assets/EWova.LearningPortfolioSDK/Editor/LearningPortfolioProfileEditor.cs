using Cysharp.Threading.Tasks;

using Newtonsoft.Json;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using UnityEditor;

using UnityEngine;

namespace EWova.LearningPortfolio.Editor
{
    [CustomEditor(typeof(LearningPortfolioProfile))]
    public class LearningPortfolioProfileEditor : UnityEditor.Editor
    {
        private const string DefaultSchemeOutputPath = "Assets/Scripts/ProjectScheme.cs";

        private SerializedProperty m_stringApiKey;

        private int m_isApiKeyValid = -1;
        private string m_message = null;

        // 驗證成功時記下來；API Key 一改就失效，必須重新驗證才能產生 Scheme
        private string m_verifiedApiKey = null;
        private Guid m_verifiedProjectId;

        private string m_schemeOutputPath;
        private bool m_isGeneratingScheme;
        private string m_schemeMessage = null;
        private MessageType m_schemeMessageType = MessageType.None;

        // EditorPrefs 是整台機器共用，key 帶上專案路徑避免不同專案互相蓋掉
        private static string SchemeOutputPathPrefKey =>
            "EWova.LearningPortfolio.SchemeOutputPath." + Application.dataPath;

        private void OnEnable()
        {
            m_stringApiKey = serializedObject.FindProperty("ProjectSettings").FindPropertyRelative("APIKey");
            m_schemeOutputPath = EditorPrefs.GetString(SchemeOutputPathPrefKey, null);
            if (string.IsNullOrEmpty(m_schemeOutputPath))
                m_schemeOutputPath = FindExistingSchemeScript() ?? DefaultSchemeOutputPath;
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("API Key", GUILayout.MaxWidth(100));
            var newApiKey = EditorGUILayout.TextField(m_stringApiKey.stringValue, GUILayout.ExpandWidth(true)).Trim();
            if (newApiKey != m_stringApiKey.stringValue)
            {
                m_stringApiKey.stringValue = newApiKey;
                // 換了 key，之前的驗證結果不再代表這把 key
                m_isApiKeyValid = -1;
                m_message = null;
                m_verifiedApiKey = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (m_isApiKeyValid != -1)
            {
#if UNITY_6000_0_OR_NEWER
                EditorGUILayout.LabelField(m_isApiKeyValid switch { -1 => "", 0 => "⏳", 1 => "✅", _ => "❌" }, GUILayout.MaxWidth(16));
#else
                EditorGUILayout.LabelField(m_isApiKeyValid switch { -1 => "", 0 => "...", 1 => "Ｏ", _ => "Ｘ" }, GUILayout.MaxWidth(16));
#endif
            }

            bool enabledCache;

            enabledCache = GUI.enabled;
            GUI.enabled = m_isApiKeyValid != 0;
            if (GUILayout.Button("驗證"))
            {
                m_isApiKeyValid = 0;
                m_message = null;
                m_verifiedApiKey = null;

                if (!LearningPortfolio.TryLoadProjectSettings(out var proSet, out string errorMessage))
                {
                    m_isApiKeyValid = 2;
                    m_message = $"載入 ProjectSettings 失敗: {errorMessage}";
                    return;
                }
                var apiClient = new LPApiClient(proSet.Value, null);

                UniTask.Void(async () =>
                {
                    try
                    {
                        var valid = await apiClient.GetApiKeyValidInfoAsync();
                        if (!valid.IsValid)
                        {
                            m_isApiKeyValid = 2;
                            m_message = $"Token 不可用 錯誤資訊:{valid.ErrorMessage}";
                            return;
                        }
                        m_isApiKeyValid = 0;
                        m_message = $"專案認證成功！\n\n取得詳細資料...";
                        var project = await apiClient.GetProjectAsync(valid.ProjectId);
                        m_isApiKeyValid = 1;
                        m_verifiedApiKey = proSet.Value.APIKey;
                        m_verifiedProjectId = valid.ProjectId;
                        m_message = $"專案認證成功！\n\n" +
                                    JsonConvert.SerializeObject(project, Formatting.Indented);
                    }
                    catch (System.Exception ex)
                    {
                        m_isApiKeyValid = 2;
                        m_message = $"[{ex.GetType().Name}]\n" +
                        $"{ex.Message}";
                        Debug.LogException(ex);
                    }
                    finally
                    {
                        Repaint();
                    }
                });
            }
            GUI.enabled = enabledCache;
            EditorGUILayout.EndHorizontal();


            enabledCache = GUI.enabled;
            if (!string.IsNullOrEmpty(m_message))
            {
                EditorGUILayout.TextArea(m_message);
            }

            DrawSchemeSection();

            serializedObject.ApplyModifiedProperties();
        }

        #region C# Scheme 產生

        private bool IsVerified =>
            m_isApiKeyValid == 1 && m_verifiedApiKey != null && m_verifiedApiKey == m_stringApiKey.stringValue;

        private void DrawSchemeSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("C# Scheme", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("輸出檔案", GUILayout.MaxWidth(100));
            EditorGUI.BeginChangeCheck();
            m_schemeOutputPath = EditorGUILayout.TextField(m_schemeOutputPath, GUILayout.ExpandWidth(true)).Trim();
            if (GUILayout.Button("…", GUILayout.Width(28)))
            {
                var dir = Path.GetDirectoryName(m_schemeOutputPath);
                var picked = EditorUtility.SaveFilePanelInProject(
                    "C# Scheme 輸出位置",
                    Path.GetFileNameWithoutExtension(m_schemeOutputPath),
                    "cs",
                    "選擇 ProjectScheme.cs 的輸出位置",
                    string.IsNullOrEmpty(dir) ? "Assets" : dir);
                if (!string.IsNullOrEmpty(picked))
                {
                    m_schemeOutputPath = picked;
                    GUI.FocusControl(null);
                }
            }
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetString(SchemeOutputPathPrefKey, m_schemeOutputPath);
            EditorGUILayout.EndHorizontal();

            bool enabledCache = GUI.enabled;
            GUI.enabled = IsVerified && !m_isGeneratingScheme;
            if (GUILayout.Button(m_isGeneratingScheme ? "產生中…" : "產生 C# Scheme"))
                GenerateScheme();
            GUI.enabled = enabledCache;

            if (!IsVerified)
                EditorGUILayout.HelpBox("請先按「驗證」，API Key 驗證成功後才能產生。", MessageType.None);

            if (!string.IsNullOrEmpty(m_schemeMessage))
                EditorGUILayout.HelpBox(m_schemeMessage, m_schemeMessageType);
        }

        private void GenerateScheme()
        {
            var outputPath = m_schemeOutputPath.Replace('\\', '/');
            if (!outputPath.StartsWith("Assets/") || !outputPath.EndsWith(".cs"))
            {
                SetSchemeMessage("輸出檔案必須在 Assets/ 底下，且副檔名為 .cs", MessageType.Error);
                return;
            }

            // 既有檔案不是產生器產的（例如手寫版 ProjectScheme.cs），覆蓋前先問
            string previousHash = null;
            if (File.Exists(outputPath))
            {
                var existing = File.ReadAllText(outputPath);
                if (!existing.Contains("<auto-generated>") &&
                    !EditorUtility.DisplayDialog(
                        "覆蓋既有檔案？",
                        $"{outputPath} 不是由產生器建立的，產生後會被整個覆蓋。\n\n手寫的擴充請改寫在另一個 partial class 檔案。",
                        "覆蓋", "取消"))
                    return;

                var m = Regex.Match(existing, "SchemeHash\\s*=\\s*\"([^\"]*)\"");
                if (m.Success) previousHash = m.Groups[1].Value;
            }

            m_isGeneratingScheme = true;
            SetSchemeMessage("從後台取得 Scheme…", MessageType.None);

            var apiClient = new LPApiClient(new ProjectSettings(m_verifiedApiKey), null);
            var projectId = m_verifiedProjectId;

            UniTask.Void(async () =>
            {
                try
                {
                    var json = await apiClient.Get($"/api/projects/{projectId}/scheme");
                    var scheme = JsonConvert.DeserializeObject<ProjectSchemeJson>(json);
                    var code = ProjectSchemeCodeGen.Generate(scheme, new ProjectSchemeCodeGen.Options());

                    var dir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(outputPath, code, new UTF8Encoding(true));
                    AssetDatabase.ImportAsset(outputPath);

                    var hashNote = previousHash == null ? $"SchemeHash {scheme.Hash}"
                        : previousHash == scheme.Hash ? $"後台樣板無變更（SchemeHash {scheme.Hash}）"
                        : $"後台樣板已變更：SchemeHash {previousHash} → {scheme.Hash}";
                    SetSchemeMessage(
                        $"已產生 {outputPath}\n{hashNote}\n頁面 {scheme.Pages.Count}、進度節點 {scheme.ProgressNodes.Count}",
                        MessageType.Info);
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<MonoScript>(outputPath));
                }
                catch (Exception ex)
                {
                    SetSchemeMessage($"[{ex.GetType().Name}]\n{ex.Message}", MessageType.Error);
                    Debug.LogException(ex);
                }
                finally
                {
                    m_isGeneratingScheme = false;
                    Repaint();
                }
            });
        }

        private void SetSchemeMessage(string message, MessageType type)
        {
            m_schemeMessage = message;
            m_schemeMessageType = type;
            Repaint();
        }

        /// <summary>專案裡已經有 ProjectScheme.cs 就預設輸出到那裡</summary>
        private static string FindExistingSchemeScript()
        {
            return AssetDatabase.FindAssets("ProjectScheme t:MonoScript")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => Path.GetFileName(p) == "ProjectScheme.cs");
        }

        #endregion
    }
}
