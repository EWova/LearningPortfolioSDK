using Cysharp.Threading.Tasks;

using Newtonsoft.Json;

using UnityEditor;

using UnityEngine;

namespace EWova.LearningPortfolio.Editor
{
    [CustomEditor(typeof(LearningPortfolioProfile))]
    public class LearningPortfolioProfileEditor : UnityEditor.Editor
    {
        private SerializedProperty m_stringApiKey;

        private int m_isApiKeyValid = -1;
        private string m_message = null;

        private void OnEnable()
        {
            m_stringApiKey = serializedObject.FindProperty("APISettings").FindPropertyRelative("APIKey");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("API Key", GUILayout.MaxWidth(100));
            m_stringApiKey.stringValue = EditorGUILayout.TextField(m_stringApiKey.stringValue, GUILayout.ExpandWidth(true)).Trim();
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

                var apiClient = new LPApiClient(new ProjectSettings(m_stringApiKey.stringValue), null, null);

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

            serializedObject.ApplyModifiedProperties();
        }
    }
}