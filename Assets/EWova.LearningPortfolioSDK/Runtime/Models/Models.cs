using System;

namespace EWova.LearningPortfolio
{
    public class Api
    {
        public class VerifyProjectInfo
        {
            public bool IsValid;
            public string ErrorMessage;
            public Guid ProjectId;
        }
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
        public class ProgressCompletion
        {
            public string Path;
            public DateTime DateTime;
        }
        public class Page
        {
            public string Label;
            public string[] ColumnLabels;
            public int RowCount;
        }
        public class Column
        {
            public string Label;
            public bool IsReadOnly;
            public string FieldType; // Number, String, Boolean
        }

        public class ColumnSummary
        {
            public string DisplayValue;
            public string Label;
            public string FieldType; // Number, String, Boolean
            public float Total;
            public int Count;
            public float Average;
        }
        public class SetColumnRequest
        {
            public string FieldType;
        }
        public class Row
        {
            public string[] Cells;
        }
        public class AddRowResponse
        {
            public int RowIndex;
        }
        public class SetRowRequest
        {
            public string[] Cells;
        }
        public class HeartbeatRequest
        {
            public int trackingId;
        }
        public class HeartbeatResponse
        {
            public bool Success;
        }
        public class SetProjectUsageRecordRequest
        {
            [Obsolete("已棄用從 Client 端 Mapping 裝置類型的方式")]
            public int UsingDeviceId;

            public string OrgId;
            public string Platform;
            public string DeviceModel;
            public bool IsXRActive;
        }
        public class ProjectUsageRecordResponse
        {
            public int TrackingID = -1;
        }
        public class UserRankingResult
        {
            public int TotalCount;
            public int Start;
            public UserRank[] Items;
        }
        public class OrgRankingResult
        {
            public int TotalCount;
            public int Start;
            public OrgRank[] Items;
        }
        public class UserRank
        {
            public Guid UserGuid;
            public string OrgIdentifier;
            public int Score;
        }
        public class OrgRank
        {
            public Guid OrgGuid;
            public int Score;
        }
    }
}