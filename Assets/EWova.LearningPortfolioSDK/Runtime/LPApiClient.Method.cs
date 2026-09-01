using System;
using System.Collections.Generic;
using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;

using EWova.Networking;

namespace EWova.LearningPortfolio
{
    public enum ApiAction
    {
        Unknown,
        // 專案與驗證
        CheckApiHealth,
        GetApiKeyValidInfo,
        GetProject,
        // 學習歷程紀錄
        FindSheets,
        GetSheet,
        GetPage,
        GetPageColumn,
        GetPageColumns,
        GetPageColumnSummary,
        GetPageColumnsSummary,
        SetPageColumn,
        GetPageRows,
        SetPageRow,
        AddPageRow,
        ClearPageReadableData,
        SetCompleteProgress,
        SetUnmarkProgress,
        GetProgressCompletion,
        // 使用紀錄
        CreateProjectUsageRecord,
        ProjectUsageRecordHeartbeat,
        // 排行榜
        GetProjectUserRanking,
        GetProjectOrgRanking
    }

    public static class ApiClientHelper
    {
        public static void ContinueWithThrowCatch(this UniTask task, Action onCompletion = null, Action<Exception> onException = null)
        {
            Run(task, onCompletion, onException).Forget();
        }
        public static void ContinueWithThrowCatch<T>(this UniTask<T> task, Action<T> onCompletion = null, Action<Exception> onException = null)
        {
            Run(task, onCompletion, onException).Forget();
        }

        private static async UniTaskVoid Run(UniTask task, Action onCompletion, Action<Exception> onError)
        {
            try
            {
                await task;
                onCompletion?.Invoke();
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
        }
        private static async UniTaskVoid Run<T>(UniTask<T> task, Action<T> onCompletion, Action<Exception> onException)
        {
            try
            {
                var result = await task;
                onCompletion?.Invoke(result);
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
            }
        }
    }

    public partial class LPApiClient
    {
        /* https://api-learning-app.ewova.dev/api/docs */

        private const string LearningPortfolioUsedContentType = "application/json";

        // --- 核心 HTTP 方法封裝與 Token 轉發 ---
        internal UniTask<string> Get(string endpoint, CancellationToken ct = default)
            => Send<string>(RequestTask.GET(
                backendUrlOrAbsoluteUrl: endpoint,
                throwApiExceptionFor4xxResponses: true,
                ct: ct));
        internal UniTask<T> Get<T>(string endpoint, CancellationToken ct = default)
            => Send<T>(RequestTask.GET(
                backendUrlOrAbsoluteUrl: endpoint,
                throwApiExceptionFor4xxResponses: true,
                ct: ct));
        internal UniTask<string> Post(string endpoint, object jsonBody, CancellationToken ct = default)
            => Send<string>(RequestTask.POST(
                backendUrlOrAbsoluteUrl: endpoint,
                body: jsonBody,
                contentType: LearningPortfolioUsedContentType,
                throwApiExceptionFor4xxResponses: true,
                ct: ct));
        internal UniTask<T> Post<T>(string endpoint, object jsonBody, CancellationToken ct = default)
            => Send<T>(RequestTask.POST(
                backendUrlOrAbsoluteUrl: endpoint,
                body: jsonBody,
                contentType: LearningPortfolioUsedContentType,
                throwApiExceptionFor4xxResponses: true,
                ct: ct));
        internal UniTask<string> Put(string endpoint, object jsonBody, CancellationToken ct = default)
            => Send<string>(RequestTask.PUT(
                backendUrlOrAbsoluteUrl: endpoint,
                body: jsonBody,
                contentType: LearningPortfolioUsedContentType,
                throwApiExceptionFor4xxResponses: true,
                ct: ct));
        internal UniTask<T> Put<T>(string endpoint, object jsonBody, CancellationToken ct = default)
            => Send<T>(RequestTask.PUT(
                backendUrlOrAbsoluteUrl: endpoint,
                body: jsonBody,
                contentType: LearningPortfolioUsedContentType,
                throwApiExceptionFor4xxResponses: true,
                ct: ct));
        internal UniTask<string> Delete(string endpoint, CancellationToken ct = default)
            => Send<string>(RequestTask.DELETE(
                backendUrlOrAbsoluteUrl: endpoint,
                throwApiExceptionFor4xxResponses: true,
                ct: ct));
        internal UniTask<T> Delete<T>(string endpoint, CancellationToken ct = default)
            => Send<T>(RequestTask.DELETE(
                backendUrlOrAbsoluteUrl: endpoint,
                throwApiExceptionFor4xxResponses: true,
                ct: ct));

        /// <summary>
        /// 取得 2D 紋理（支援取消權杖機制）
        /// </summary>
        public new UniTask<Texture2D> GetTex2D(string url, bool isAbsoluteUrl, IProgress<float> progress, CancellationToken ct = default)
        {
            return base.GetTex2D(url, isAbsoluteUrl, progress, ct);
        }

        #region 專案與驗證 API (Project & Verification)

        /// <summary>
        /// 檢查 API 服務健康狀態
        /// </summary>
        public async UniTask CheckApiHealthAsync(CancellationToken ct = default)
        {
            try
            {
                await Get("/api/health", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiProjectException(ApiAction.CheckApiHealth, "API service health check failed.", ex);
            }
            catch (OperationCanceledException) { throw; }
        }

        /// <summary>
        /// 驗證目前的 API Key 是否有效並取得專案資訊 (對齊新 API 路由規範)
        /// </summary>
        public async UniTask<Api.VerifyProjectInfo> GetApiKeyValidInfoAsync(CancellationToken ct = default)
        {
            try
            {
                return await Get<Api.VerifyProjectInfo>("/api/projects/verify", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiProjectException(ApiAction.GetApiKeyValidInfo, "Failed to verify API Key or retrieve project verification info. Message: " + ex.Message, ex);
            }
            catch (OperationCanceledException) { throw; }
        }

        /// <summary>
        /// 取得指定專案的詳細資料
        /// </summary>
        public async UniTask<Api.Project> GetProjectAsync(Guid projectId, CancellationToken ct = default)
        {
            try
            {
                return await Get<Api.Project>($"/api/projects/{projectId}", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiProjectException(ApiAction.GetProject, $"Failed to get project details for project ID: {projectId}, Message: {ex.ResponseText}", ex, projectId: projectId.ToString());
            }
            catch (OperationCanceledException) { throw; }
        }

        #endregion

        #region 學習歷程紀錄 API (Learning Portfolio Sheets)

        public async UniTask<List<string>> FindSheetsAsync(string projectId, CancellationToken ct = default)
        {
            try
            {
                return await Get<List<string>>($"/api/projects/{projectId}/me/sheets", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiSheetException(ApiAction.FindSheets, null, $"Failed to find sheets for project: {projectId}", ex);
            }
            catch (OperationCanceledException) { throw; }
        }

        public async UniTask<Api.Sheet> GetSheetAsync(string sheetId, CancellationToken ct = default)
        {
            try
            {
                return await Get<Api.Sheet>($"/api/sheets/{sheetId}", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiSheetException(ApiAction.GetSheet, sheetId, $"Failed to get sheet: {sheetId}", ex);
            }
            catch (OperationCanceledException) { throw; }
        }

        public async UniTask<Api.Page> GetPageAsync(string sheetId, int page, CancellationToken ct = default)
        {
            try
            {
                return await Get<Api.Page>($"/api/sheets/{sheetId}/{page}", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiSheetException(ApiAction.GetPage, sheetId, $"Failed to get page {page}", ex, page: page);
            }
            catch (OperationCanceledException) { throw; }
        }

        public async UniTask<Api.Column> GetPageColumnAsync(string sheetId, int page, int column, CancellationToken ct = default)
        {
            try
            {
                return await Get<Api.Column>($"/api/sheets/{sheetId}/{page}/{column}", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiSheetException(ApiAction.GetPageColumn, sheetId, $"Failed to get column {column} on page {page}", ex, page: page, column: column);
            }
            catch (OperationCanceledException) { throw; }
        }

        public async UniTask<Api.Column[]> GetPageColumnsAsync(string sheetId, int page, CancellationToken ct = default)
        {
            try
            {
                return await Get<Api.Column[]>($"/api/sheets/{sheetId}/{page}/columns", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiSheetException(ApiAction.GetPageColumns, sheetId, $"Failed to get columns on page {page}", ex, page: page);
            }
            catch (OperationCanceledException) { throw; }
        }

        public async UniTask<Api.ColumnSummary> GetPageColumnSummaryAsync(string sheetId, int page, int column, CancellationToken ct = default)
        {
            try
            {
                return await Get<Api.ColumnSummary>($"/api/sheets/{sheetId}/{page}/{column}/summary", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiSheetException(ApiAction.GetPageColumnSummary, sheetId, $"Failed to get column {column} summary on page {page}", ex, page: page, column: column);
            }
            catch (OperationCanceledException) { throw; }
        }

        public async UniTask<Api.ColumnSummary[]> GetPageColumnsSummaryAsync(string sheetId, int page, CancellationToken ct = default)
        {
            try
            {
                return await Get<Api.ColumnSummary[]>($"/api/sheets/{sheetId}/{page}/columns/summary", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiSheetException(ApiAction.GetPageColumnsSummary, sheetId, $"Failed to get columns summary on page {page}", ex, page: page);
            }
            catch (OperationCanceledException) { throw; }
        }

#pragma warning disable CS0618 // 類型或成員已經過時
        [Obsolete("已棄用，欄位型別已改由後台管理，不應再呼叫此 API。")]
        public async UniTask SetPageColumnAsync(string sheetId, int page, int column, Api.SetColumnRequest request, CancellationToken ct = default)
        {
            try
            {
                await Put($"/api/sheets/{sheetId}/{page}/{column}", request, ct);
            }
            catch (ApiException ex)
            {
                throw new ApiSheetException(ApiAction.SetPageColumn, sheetId, $"Failed to set column {column} on page {page}", ex, page: page, column: column);
            }
            catch (OperationCanceledException) { throw; }
        }
#pragma warning restore CS0618 // 類型或成員已經過時

        public async UniTask<List<Api.Row>> GetPageRowsAsync(string sheetId, int page, int start, int count, CancellationToken ct = default)
        {
            try
            {
                return await Get<List<Api.Row>>($"/api/sheets/{sheetId}/{page}/rows?start={start}&count={count}", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiSheetException(ApiAction.GetPageRows, sheetId, $"Failed to get rows on page {page}", ex, page: page, start: start, count: count);
            }
            catch (OperationCanceledException) { throw; }
        }

        public async UniTask SetPageRowAsync(string sheetId, int page, int row, Api.SetRowRequest request, CancellationToken ct = default)
        {
            try
            {
                await Put($"/api/sheets/{sheetId}/{page}/rows/{row}", request, ct);
            }
            catch (ApiException ex)
            {
                throw new ApiSheetException(ApiAction.SetPageRow, sheetId, $"Failed to set row {row} on page {page}", ex, page: page, row: row);
            }
            catch (OperationCanceledException) { throw; }
        }

        public async UniTask<Api.AddRowResponse> AddPageRowAsync(string sheetId, int page, CancellationToken ct = default)
        {
            try
            {
                return await Get<Api.AddRowResponse>($"/api/sheets/{sheetId}/{page}/rows/add", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiSheetException(ApiAction.AddPageRow, sheetId, $"Failed to add row on page {page}", ex, page: page);
            }
            catch (OperationCanceledException) { throw; }
        }

        public async UniTask ClearPageReadableDataAsync(string sheetId, int page, CancellationToken ct = default)
        {
            try
            {
                await Get($"/api/sheets/{sheetId}/{page}/clear", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiSheetException(ApiAction.ClearPageReadableData, sheetId, $"Failed to clear page {page} data", ex, page: page);
            }
            catch (OperationCanceledException) { throw; }
        }

        public async UniTask SetCompleteProgressAsync(string sheetId, string path, CancellationToken ct = default)
        {
            try
            {
                await Get($"/api/sheets/{sheetId}/progress-completion/complete/{path}", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiSheetException(ApiAction.SetCompleteProgress, sheetId, $"Failed to set complete progress for path: {path}", ex, path: path);
            }
            catch (OperationCanceledException) { throw; }
        }

        public async UniTask SetUnmarkProgressAsync(string sheetId, string path, CancellationToken ct = default)
        {
            try
            {
                await Get($"/api/sheets/{sheetId}/progress-completion/unmark/{path}", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiSheetException(ApiAction.SetUnmarkProgress, sheetId, $"Failed to unmark progress for path: {path}", ex, path: path);
            }
            catch (OperationCanceledException) { throw; }
        }

        public async UniTask<float> GetProgressCompletionAsync(string sheetId, CancellationToken ct = default)
        {
            try
            {
                return await Get<float>($"/api/sheets/{sheetId}/progress-completion", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiSheetException(ApiAction.GetProgressCompletion, sheetId, "Failed to get progress completion.", ex);
            }
            catch (OperationCanceledException) { throw; }
        }

        #endregion

        #region 使用紀錄 API (Usage Records)

        public async UniTask<Api.ProjectUsageRecordResponse> CreateProjectUsageRecordAsync(string projectId, Api.SetProjectUsageRecordRequest request, CancellationToken ct = default)
        {
            try
            {
                return await Post<Api.ProjectUsageRecordResponse>($"/api/projects/{projectId}/me/usage", request, ct);
            }
            catch (ApiException ex)
            {
                throw new ApiUsageException(ApiAction.CreateProjectUsageRecord, $"Failed to create project usage record for project: {projectId}", ex, targetId: projectId);
            }
            catch (OperationCanceledException) { throw; }
        }

        public async UniTask<Api.HeartbeatResponse> ProjectUsageRecordHeartbeatAsync(int trackingId, CancellationToken ct = default)
        {
            try
            {
                var body = new Api.HeartbeatRequest { trackingId = trackingId };
                return await Post<Api.HeartbeatResponse>($"/api/usage/{trackingId}/heartbeat", body, ct);
            }
            catch (ApiException ex)
            {
                throw new ApiUsageException(ApiAction.ProjectUsageRecordHeartbeat, $"Failed to send heartbeat for tracking ID: {trackingId}", ex, targetId: trackingId.ToString());
            }
            catch (OperationCanceledException) { throw; }
        }

        #endregion

        #region 排行榜 API (Leaderboards)

        /// <summary>
        /// 取得該專案下，指定組織內的使用者排名
        /// </summary>
        public async UniTask<Api.UserRankingResult> GetProjectUserRankingAsync(string projectId, string orgId, int start, int count, CancellationToken ct = default)
        {
            try
            {
                return await Get<Api.UserRankingResult>($"/api/projects/{projectId}/organizations/{orgId}/rankings?start={start}&count={count}", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiLeaderboardException(ApiAction.GetProjectUserRanking, projectId, $"Failed to get user rankings for project: {projectId}, organization: {orgId} (start: {start}, count: {count})", ex, orgId: orgId);
            }
            catch (OperationCanceledException) { throw; }
        }

        /// <summary>
        /// 取得該專案下的所有組織排名
        /// </summary>
        public async UniTask<Api.OrgRankingResult> GetProjectOrgRankingAsync(string projectId, int start, int count, CancellationToken ct = default)
        {
            try
            {
                return await Get<Api.OrgRankingResult>($"/api/projects/{projectId}/organizations/rankings?start={start}&count={count}", ct);
            }
            catch (ApiException ex)
            {
                throw new ApiLeaderboardException(ApiAction.GetProjectOrgRanking, projectId, $"Failed to get organization rankings for project: {projectId} (start: {start}, count: {count})", ex);
            }
            catch (OperationCanceledException) { throw; }
        }

        #endregion

    }

    #region Exceptions
    /// <summary>
    /// 學習歷程 API 的最高基底 Exception
    /// </summary>
    public abstract class LearningPortfolioApiException : Exception
    {
        public ApiAction Action { get; protected set; }

        public ApiException SourceApiEx { get; protected set; }

        public override string Message => SourceApiEx.Message;

        protected LearningPortfolioApiException(ApiAction action, string message, ApiException sourceApiEx = null)
            : base(message, sourceApiEx)
        {
            Action = action;
            SourceApiEx = sourceApiEx;
        }
    }

    // ==========================================
    // 1. 專案與驗證模組 Exception
    // ==========================================
    public class ApiProjectException : LearningPortfolioApiException
    {
        public string ProjectId { get; private set; }

        public ApiProjectException(ApiAction action, string message, ApiException innerException = null, string projectId = null)
            : base(action, message, innerException)
        {
            ProjectId = projectId;
        }
    }

    // ==========================================
    // 2. 學習歷程紀錄（試算表）模組 Exception
    // ==========================================
    public class ApiSheetException : LearningPortfolioApiException
    {
        public string SheetId { get; private set; }
        public int? Page { get; private set; }
        public int? Column { get; private set; }
        public int? Row { get; private set; }
        public int? Start { get; private set; }
        public int? Count { get; private set; }
        public string Path { get; private set; }

        public ApiSheetException(
            ApiAction action,
            string sheetId,
            string message,
            ApiException innerException = null,
            int? page = null,
            int? column = null,
            int? row = null,
            int? start = null,
            int? count = null,
            string path = null)
            : base(action, message, innerException)
        {
            SheetId = sheetId;
            Page = page;
            Column = column;
            Row = row;
            Start = start;
            Count = count;
            Path = path;
        }
    }

    // ==========================================
    // 3. 使用紀錄模組 Exception
    // ==========================================
    public class ApiUsageException : LearningPortfolioApiException
    {
        public string TargetId { get; private set; } // 可彈性存放 ProjectId 或 TrackingId

        public ApiUsageException(ApiAction action, string message, ApiException innerException = null, string targetId = null)
            : base(action, message, innerException)
        {
            TargetId = targetId;
        }
    }

    // ==========================================
    // 4. 排行榜模組 Exception
    // ==========================================
    public class ApiLeaderboardException : LearningPortfolioApiException
    {
        public string ProjectId { get; private set; }
        public string OrgId { get; private set; }

        public ApiLeaderboardException(ApiAction action, string projectId, string message, ApiException innerException = null, string orgId = null)
            : base(action, message, innerException)
        {
            ProjectId = projectId;
            OrgId = orgId;
        }
    }
    #endregion
}