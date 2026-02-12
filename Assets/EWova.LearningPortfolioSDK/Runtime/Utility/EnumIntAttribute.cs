using System;

using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class EnumIntAttribute : PropertyAttribute
{
    public Type EnumType { get; }

    public EnumIntAttribute(Type enumType)
    {
        EnumType = enumType;
    }
}