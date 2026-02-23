using UnityEditor;

using UnityEngine;

namespace EWova.DeepLink.Editor
{
    [CustomEditor(typeof(Config))]
    public class ConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var config = (Config)target;

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "此應用程式的 DeepLink Scheme\n\n" +
                "請將 'example' 替換為你的 Scheme 文字。\n" +
                "例子中的 example 讓你可以使用 example:// 這樣的 DeepLink 連結來啟動應用程式。\n\n" +
                "注意：Scheme 文字只能包含小寫字母、數字，不要使用特殊符號，也盡量不要出現大寫字母，並且不能以數字開頭。\n" +
                "例如：myapp、myapp123 等。",
                MessageType.Info
            );

            EditorGUILayout.Space();

            config.MyAppScheme = EditorGUILayout.TextField("My App Scheme", config.MyAppScheme);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(config);
            }
        }
    }
}