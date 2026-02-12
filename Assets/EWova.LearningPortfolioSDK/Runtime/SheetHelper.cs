using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace EWova.LearningPortfolio
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class ColumnAttribute : Attribute
    {
        public string CustomLabel { get; private set; }
        public ColumnAttribute() { }
        public ColumnAttribute(string customLabel)
        {
            CustomLabel = customLabel;
        }
    }
    /// <summary>
    /// 提供格式化物件值的靜態方法。
    /// 這些數值轉換都確保可逆 Parse 的格式。
    /// </summary>
    public static class SheetHelper
    {
        public static readonly Dictionary<Type, (Func<object, string> FormatFunc, Func<string, object> ParseFunc)>
            TypeFormatters = new()
            {
                [typeof(bool)] = (
                    o => (bool)o ? "true" : "false",
                    s => bool.TryParse(s, out var b) ? b : default
                ),
                [typeof(byte)] = (
                    o => ((byte)o).ToString(CultureInfo.InvariantCulture),
                    s => byte.TryParse(s, out var b) ? b : default
                ),
                [typeof(char)] = (
                    o => ((char)o).ToString(CultureInfo.InvariantCulture),
                    s => char.TryParse(s, out var c) ? c : default
                ),
                [typeof(double)] = (
                    o => ((double)o).ToString("#.##", CultureInfo.InvariantCulture),
                    s => double.TryParse(s, out var d) ? d : default
                ),
                [typeof(int)] = (
                    o => ((int)o).ToString(CultureInfo.InvariantCulture),
                    s => int.TryParse(s, out var i) ? i : default
                ),
                [typeof(float)] = (
                    o => ((float)o).ToString("#.##", CultureInfo.InvariantCulture),
                    s => float.TryParse(s, out var f) ? f : default
                ),
                [typeof(decimal)] = (
                    o => ((decimal)o).ToString("#.##", CultureInfo.InvariantCulture),
                    s => decimal.TryParse(s, out var m) ? m : default
                ),
                [typeof(string)] = (
                    o => (string)o,
                    s => s
                ),
                // 輸出 "s" 格式 ISO 8601: "2025-09-25T14:30:00"
                [typeof(DateTime)] = (
                    o => ((DateTime)o).ToString("s", CultureInfo.InvariantCulture),
                    s => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : DateTime.MinValue
                ),
                // 四捨五入到秒 輸出 "c" 格式 "1.02:03:04" (1天2小時3分鐘4秒)
                [typeof(TimeSpan)] = (
                    o => TimeSpan.FromSeconds(Math.Round(((TimeSpan)o).TotalSeconds)).ToString("c", CultureInfo.InvariantCulture),
                    s => TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts) ? ts : TimeSpan.Zero
                )
            };

        public static string FormatAny(object obj)
        {
            if (obj == null) return
                    string.Empty;

            var type = obj.GetType();
            if (TypeFormatters.TryGetValue(type, out var funcs))
                return funcs.FormatFunc(obj);

            return obj.ToString();
        }

        public static object ParseAny(Type type, string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                if (type.IsValueType)
                    return Activator.CreateInstance(type);
                return null;
            }

            if (TypeFormatters.TryGetValue(type, out var funcs))
                return funcs.ParseFunc(str);

            return Convert.ChangeType(str, type, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 將物件資料寫入字典
        /// </summary>
        public static void WriteTo(in object sourceObj, Dictionary<string, string> outputDic)
        {
            if (sourceObj == null)
                throw new ArgumentNullException(nameof(sourceObj));

            if (outputDic == null)
                throw new ArgumentNullException(nameof(outputDic));

            var type = sourceObj.GetType();
            IEnumerable<FieldInfo> props = type.GetFields().Where(p => Attribute.IsDefined(p, typeof(ColumnAttribute)));
            foreach (var prop in props)
            {
                ColumnAttribute attr = (ColumnAttribute)Attribute.GetCustomAttribute(prop, typeof(ColumnAttribute));
                object value = prop.GetValue(sourceObj);
                string strValue = FormatAny(value);
                string key = attr.CustomLabel ?? prop.Name;
                if (outputDic.ContainsKey(key))
                    outputDic[key] = strValue;
            }
        }

        /// <summary>
        /// 將物件資料寫入資料列
        /// </summary>
        public static string[] WriteToRow(in object sourceObj, LearningPortfolio.Row targetRow)
        {
            Dictionary<string, string> result = targetRow.Cells.ToDictionary(k => k.ColumnLabel, v => (string)null);
            WriteTo(sourceObj, result);
            return result.Values.ToArray();
        }

        /// <summary>
        /// 從字典讀取資料到物件
        /// </summary>
        public static void ReadFrom<T>(Dictionary<string, string> source, ref T destinationObj)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destinationObj == null)
                throw new ArgumentNullException(nameof(destinationObj));

            Internal_ReadFrom(source, ref destinationObj);
        }

        /// <summary>
        /// 從資料列讀取資料到物件
        /// </summary>
        public static void ReadFromRow<T>(LearningPortfolio.Row sourceRow, ref T destinationObj)
        {
            if (sourceRow == null)
                throw new ArgumentNullException(nameof(sourceRow));

            if (destinationObj == null)
                throw new ArgumentNullException(nameof(destinationObj));

            var source = sourceRow.GetData();
            Internal_ReadFrom(source, ref destinationObj);
        }

        /// <summary>
        /// 取得欄位對應的 ColumnAttribute 標籤名稱
        /// </summary>
        public static string GetColumnLabel<T>(string fieldName)
        {
            var cache = RetrieveFieldMappings(typeof(T));

            if (cache == null)
                throw new ArgumentException($"Type {typeof(T).FullName} has no fields with ColumnAttribute.");

            var mapping = cache.FirstOrDefault(f => f.field.Name == fieldName);

            if (mapping.field == null)
                throw new ArgumentException($"Field '{fieldName}' not found in type {typeof(T).FullName} or it does not have ColumnAttribute.");

            return mapping.name;
        }

        private readonly static Dictionary<Type, (FieldInfo field, string name)[]> s_typeFieldCache = new();
        private static (FieldInfo field, string name)[] RetrieveFieldMappings(Type type)
        {
            if (!s_typeFieldCache.TryGetValue(type, out (FieldInfo field, string name)[] propsCache))
            {
                propsCache = type.GetFields()
                    .Where(p => Attribute.IsDefined(p, typeof(ColumnAttribute)))
                    .Select(f =>
                    {
                        var attr = Attribute.GetCustomAttribute(f, typeof(ColumnAttribute)) as ColumnAttribute;
                        return (f, attr?.CustomLabel ?? f.Name);
                    })
                    .ToArray();

                s_typeFieldCache[type] = propsCache;
            }

            return propsCache;
        }
        private static void Internal_ReadFrom<T>(Dictionary<string, string> source, ref T destinationObj)
        {
            object boxed = destinationObj;

            var cache = RetrieveFieldMappings(typeof(T));

            foreach (var (field, name) in cache)
            {
                if (!source.TryGetValue(name, out var strValue))
                    continue;

                object value = ParseAny(field.FieldType, strValue);
                field.SetValue(boxed, value);
            }

            destinationObj = (T)boxed;
        }
    }
}