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
                [typeof(DateTimeOffset)] = (
                    o => ((DateTimeOffset)o).ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture),
                    s => DateTimeOffset.TryParse(
                        s,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out var dto
                    ) ? dto : DateTimeOffset.MinValue
                ),
                // 四捨五入到秒 輸出 "c" 格式 "1.02:03:04" (1天2小時3分鐘4秒)
                [typeof(TimeSpan)] = (
                    o => TimeSpan.FromSeconds(Math.Round(((TimeSpan)o).TotalSeconds)).ToString("c", CultureInfo.InvariantCulture),
                    s => TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts) ? ts : TimeSpan.Zero
                ),
            };

        public static string FormatAny(object obj)
        {
            if (obj == null)
                return string.Empty;

            var type = obj.GetType();

            if (type == typeof(DateTime))
            {
                throw new NotSupportedException(
                    "DateTime is not supported because it does not preserve an explicit UTC offset. " +
                    "Please use DateTimeOffset instead."
                );
            }

            if (TypeFormatters.TryGetValue(type, out var funcs))
                return funcs.FormatFunc(obj);

            // Enum 沒有登記在 TypeFormatters 中，ToString() 已可輸出可逆格式（名稱），交由 ParseAny 用 Enum.Parse 還原。
            if (type.IsEnum)
                return obj.ToString();

            throw new NotSupportedException($"Type {type.FullName} is not supported.");
        }

        public static object ParseAny(Type type, string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                if (type.IsValueType)
                    return Activator.CreateInstance(type);
                return null;
            }

            if (type == typeof(DateTime))
            {
                UnityEngine.Debug.LogError(
                    "DateTime is not supported. Please use DateTimeOffset instead, " +
                    "because DateTime does not reliably preserve UTC/offset information."
                );

                DateTimeOffset dto = (DateTimeOffset)TypeFormatters[typeof(DateTimeOffset)].ParseFunc(str);

                // 舊版資料相容 統一視為本地時間 這可能會造成 v1.3.0 升級使用者資料錯誤
                return dto.DateTime;
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

            throw new NotSupportedException($"Type {type.FullName} is not supported.");
        }

        public static bool TryParseAny<T>(string str, out T result)
        {
            try
            {
                result = (T)ParseAny(typeof(T), str);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// 將物件欄位值依 <paramref name="targetPage"/> 的欄位順序對齊，組成字串陣列，用於「新增一筆列」的情境
        /// （可直接用於 <c>AddRowAndSetCells.Request</c>），故不需要（也不會用到）既有的 Row。
        /// 物件沒有對應到的欄位，其值為 null。
        /// </summary>
        public static string[] AlignToColumns(object sourceObj, LearningPortfolio.Page targetPage)
        {
            if (sourceObj == null)
                throw new ArgumentNullException(nameof(sourceObj));

            if (targetPage == null)
                throw new ArgumentNullException(nameof(targetPage));

            var mapping = RetrieveFieldMappings(sourceObj.GetType());
            var valueByLabel = new Dictionary<string, string>(mapping.Fields.Length);
            foreach (var (field, label) in mapping.Fields)
                valueByLabel[label] = FormatAny(field.GetValue(sourceObj));

            string[] columnLabels = targetPage.GetColumnsLabel();
            string[] result = new string[columnLabels.Length];
            for (int i = 0; i < columnLabels.Length; i++)
                valueByLabel.TryGetValue(columnLabels[i], out result[i]);

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
        /// 建立一個新的 <typeparamref name="T"/> 執行個體，並從字典讀取資料填入（等同於 <c>new T()</c> 後再呼叫 <see cref="ReadFrom{T}"/>）。
        /// 注意：每次呼叫都會配置新物件，會產生 GC 配置；若需重複讀取多筆資料，建議改用 <see cref="ReadFrom{T}"/> 搭配既有物件重複利用，以避免額外 GC。
        /// </summary>
        public static T CreateFrom<T>(Dictionary<string, string> source) where T : new()
        {
            T destinationObj = new();
            ReadFrom(source, ref destinationObj);
            return destinationObj;
        }

        /// <summary>
        /// 建立一個新的 <typeparamref name="T"/> 執行個體，並從資料列讀取資料填入（等同於 <c>new T()</c> 後再呼叫 <see cref="ReadFromRow{T}"/>）。
        /// 注意：每次呼叫都會配置新物件，會產生 GC 配置；若需重複讀取多筆資料，建議改用 <see cref="ReadFromRow{T}"/> 搭配既有物件重複利用，以避免額外 GC。
        /// </summary>
        public static T CreateFromRow<T>(LearningPortfolio.Row sourceRow) where T : new()
        {
            T destinationObj = new();
            ReadFromRow(sourceRow, ref destinationObj);
            return destinationObj;
        }

        /// <summary>
        /// 預先建立並快取 <typeparamref name="T"/> 的欄位對應資訊。
        /// 欄位對應本來就會在第一次使用該型別時自動快取，此方法僅用於「明確指定時機」預先付出這筆一次性的反射成本
        /// （例如在 Loading 畫面呼叫，避免第一次 AlignToColumns/ReadFrom 等操作剛好發生在遊玩當下造成的 hitch）。
        /// </summary>
        public static void WarmUp<T>() => RetrieveFieldMappings(typeof(T));

        /// <summary>
        /// 預先建立並快取多個型別的欄位對應資訊，用途同 <see cref="WarmUp{T}"/>。
        /// </summary>
        public static void WarmUp(params Type[] types)
        {
            if (types == null)
                return;

            foreach (var type in types)
                RetrieveFieldMappings(type);
        }

        /// <summary>
        /// 釋放 <typeparamref name="T"/> 已快取的欄位對應資訊（對應 <see cref="WarmUp{T}"/>）。
        /// 欄位對應本身很小，通常不需要主動釋放；若該型別已確定不再使用（例如卸載某個場景/關卡專屬的大量 Scheme），
        /// 可呼叫此方法釋放快取。之後若再次使用該型別，會自動重新建立並快取。
        /// </summary>
        public static void Release<T>() => s_typeFieldCache.Remove(typeof(T));

        /// <summary>
        /// 釋放多個型別已快取的欄位對應資訊，用途同 <see cref="Release{T}"/>。
        /// </summary>
        public static void Release(params Type[] types)
        {
            if (types == null)
                return;

            foreach (var type in types)
                s_typeFieldCache.Remove(type);
        }

        /// <summary>
        /// 釋放所有型別已快取的欄位對應資訊。
        /// </summary>
        public static void ReleaseAll() => s_typeFieldCache.Clear();

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
        /// 將物件欄位值依頁面欄位順序對齊，組成字串陣列，用於「新增一筆列」的情境（可直接用於 AddRowAndSetCells.Request）。
        /// </summary>
        public static string[] AlignToColumns(this object sourceObj, LearningPortfolio.Page targetPage)
            => SheetHelper.AlignToColumns(sourceObj, targetPage);

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

        /// <summary>
        /// 建立一個新的 <typeparamref name="T"/> 執行個體，並從字典讀取資料填入。
        /// 注意：每次呼叫都會配置新物件，會產生 GC 配置；重複讀取多筆資料時建議改用 <see cref="ReadFrom{T}"/> 重複利用既有物件。
        /// </summary>
        public static T CreateFrom<T>(this Dictionary<string, string> source) where T : new()
            => SheetHelper.CreateFrom<T>(source);

        /// <summary>
        /// 建立一個新的 <typeparamref name="T"/> 執行個體，並從資料列讀取資料填入。
        /// 注意：每次呼叫都會配置新物件，會產生 GC 配置；重複讀取多筆資料時建議改用 <see cref="ReadFromRow{T}"/> 重複利用既有物件。
        /// </summary>
        public static T CreateFromRow<T>(this LearningPortfolio.Row sourceRow) where T : new()
            => SheetHelper.CreateFromRow<T>(sourceRow);
    }
}
