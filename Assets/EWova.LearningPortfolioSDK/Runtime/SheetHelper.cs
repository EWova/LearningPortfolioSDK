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
            if (obj == null)
                return string.Empty;

            var type = obj.GetType();
            if (TypeFormatters.TryGetValue(type, out var funcs))
                return funcs.FormatFunc(obj);

            // Enum 沒有登記在 TypeFormatters 中，ToString() 已可輸出可逆格式（名稱），交由 ParseAny 用 Enum.Parse 還原。
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

            if (type.IsEnum)
            {
                try
                {
                    return Enum.Parse(type, str, true);
                }
                catch (Exception)
                {
                    return Activator.CreateInstance(type);
                }
            }

            return Convert.ChangeType(str, type, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 將物件資料寫入字典。
        /// 僅會覆寫 <paramref name="outputDic"/> 中已存在的鍵，不會新增新的鍵值。
        /// </summary>
        public static void WriteTo(object sourceObj, Dictionary<string, string> outputDic)
        {
            if (sourceObj == null)
                throw new ArgumentNullException(nameof(sourceObj));

            if (outputDic == null)
                throw new ArgumentNullException(nameof(outputDic));

            var mapping = RetrieveFieldMappings(sourceObj.GetType());
            foreach (var (field, label) in mapping.Fields)
            {
                if (!outputDic.ContainsKey(label))
                    continue;

                object value = field.GetValue(sourceObj);
                outputDic[label] = FormatAny(value);
            }
        }

        /// <summary>
        /// 將物件資料寫入資料列，回傳依資料列儲存格順序排列的字串陣列（可直接用於 SetCells.Request）。
        /// 資料列中沒有對應物件欄位的儲存格，其值為 null。
        /// </summary>
        public static string[] WriteToRow(object sourceObj, LearningPortfolio.Row targetRow)
        {
            if (sourceObj == null)
                throw new ArgumentNullException(nameof(sourceObj));

            if (targetRow == null)
                throw new ArgumentNullException(nameof(targetRow));

            var mapping = RetrieveFieldMappings(sourceObj.GetType());

            var valueByLabel = new Dictionary<string, string>(mapping.Fields.Length);
            foreach (var (field, label) in mapping.Fields)
                valueByLabel[label] = FormatAny(field.GetValue(sourceObj));

            var cells = targetRow.Cells;
            var result = new string[cells.Count];
            for (int i = 0; i < cells.Count; i++)
                valueByLabel.TryGetValue(cells[i].ColumnLabel, out result[i]);

            return result;
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
            var mapping = RetrieveFieldMappings(typeof(T));

            if (mapping.Fields.Length == 0)
                throw new ArgumentException($"Type {typeof(T).FullName} has no fields with ColumnAttribute.");

            if (!mapping.LabelByFieldName.TryGetValue(fieldName, out var label))
                throw new ArgumentException($"Field '{fieldName}' not found in type {typeof(T).FullName} or it does not have ColumnAttribute.");

            return label;
        }

        private readonly struct FieldMapping
        {
            public readonly (FieldInfo field, string label)[] Fields;
            public readonly Dictionary<string, string> LabelByFieldName;

            public FieldMapping((FieldInfo field, string label)[] fields, Dictionary<string, string> labelByFieldName)
            {
                Fields = fields;
                LabelByFieldName = labelByFieldName;
            }
        }

        private readonly static Dictionary<Type, FieldMapping> s_typeFieldCache = new();
        private static FieldMapping RetrieveFieldMappings(Type type)
        {
            if (!s_typeFieldCache.TryGetValue(type, out var mapping))
            {
                var fields = type.GetFields()
                    .Select(f => (field: f, attr: Attribute.GetCustomAttribute(f, typeof(ColumnAttribute)) as ColumnAttribute))
                    .Where(x => x.attr != null)
                    .Select(x => (x.field, label: x.attr.CustomLabel ?? x.field.Name))
                    .ToArray();

                var labelByFieldName = fields.ToDictionary(f => f.field.Name, f => f.label);

                mapping = new FieldMapping(fields, labelByFieldName);
                s_typeFieldCache[type] = mapping;
            }

            return mapping;
        }
        private static void Internal_ReadFrom<T>(Dictionary<string, string> source, ref T destinationObj)
        {
            object boxed = destinationObj;

            var mapping = RetrieveFieldMappings(typeof(T));

            foreach (var (field, label) in mapping.Fields)
            {
                if (!source.TryGetValue(label, out var strValue))
                    continue;

                object value = ParseAny(field.FieldType, strValue);
                field.SetValue(boxed, value);
            }

            destinationObj = (T)boxed;
        }
    }

    /// <summary>
    /// <see cref="SheetHelper"/> 的擴充方法，提供更貼近物件導向風格的呼叫方式。
    /// </summary>
    public static class SheetHelperExtensions
    {
        /// <summary>
        /// 將物件資料寫入字典。
        /// 僅會覆寫字典中已存在的鍵，不會新增新的鍵值。
        /// </summary>
        public static void WriteTo(this object sourceObj, Dictionary<string, string> outputDic)
            => SheetHelper.WriteTo(sourceObj, outputDic);

        /// <summary>
        /// 將物件資料寫入資料列，回傳依資料列儲存格順序排列的字串陣列（可直接用於 SetCells.Request）。
        /// </summary>
        public static string[] WriteToRow(this object sourceObj, LearningPortfolio.Row targetRow)
            => SheetHelper.WriteToRow(sourceObj, targetRow);

        /// <summary>
        /// 從字典讀取資料到物件
        /// </summary>
        public static void ReadFrom<T>(this Dictionary<string, string> source, ref T destinationObj)
            => SheetHelper.ReadFrom(source, ref destinationObj);

        /// <summary>
        /// 從資料列讀取資料到物件
        /// </summary>
        public static void ReadFromRow<T>(this LearningPortfolio.Row sourceRow, ref T destinationObj)
            => SheetHelper.ReadFromRow(sourceRow, ref destinationObj);
    }
}
