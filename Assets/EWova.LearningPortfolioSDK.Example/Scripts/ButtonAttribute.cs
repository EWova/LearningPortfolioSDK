using System;
#if UNITY_EDITOR
using System.Reflection;

using UnityEngine;

using UnityEditor;
#endif
namespace Test
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ButtonAttribute : Attribute
    {
        public string Name { get; }
        public float Space { get; set; } = 0f;
        public ButtonAttribute(string name = null) { Name = name; }
    }

#if UNITY_EDITOR
    [CanEditMultipleObjects, CustomEditor(typeof(MonoBehaviour), true)]
    public class ButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MethodInfo[] methods = target.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (MethodInfo method in methods)
            {
                ButtonAttribute buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();
                if (buttonAttribute == null)
                    continue;
                if (method.GetParameters().Length > 0)
                {
                    Debug.LogWarning($"[Button] 只支援無參數方法: {method.DeclaringType.Name}.{method.Name}");
                    continue;
                }

                if (buttonAttribute.Space > 0f)
                    GUILayout.Space(buttonAttribute.Space);

                string buttonName = string.IsNullOrEmpty(buttonAttribute.Name)
                    ? ObjectNames.NicifyVariableName(method.Name)
                    : buttonAttribute.Name;

                if (GUILayout.Button(buttonName))
                {
                    foreach (UnityEngine.Object t in targets)
                        method.Invoke(t, null);
                }
            }
        }
    }
#endif
}
