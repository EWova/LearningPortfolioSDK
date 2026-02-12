using System;

using UnityEditor;

using UnityEngine;

[CustomPropertyDrawer(typeof(EnumIntAttribute))]
public class EnumIntDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnumIntAttribute enumAttr = (EnumIntAttribute)attribute;

        if (enumAttr.EnumType.IsEnum && property.propertyType == SerializedPropertyType.Integer)
        {
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            float remainingWidth = position.width - EditorGUIUtility.labelWidth;
            float halfWidth = remainingWidth * 0.5f;

            Rect intRect = new Rect(labelRect.xMax, position.y, halfWidth, position.height);
            Rect enumRect = new Rect(intRect.xMax, position.y, halfWidth, position.height);

            EditorGUI.LabelField(labelRect, label);

            int newInt = EditorGUI.IntField(intRect, GUIContent.none, property.intValue);

            Enum currentEnum = (Enum)Enum.ToObject(enumAttr.EnumType, property.intValue);
            Enum newEnum = EditorGUI.EnumPopup(enumRect, GUIContent.none, currentEnum);


            if (Convert.ToInt32(newEnum) != property.intValue)
            {
                property.intValue = Convert.ToInt32(newEnum);
            }
            else if (newInt != property.intValue)
            {
                property.intValue = newInt;
            }
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use [EnumInt] with int + enum type.");
        }
    }
}