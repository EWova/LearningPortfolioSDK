using Cysharp.Threading.Tasks;

using System;

namespace EWova.LearningPortfolio 
{
    public static class ErrorHandleExtensions
    {
        public static async UniTask<bool> CatchAsBool(this UniTask asyncAction, Action<Exception> onCatch = null)
        {
            try
            {
                await asyncAction;
                return true;
            }
            catch (Exception ex)
            {
                onCatch?.Invoke(ex);
                return false;
            }
        }
    }
    public class ErrorHandleException : Exception
    {
        public string Endpoint { get; }
        public string Title { get; }
        public string ErrorMessage { get; }
        public ErrorHandleException(string title, string message, string endpoint)
            : base($"[{title}]: {message}. endpoint:{endpoint}")
        {
            Endpoint = endpoint;
            Title = title;
            ErrorMessage = $"[{title}]: {message}. endpoint: {endpoint}";
        }
        public ErrorHandleException(ErrorHandle error)
            : this(error?.Title ?? "Error", error?.Message ?? "No message provided", error.Endpoint)
        {
        }
    }
    public class ErrorHandle
    {
        public string Endpoint;
        public string Message;
        public string Title;
    }

    public class API 
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
            public string Label;
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
        public class SetProjectUsageRecordRequest
        {
            public int UsingDeviceId;
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