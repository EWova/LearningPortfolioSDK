using System;

using UnityEngine.Scripting;

namespace EWova.LearningPortfolio
{
    // Api 內的類別僅透過網路層以反射方式進行 JSON 序列化/反序列化，
    // 從未在程式碼中以 `new` 直接建構或直接存取欄位，
    // IL2CPP 在 Managed Stripping Level 較高時可能將其建構子/欄位視為未使用而裁剪，
    // 故加上 [Preserve] 避免打包後於裝置上反序列化失敗或欄位遺失。
    [Preserve]
    public partial class Api
    {
        [Preserve]
        public class VerifyProjectInfo
        {
            public bool IsValid;
            public string ErrorMessage;
            public Guid ProjectId;
        }
        [Preserve]
        public class Project
        {
            public Guid Id;
            public Guid OrgId;
            public string UniqueName;
            public string Publicity; // Private, Internal, Public
            public string Name;
            public string Description;
            public string ThumbnailUrl;
            public string SupportMail;
            public override string ToString()
            {
                return
                    $"Label = {Name},\n" +
                    $"Id = {Id},\n" +
                    $"OrgId = {OrgId},\n" +
                    $"UniqueName = {UniqueName},\n" +
                    $"Publicity = {Publicity},\n" +
                    $"Description = {Description},\n" +
                    $"ThumbnailUrl = {ThumbnailUrl},\n" +
                    $"SupportMail = {SupportMail}";
            }
        }
        [Preserve]
        public class Sheet
        {
            public Guid Id;
            public Guid ProjectId;
            public Guid UserId;
            public string Name;
            public DateTime CreatedTime;
            public DateTime LastUpdated;
            public string[] PageLabels;
            public float CompletionProgress;
            public ProgressNode ProgressNode;
            public ProgressCompletion[] ProgressCompletions;
        }
        [Preserve]
        public class ProgressNode
        {
            public string Id;
            public string Label;
            public string Description;
            public string IconUrl;
            public int ScoreWeight;
            public bool Hidden;
            public ProgressNode[] Children;
        }
        [Preserve]
        public class ProgressCompletion
        {
            public string Path;
            public DateTime DateTime;
        }
        [Preserve]
        public class Page
        {
            public string Label;
            public string[] ColumnLabels;
            public int RowCount;
        }
        [Preserve]
        public class Column
        {
            public string Label;
            public bool IsReadOnly;
            public string FieldType; // Number, String, Boolean
        }

        [Preserve]
        public class ColumnSummary
        {
            public string DisplayValue;
            public string Label;
            public string FieldType; // Number, String, Boolean
            public float Total;
            public int Count;
            public float Average;
        }
        [Preserve]
        public class SetColumnRequest
        {
            public string FieldType;
        }
        [Preserve]
        public class Row
        {
            public string[] Cells;
        }
        [Preserve]
        public class AddRowResponse
        {
            public int RowIndex;
        }
        [Preserve]
        public class SetRowRequest
        {
            public string[] Cells;
        }
        [Preserve]
        public class HeartbeatRequest
        {
            public int trackingId;
        }
        [Preserve]
        public class HeartbeatResponse
        {
            public bool Success;
        }
        [Preserve]
        public partial class SetProjectUsageRecordRequest
        {
            public string OrgId;
            public string Platform;
            public string DeviceModel;
            public bool IsXRActive;
        }
        [Preserve]
        public class ProjectUsageRecordResponse
        {
            public int TrackingID = -1;
        }
        [Preserve]
        public class UserRankingResult
        {
            public int TotalCount;
            public int Start;
            public UserRank[] Items;
        }
        [Preserve]
        public class OrgRankingResult
        {
            public int TotalCount;
            public int Start;
            public OrgRank[] Items;
        }
        [Preserve]
        public class UserRank
        {
            public Guid UserGuid;
            public string OrgIdentifier;
            public int Score;
        }
        [Preserve]
        public class OrgRank
        {
            public Guid OrgGuid;
            public int Score;
        }
    }
}
