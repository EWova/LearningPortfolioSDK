using Cysharp.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.XR;

namespace EWova.LearningPortfolio
{
    public partial class LearningPortfolio
    {
        public enum UsingDeviceList : int
        {
            // 如果有其他裝置需求請到官網裝置列表查詢裝置ID
            Editor = -1,
            Unknown = 0,
            Windows = 1000,
            Windows_VR = 1500,
            macOS = 2000,
            iOS = 3000,
            visionOS = 4000,
            Linux = 5000,
            Android = 6000,
            AllInOne = 6500,
            AllInOne_Meta_Quest = 6501,
            AllInOne_HTC_VIVE = 6502,
            Web = 7000,
            Web_VR = 7500,
        }
        [Serializable]
        public class LoginRequestData
        {
            [Tooltip("目前使用裝置追蹤ID，如果有其他裝置需求請到官網裝置列表查詢")]
            [EnumInt(typeof(UsingDeviceList))]
            public int UsingDeviceId = 0;

            public static LoginRequestData Create()
            {
                LoginRequestData data = new();
                UsingDeviceList device;
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.LinuxEditor:
                        device = UsingDeviceList.Editor;
                        break;

                    case RuntimePlatform.WebGLPlayer:
                        if (IsXRRunning())
                            device = UsingDeviceList.Web_VR;
                        else
                            device = UsingDeviceList.Web;
                        break;

                    case RuntimePlatform.Android:
                        if (IsXRRunning())
                        {
                            var modelName = SystemInfo.deviceModel.ToLower();
                            if (modelName.Contains("quest") || modelName.Contains("oculus"))
                                device = UsingDeviceList.AllInOne_Meta_Quest;
                            else if (modelName.Contains("vive") || modelName.Contains("htc"))
                                device = UsingDeviceList.AllInOne_HTC_VIVE;
                            else
                                device = UsingDeviceList.AllInOne;
                        }
                        else
                            device = UsingDeviceList.Android;
                        break;

                    case RuntimePlatform.OSXPlayer:
                        device = UsingDeviceList.macOS;
                        break;
                    case RuntimePlatform.IPhonePlayer:
                        device = UsingDeviceList.iOS;
                        break;
#if UNITY_2023_2_OR_NEWER || UNITY_2022_3
                    case RuntimePlatform.VisionOS:
                        device = UsingDeviceList.visionOS;
                        break;
#endif

                    case RuntimePlatform.LinuxPlayer:
                        device = UsingDeviceList.Linux;
                        break;

                    case RuntimePlatform.WindowsPlayer:
                        if (IsXRRunning())
                            device = UsingDeviceList.Windows_VR;
                        else
                            device = UsingDeviceList.Windows;
                        break;

                    default:
                        device = UsingDeviceList.Unknown;
                        break;
                }
                data.UsingDeviceId = (int)device;
                return data;
            }
        }
        private static bool IsXRRunning()
        {
            List<XRDisplaySubsystem> displaySubsystems = new();
            SubsystemManager.GetSubsystems(displaySubsystems);
            foreach (var d in displaySubsystems)
            {
                if (d.running)
                    return true;
            }
            return false;
        }

        [Serializable]
        public class UserData
        {
            public string Name;
            public string Nickname;
            public string Guid;
            [Space]
            public string OrgName;
            public string OrgGuid;

            public override string ToString()
            {
                return $"{Name}({Nickname}); Org:{OrgName};";
            }
            public string ToString(bool detail)
            {
                return detail ? $"{Name}({Nickname})[{Guid}]; Org:{OrgName}[{OrgGuid}];" : ToString();
            }
        }
        public enum FieldType
        {
            Number,
            String,
            Boolean,
        }

        /*
         Sheet
            +---------+---------+---------+---------+
            |         | Column0 | Column1 | Column2 |
            +---------+---------+---------+---------+
            | Row1    | Cell    | Cell    | Cell    |
            +---------+---------+---------+---------+
            | Row2    | Cell    | Cell    | Cell    |
            +---------+---------+---------+---------+
            | Row3    | Cell    | Cell    | Cell    |
            +---------+---------+---------+---------+
        */

        /// <summary>
        /// 使用者專案記錄表單
        /// </summary>
        public class UserProjectRecordSheet : IDisposable
        {
            public List<UnityEngine.Object> ManagedObjects = new();

            private bool disposedValue;

            internal NetServiceRequestHandler NetServiceHandler { get; set; }

            public UserData Owner { get; internal set; }

            /// <summary>
            /// 是否有任何網路服務正在請求寫入資料
            /// </summary>
            public bool IsAnyNetSerivceRequesting => NetServiceHandler.IsAnyNetSerivceRequesting;

            /// <summary>
            /// 使用者專案記錄表單ID
            /// </summary>
            public string ProjectId { get; internal set; }

            /// <summary>
            /// 使用者專案記錄表單名稱
            /// </summary>
            public string Name { get; internal set; }
            /// <summary>
            /// 表單的唯一識別ID
            /// </summary>
            public string SheetId { get; internal set; }
            /// <summary>
            /// 擁有者識別ID
            /// </summary>
            public string UserId { get; internal set; }
            /// <summary>
            /// 表單的最後更新時間（本地時間)
            /// </summary>
            public DateTime LastUpdatedLocal { get; internal set; }
            /// <summary>
            /// 表單分頁 (第0頁即總覽)
            /// </summary>
            public Page[] Pages { get; internal set; }
            /// <summary>
            /// 取得所有分頁的標籤名稱
            /// </summary>
            public string[] GetPagesLabel() => Pages != null ? Array.ConvertAll(Pages, p => p.Label) : Array.Empty<string>();

            /// <summary>
            /// 使用者該紀錄完成度，透過進度節點的分數權重計算出的 (0.0 ~ 1.0)
            /// </summary>
            public float CompletionProgress { get; internal set; }
            /// <summary>
            /// 進度節點樹狀結構 (可能為 null，表示無進度節點)
            /// </summary>
            public ProgressNode ProgressNode { get; internal set; }
            /// <summary>
            /// 已完成的進度節點路徑清單 (格式為 "根節點/子節點1/子節點2/..." )。
            /// 請注意，此路徑可能包含不存在的 Node ID (表示該節點已被刪除但完成紀錄仍保留)
            /// 你也可以透過這個特性使用 <c>MarkNonNodeComplete</c> 來記錄不存在的節點作為隱藏進度紀錄
            /// </summary>
            public IReadOnlyList<string> ProgressCompletions { get; internal set; } = new List<string>();
            public IReadOnlyList<DateTime> ProgressCompletionsLocalDateTime { get; internal set; } = new List<DateTime>();
            /// <summary>
            /// 所有進度節點的路徑對照表 (格式為 "根節點/子節點1/子節點2/..." => ProgressNode ) key: StringComparer.OrdinalIgnoreCase
            /// </summary>
            public IReadOnlyDictionary<string, ProgressNode> AllProgressNodesPathMap { get; internal set; }
            /// <summary>
            /// 所有進度節點
            /// </summary>
            public IEnumerable<ProgressNode> AllProgressNodes => AllProgressNodesPathMap.Values;
            /// <summary>
            /// 透過路徑尋找進度節點
            /// </summary>
            public bool FindProgressNodeByPath(string path, out ProgressNode progressNode)
            {
                progressNode = null;
                if (string.IsNullOrEmpty(path) || ProgressNode == null)
                    return false;
                if (!AllProgressNodesPathMap.ContainsKey(path))
                    return false;
                progressNode = AllProgressNodesPathMap[path];
                return true;
            }

            /// <summary>
            /// [網路服務請求] 標記某路徑為已完成 (節點可能不存在，但允許標記完成。可能用於隱藏進度紀錄)
            /// </summary>
            public NetSerivceRequest<string> SetCompleteIncludeNonNode { get; internal set; }
            /// <summary>
            /// [網路服務請求] 取消某路徑已完成標記 (節點可能不存在，但允許標記完成。可能用於隱藏進度紀錄)
            /// </summary>
            public NetSerivceRequest<string> SetUnmarkIncludeNonNode { get; internal set; }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        foreach (var item in ManagedObjects)
                        {
                            if (item != null)
                                DestroyImmediate(item);
                        }
                    }

                    disposedValue = true;
                }
            }
            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
        public class ProgressNode
        {
            /// <summary>
            /// 所屬的使用者專案記錄表單
            /// </summary>
            public UserProjectRecordSheet RootSheet { get; internal set; }
            /// <summary>
            /// 上層節點 (null表示為根節點)
            /// </summary>
            public ProgressNode Parent { get; internal set; }
            /// <summary>
            /// 子節點 (可能為空)
            /// </summary>
            public ProgressNode[] Children { get; internal set; } = new ProgressNode[0];
            public ProgressNode this[string id]
            {
                get
                {
                    if (string.IsNullOrEmpty(id))
                        return null;
                    return Children.FirstOrDefault(n => string.Equals(n.Id, id, StringComparison.OrdinalIgnoreCase));
                }
            }

            /// <summary>
            /// 節點識別ID
            /// </summary>
            public string Id { get; internal set; }
            /// <summary>
            /// 節點標籤名稱
            /// </summary>
            public string Label { get; internal set; }
            /// <summary>
            /// 節點說明文字
            /// </summary>
            public string Description { get; internal set; }
            /// <summary>
            /// 節點圖示 (可能為 null)
            /// </summary>
            public Texture2D IconTex { get; internal set; }
            /// <summary>
            /// 節點分數權重 (用於計算完成度)
            /// </summary>
            public int ScoreWeight { get; internal set; }
            /// <summary>
            /// 透過節點 ScoreWeight 計算出的完成度分數 (用於計算完成度，範圍為 0.0 ~ 1.0)
            /// </summary>
            public float CalculatedProgressScore { get; internal set; }
            /// <summary>
            /// 節點是否可見
            /// </summary>
            public bool IsHidden { get; internal set; }


            /// <summary>
            /// 節點路徑 (用於標記完成度，格式為 "根節點/子節點1/子節點2/..." )
            /// </summary>
            public string Path { get; internal set; }
            /// <summary>
            /// [網路服務請求] 標記該節點為已完成
            /// </summary>
            public NetSerivceVoid SetComplete { get; internal set; }
            /// <summary>
            /// [網路服務請求] 取消標記該節點已完成
            /// </summary>
            public NetSerivceVoid SetUnmark { get; internal set; }
            /// <summary>
            /// 是否已完成 (自己、子節點或父節點其中之一已完成即為完成)
            /// </summary>
            public bool IsCompleted => IsCompletedSelf || IsCompletedChildren() || IsCompletedParent();
            /// <summary>
            /// 是否自己被標記為已完成
            /// </summary>
            public bool IsCompletedSelf => RootSheet.ProgressCompletions.Contains(Path, StringComparer.OrdinalIgnoreCase);
            /// <summary>
            /// 自己被標記為已完成的時間 (本地時間)
            /// </summary>
            public DateTime? CompleteTime
            {
                get
                {
                    int index = ((List<string>)RootSheet.ProgressCompletions).IndexOf(Path);
                    if (index < 0)
                        return null;
                    return RootSheet.ProgressCompletionsLocalDateTime[index];
                }
            }
            /// <summary>
            /// 是否子節點被標記為已完成 (自己未完成且子節點有一個以上被標記為已完成即為完成)
            /// </summary>
            public bool IsCompletedChildren()
            {
                if (IsCompletedSelf)
                    return true;

                if (Children.Length > 0 && Children.All(c => c.IsCompletedChildren()))
                    return true;

                return false;
            }
            /// <summary>
            /// 是否父節點被標記為已完成 (自己未完成且父節點有一個以上被標記為已完成即為完成)
            /// </summary>
            public bool IsCompletedParent()
            {
                if (IsCompletedSelf)
                    return true;

                if (Parent != null && Parent.IsCompletedParent())
                    return true;

                return false;
            }
            /// <summary>
            /// 取得自己與所有子孫節點 (包含自己)
            /// </summary>
            public IEnumerable<ProgressNode> AllProgressNodes
            {
                get
                {
                    static IEnumerable<ProgressNode> Traverse(ProgressNode node)
                    {
                        yield return node;

                        foreach (var child in node.Children ?? Enumerable.Empty<ProgressNode>())
                        {
                            foreach (var descendant in Traverse(child))
                                yield return descendant;
                        }
                    }

                    return Traverse(this);
                }
            }
            public bool IsRoot => Parent == null;
            public bool IsLeaf => Children == null || Children.Length == 0;
        }

        /// <summary>
        /// 使用者專案記錄表單的分頁
        /// </summary>
        public class Page
        {
            /// <summary>
            /// 分頁索引
            /// </summary>
            public int Index { get; internal set; }
            /// <summary>
            /// 上層
            /// </summary>
            public UserProjectRecordSheet RootSheet { get; internal set; }
            /// <summary>
            /// 標籤名稱
            /// </summary>
            public string Label { get; internal set; }
            /// <summary>
            /// 欄位屬性設定
            /// </summary>
            public Column[] Columns { get; internal set; }
            /// <summary>
            /// 資料列
            /// </summary>
            public IReadOnlyDictionary<int, Row> Rows { get; internal set; }

            // m_cell[列][欄]
            internal SortedDictionary<int, List<Cell>> Cells { get; set; }

            /// <summary>
            /// 取得所有欄位的名稱
            /// </summary>
            public string[] GetColumnsLabel() => Columns.Select(x => x.Label).ToArray();

            /// <summary>
            /// [網路服務請求] 加一資料列
            /// </summary>
            public NetSerivceRespond<API.AddRowResponse> AddRow { get; internal set; }
            /// <summary>
            /// [網路服務請求] 加一資料列並設定內容
            /// </summary>
            public NetSerivceRequestRespond<API.SetRowRequest, API.AddRowResponse> AddRowAndSetCells { get; internal set; }
            /// <summary>
            /// [網路服務請求] 清除所有可讀寫資料
            /// </summary>
            public NetSerivceVoid ClearReadableData { get; internal set; }
        }
        /// <summary>
        /// 使用者專案記錄表單的欄位資料
        /// </summary>
        public class Column
        {
            /// <summary>
            /// 欄位索引
            /// </summary>
            public int Index { get; internal set; }
            /// <summary>
            /// 上層
            /// </summary>
            public Page RootPage { get; internal set; }
            /// <summary>
            /// 欄位標籤名稱
            /// </summary>
            public string Label { get; internal set; }
            /// <summary>
            /// 是否為唯讀欄位
            /// </summary>
            public bool IsReadOnly { get; internal set; }
            /// <summary>
            /// (*可修改項目) 欄位參考資料類型
            /// </summary>
            public FieldType FieldType { get; internal set; }
            /// <summary>
            /// 取得該欄垂直的所有儲存格
            /// </summary>
            public IReadOnlyList<Cell> Cells => RootPage.Cells != null
                ? RootPage.Cells.Select(c => c.Value.Count > Index ? c.Value[Index] : new Cell { Text = string.Empty }).ToList()
                : Array.Empty<Cell>();
            /// <summary>
            /// 取得該欄垂直的所有儲存格文字
            /// </summary>
            public string[] GetCellsText() => Cells.Select(c => c.Text).ToArray();
            /// <summary>
            /// [網路服務請求] 修改欄位屬性設定
            /// </summary>
            public NetSerivceRequest<API.SetColumnRequest> Edit { get; internal set; }
            /// <summary>
            /// 儲存格彙總資訊
            /// </summary>
            public string CellsSummary { get; internal set; }
        }
        /// <summary>
        /// 使用者專案記錄表單的資料列
        /// </summary>
        public class Row
        {
            /// <summary>
            /// 資料列索引
            /// </summary>
            public int Index { get; internal set; }
            /// <summary>
            /// 上層
            /// </summary>
            public Page RootPage { get; internal set; }
            /// <summary>
            /// 取得該資料列水平的所有儲存格
            /// </summary>
            public IReadOnlyList<Cell> Cells => RootPage.Cells != null
                    ? RootPage.Cells[Index].ToList()
                    : Array.Empty<Cell>();
            /// <summary>
            /// 取得該資料列水平的所有儲存格文字
            /// </summary>
            public string[] GetCellsText() => Cells.Select(c => c.Text).ToArray();
            /// <summary>
            /// 取得該資料列水平的所有儲存格欄位名稱
            /// </summary>
            public string[] GetCellsLabel() => Cells.Select(c => c.ColumnLabel).ToArray();
            /// <summary>
            /// 取得該資料列的欄位名稱與儲存格文字對應字典
            /// </summary>
            public Dictionary<string, string> GetData() => Cells.ToDictionary(c => c.Column.Label, c => c.Text);
            /// <summary>
            /// [網路服務請求] 修改資料列內容
            /// </summary>
            public NetSerivceRequest<API.SetRowRequest> SetCells { get; internal set; }
        }
        /// <summary>
        /// 使用者專案記錄表單的儲存格資料
        /// </summary>
        public class Cell
        {
            /// <summary>
            /// 儲存格所在的資料列
            /// </summary>
            public Row Row { get; internal set; }
            /// <summary>
            /// 儲存格所在的欄位 (可能不存在)
            /// </summary>
            public Column Column { get; internal set; }
            /// <summary>
            /// 儲存格所在的欄位名稱
            /// </summary>
            public string ColumnLabel => Column.Label;
            /// <summary>
            /// 是否為唯讀儲存格
            /// </summary>
            public bool IsReadOnly => Column != null && Column.IsReadOnly;
            /// <summary>
            /// 儲存格的文字內容
            /// </summary>
            public string Text { get; internal set; }
        }
    }
}
