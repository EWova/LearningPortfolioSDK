using System.Collections.Generic;
using System.Threading;
using System;
using System.Net;

using UnityEngine;
using UnityEngine.Networking;

using Proyecto26;

using Cysharp.Threading.Tasks;

using Newtonsoft.Json;

using EWova.NetService;

namespace EWova.LearningPortfolio
{
    public partial class LearningPortfolio
    {
        public static Debug HttpDebugger = new("[EWova/LearningPortfolio] ");
        public static async UniTask<string> Get(string token, string url, CancellationToken cancellationToken = default, Debug debugger = null)
        {
            var req = new RequestHelper
            {
                Uri = url,
                Headers = new Dictionary<string, string> { { "Authorization", token } },
                Method = "GET",
            };

            debugger.Log($"[{req.Method}] RequestAsync Uri:{req.Uri}");

            ResponseHelper rsp;
            try
            {
                rsp = await RestClient.Request(req).AsUniTask(cancellationToken);
            }
            catch (Exception ex)
            {
                bool isErrorHandled = false;
                if (ex is RequestException badReq)
                    ex = DeserializeErrorObject<ErrorHandle>(badReq.Response, url, out isErrorHandled);

                if (isErrorHandled)
                    debugger.LogWarning($"[{req.Method}] Uri:{req.Uri} RequestAsync failed: {ex.Message}");
                else
                    debugger.LogError($"[{req.Method}] Uri:{req.Uri} RequestAsync failed: {ex.Message}");

                throw;
            }

            debugger.Log($"[{req.Method}] Response Uri:{req.Uri} ChartContent:{rsp.Text}");

            return rsp.Text;
        }
        public static async UniTask<T> Get<T>(string token, string url, CancellationToken cancellationToken = default, Debug debugger = null)
        {
            var req = new RequestHelper
            {
                Uri = url,
                Headers = new Dictionary<string, string> { { "Authorization", token } },
                Method = "GET",
            };

            debugger?.Log($"[{req.Method}] RequestAsync Uri:{req.Uri}");

            ResponseHelper rsp;
            try
            {
                rsp = await RestClient.Request(req).AsUniTask(cancellationToken);
            }
            catch (Exception ex)
            {
                bool isErrorHandled = false;

                if (ex is RequestException badReq)
                    ex = DeserializeErrorObject<ErrorHandle>(badReq.Response, url, out isErrorHandled);

                if (isErrorHandled)
                    debugger.LogWarning($"[{req.Method}] Uri:{req.Uri} RequestAsync failed: {ex.Message}");
                else
                    debugger.LogError($"[{req.Method}] Uri:{req.Uri} RequestAsync failed: {ex.Message}");

                throw;
            }

            T typed = DeserializeObject<T>(rsp.Text);

            debugger?.Log($"[{req.Method}] Response Uri:{req.Uri} ChartContent({typeof(T)}):{rsp.Text}");
            return typed;
        }
        public static async UniTask<string> Post(string token, string url, object body, CancellationToken cancellationToken = default, Debug debugger = null)
        {
            var req = new RequestHelper
            {
                Uri = url,
                Headers = new Dictionary<string, string> { { "Authorization", token } },
                Method = "POST",
                BodyString = SerializeObject(body)
            };

            debugger?.Log($"[{req.Method}] RequestAsync Uri:{req.Uri} ChartContent:{req.BodyString}");

            ResponseHelper rsp;
            try
            {
                rsp = await RestClient.Request(req).AsUniTask(cancellationToken);
            }
            catch (Exception ex)
            {
                bool isErrorHandled = false;

                if (ex is RequestException badReq)
                    ex = DeserializeErrorObject<ErrorHandle>(badReq.Response, url, out isErrorHandled);

                if (isErrorHandled)
                    debugger.LogWarning($"[{req.Method}] Uri:{req.Uri} RequestAsync failed: {ex.Message}");
                else
                    debugger.LogError($"[{req.Method}] Uri:{req.Uri} RequestAsync failed: {ex.Message}");

                throw;
            }

            debugger?.Log($"[{req.Method}] Response Uri:{req.Uri} ChartContent:{rsp.Text}");

            return rsp.Text;
        }
        public static async UniTask<T> Post<T>(string token, string url, object body, CancellationToken cancellationToken = default, Debug debugger = null)
        {
            var req = new RequestHelper
            {
                Uri = url,
                Headers = new Dictionary<string, string> { { "Authorization", token } },
                Method = "POST",
                BodyString = SerializeObject(body)
            };

            debugger?.Log($"[{req.Method}] RequestAsync Uri:{req.Uri} ChartContent:{req.BodyString}");

            ResponseHelper rsp;
            try
            {
                rsp = await RestClient.Request(req).AsUniTask(cancellationToken);
            }
            catch (Exception ex)
            {
                bool isErrorHandled = false;

                if (ex is RequestException badReq)
                    ex = DeserializeErrorObject<ErrorHandle>(badReq.Response, url, out isErrorHandled);

                if (isErrorHandled)
                    debugger.LogWarning($"[{req.Method}] Uri:{req.Uri} RequestAsync failed: {ex.Message}");
                else
                    debugger.LogError($"[{req.Method}] Uri:{req.Uri} RequestAsync failed: {ex.Message}");

                throw;
            }

            T typed = DeserializeObject<T>(rsp.Text);

            debugger?.Log($"[{req.Method}] Response Uri:{req.Uri} ChartContent({typeof(T)}):{rsp.Text}");
            return typed;
        }
        public static async UniTask<string> Put(string token, string url, object body, CancellationToken cancellationToken = default, Debug debugger = null)
        {
            var req = new RequestHelper
            {
                Uri = url,
                Headers = new Dictionary<string, string> { { "Authorization", token } },
                Method = "PUT",
                BodyString = SerializeObject(body)
            };

            debugger?.Log($"[{req.Method}] RequestAsync Uri:{req.Uri} ChartContent:{req.BodyString}");

            ResponseHelper rsp;
            try
            {
                rsp = await RestClient.Request(req).AsUniTask(cancellationToken);
            }
            catch (Exception ex)
            {
                bool isErrorHandled = false;

                if (ex is RequestException badReq)
                    ex = DeserializeErrorObject<ErrorHandle>(badReq.Response, url, out isErrorHandled);

                if (isErrorHandled)
                    debugger.LogWarning($"[{req.Method}] Uri:{req.Uri} RequestAsync failed: {ex.Message}");
                else
                    debugger.LogError($"[{req.Method}] Uri:{req.Uri} RequestAsync failed: {ex.Message}");

                throw;
            }

            debugger?.Log($"[{req.Method}] Response Uri:{req.Uri} ChartContent:{rsp.Text}");

            return rsp.Text;
        }
        public static async UniTask<T> Put<T>(string token, string url, object body, CancellationToken cancellationToken = default, Debug debugger = null)
        {
            var req = new RequestHelper
            {
                Uri = url,
                Headers = new Dictionary<string, string> { { "Authorization", token } },
                Method = "PUT",
                BodyString = SerializeObject(body)
            };

            debugger?.Log($"[{req.Method}] RequestAsync Uri:{req.Uri} ChartContent:{req.BodyString}");

            ResponseHelper rsp;
            try
            {
                rsp = await RestClient.Request(req).AsUniTask(cancellationToken);
            }
            catch (Exception ex)
            {
                bool isErrorHandled = false;

                if (ex is RequestException badReq)
                    ex = DeserializeErrorObject<ErrorHandle>(badReq.Response, url, out isErrorHandled);

                if (!isErrorHandled)
                    debugger.LogError($"[{req.Method}] Uri:{req.Uri} ChartContent:{req.BodyString} RequestAsync failed: {ex.Message}");

                throw;
            }

            T typed = DeserializeObject<T>(rsp.Text);

            debugger?.Log($"[{req.Method}] Response Uri:{req.Uri} ChartContent({typeof(T)}):{rsp.Text}");
            return typed;
        }

        public static async UniTask<Texture2D> GetTex2D(string token, string url, CancellationToken cancellationToken = default, Debug debugger = null)
        {
            var req = new RequestHelper
            {
                Uri = url,
                Method = "GET",
                Headers = new Dictionary<string, string> { { "Authorization", token } },
                Request = new UnityWebRequest(url),
            };

#if UNITY_6000_0_OR_NEWER
            req.DownloadHandler = new DownloadHandlerTexture();
#else
            req.DownloadHandler = new DownloadHandlerBuffer();
#endif

            debugger?.Log($"[{req.Method}] Tex2D RequestAsync Uri:{req.Uri}");

            ResponseHelper rsp;
            try
            {
                rsp = await RestClient.Request(req).AsUniTask(cancellationToken);
            }
            catch (Exception ex)
            {
                bool isErrorHandled = false;

                if (ex is RequestException badReq)
                    ex = DeserializeErrorObject<ErrorHandle>(badReq.Response, url, out isErrorHandled);

                if (!isErrorHandled)
                    debugger.LogError($"[{req.Method}] Tex2D Uri:{req.Uri} RequestAsync failed: {ex.Message}");

                throw;
            }
            Texture2D tex;

#if UNITY_6000_0_OR_NEWER
            DownloadHandlerTexture dhTex = rsp.Request.downloadHandler as DownloadHandlerTexture;
            tex = dhTex.texture;
#else
            DownloadHandlerBuffer dhBuf = rsp.Request.downloadHandler as DownloadHandlerBuffer;
            tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true, linear: false);
            tex.LoadImage(dhBuf.data, markNonReadable: true);
#endif

            debugger?.Log($"[{req.Method}] Tex2D Response Uri:{req.Uri} {tex.width}x{tex.height}");
            return tex;
        }

        private static string SerializeObject<T>(T obj)
        {
            if (obj == null)
                return "{}";
            return JsonConvert.SerializeObject(obj);
        }
        private static T DeserializeObject<T>(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return default;

            try
            {
                return JsonConvert.DeserializeObject<T>(text);
            }
            catch
            {
                return default;
            }
        }
        private static Exception DeserializeErrorObject<T>(string text, string endpoint, out bool IsErrorHandled)
        {
            ErrorHandle result = DeserializeObject<ErrorHandle>(text);
            result.Endpoint = endpoint;
            if (result != null)
            {
                IsErrorHandled = true;
                return new ErrorHandleException(result);
            }
            else
            {
                IsErrorHandled = false;
                return new Exception($"Failed to parse response to {typeof(T)} and also to ErrorHandle. Raw response: {text}");
            }
        }

        public static UniTask<string> Get(string path) => Get(CurrentAPISettings.APIKey, CurrentAPISettings.ServiceUrl + path, debugger: HttpDebugger);
        public static UniTask<T> Get<T>(string path) => Get<T>(CurrentAPISettings.APIKey, CurrentAPISettings.ServiceUrl + path, debugger: HttpDebugger);
        public static UniTask<string> Post(string path, object body) => Post(CurrentAPISettings.APIKey, CurrentAPISettings.ServiceUrl + path, body, debugger: HttpDebugger);
        public static UniTask<T> Post<T>(string path, object body) => Post<T>(CurrentAPISettings.APIKey, CurrentAPISettings.ServiceUrl + path, body, debugger: HttpDebugger);
        public static UniTask<string> Put(string path, object body) => Put(CurrentAPISettings.APIKey, CurrentAPISettings.ServiceUrl + path, body, debugger: HttpDebugger);
        public static UniTask<T> Put<T>(string path, object body) => Put<T>(CurrentAPISettings.APIKey, CurrentAPISettings.ServiceUrl + path, body, debugger: HttpDebugger);
        public static UniTask<Texture2D> GetTex2D(string url) => GetTex2D(CurrentAPISettings.APIKey, url, debugger: HttpDebugger);

        #region 公開用API
        public static void GetProject(string address, string apiKey, Guid projectId, Action<API.Project> onSuccess, Action<Exception> onError = null)
        {
            string url = address + $"/projects/{projectId}";

#if UNITY_EDITOR //RestClient 不支援 Editor 下的使用
            if (!Application.isPlaying)
            {
                UniTask.Void(async () =>
                {
                    using var req = UnityWebRequest.Get(url);
                    req.SetRequestHeader("Authorization", apiKey);
                    try
                    {
                        var res = await req.SendWebRequest();
                        if (res.result == UnityWebRequest.Result.Success)
                        {
                            API.Project response = JsonConvert.DeserializeObject<API.Project>(res.downloadHandler.text);
                            onSuccess?.Invoke(response);
                        }
                        else
                        {
                            onError?.Invoke(new Exception(res.error));
                        }
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke(e);
                    }
                });
                return;
            }
#endif
            UniTask.Void(async () =>
            {
                try
                {
                    API.Project response = await Get<API.Project>(apiKey, url);
                    onSuccess?.Invoke(response);
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex);
                }
            });
        }
        public void GetProject(Guid projectId, Action<API.Project> onSuccess, Action<Exception> onError = null)
        {
            GetProject(
                address: CurrentAPISettings.ServiceUrl
                , apiKey: CurrentAPISettings.APIKey
                , projectId: projectId
                , onSuccess: onSuccess
                , onError: onError);
        }
        #endregion

        #region 專案開發
        private static Exception GetException(long responseCode, Exception source)
        {
            var code = (HttpStatusCode)responseCode;

            switch (code)
            {
                case HttpStatusCode.Unauthorized: //401
                case HttpStatusCode.Forbidden: //403
                    return new Exception("驗證失敗，請確認 Token");
                case HttpStatusCode.NotFound: //404
                    return new Exception("找不到驗證伺服器，請更新到最新版本");
                case HttpStatusCode.InternalServerError: //500
                case HttpStatusCode.BadGateway: //502
                case HttpStatusCode.ServiceUnavailable: //503
                    return new Exception("伺服器錯誤，請稍後再試");
                default:
                    return source;
            }
        }
        public static void GetApiKeyValidInfo(string address, string apiKey, Action<API.VerifyProjectInfo> onSuccess, Action<Exception> onError = null)
        {
            string url = address + "/projects/verify";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                onError?.Invoke(new Exception("API Key 為空"));
                return;
            }

#if UNITY_EDITOR //RestClient 不支援 Editor 下的使用
            if (!Application.isPlaying)
            {
                UniTask.Void(async () =>
                {
                    using var req = UnityWebRequest.Get(url);
                    req.SetRequestHeader("Authorization", apiKey);
                    UnityWebRequest res;
                    try
                    {
                        res = await req.SendWebRequest();
                        if (res.result == UnityWebRequest.Result.Success)
                        {
                            API.VerifyProjectInfo response = JsonConvert.DeserializeObject<API.VerifyProjectInfo>(res.downloadHandler.text);
                            onSuccess?.Invoke(response);
                        }
                        else
                        {
                            onError?.Invoke(new Exception(res.error));
                        }
                    }
                    catch (UnityWebRequestException e)
                    {
                        onError?.Invoke(GetException(e.ResponseCode, e));
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke(e);
                    }
                });
                return;
            }
#endif
            UniTask.Void(async () =>
            {
                try
                {
                    API.VerifyProjectInfo response = await Get<API.VerifyProjectInfo>(apiKey, url);
                    onSuccess?.Invoke(response);
                }
                catch (RequestException e)
                {
                    onError?.Invoke(GetException(e.StatusCode, e));
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                    onError?.Invoke(ex);
                }
            });
        }
        public void GetApiKeyValidInfo(Action<API.VerifyProjectInfo> onSuccess, Action<Exception> onError = null)
        {
            GetApiKeyValidInfo(
                address: CurrentAPISettings.ServiceUrl
                , apiKey: CurrentAPISettings.APIKey
                , onSuccess: onSuccess
                , onError: onError);
        }
        #endregion

        #region 歷程紀錄
        public static UniTask<List<string>> FindSheetsAsync(string projectId, string userId)
            => Get<List<string>>($"/projects/{projectId}/users/{userId}/sheets");
        public static UniTask<API.Sheet> GetSheetAsync(string sheetId)
            => Get<API.Sheet>($"/sheets/{sheetId}");
        public static UniTask<API.Page> GetPageAsync(string sheetId, int page)
            => Get<API.Page>($"/sheets/{sheetId}/{page}");
        public static UniTask<API.Column> GetPageColumnAsync(string sheetId, int page, int column)
            => Get<API.Column>($"/sheets/{sheetId}/{page}/{column}");
        public static UniTask<API.Column[]> GetPageColumnsAsync(string sheetId, int page)
            => Get<API.Column[]>($"/sheets/{sheetId}/{page}/columns");
        public static UniTask<API.ColumnSummary> GetPageColumnSummaryAsync(string sheetId, int page, int column)
            => Get<API.ColumnSummary>($"/sheets/{sheetId}/{page}/{column}/summary");
        public static UniTask<API.ColumnSummary[]> GetPageColumnsSummaryAsync(string sheetId, int page)
            => Get<API.ColumnSummary[]>($"/sheets/{sheetId}/{page}/columns/summary");
        public static UniTask SetPageColumnAsync(string sheetId, int page, int column, API.SetColumnRequest request)
            => Put($"/sheets/{sheetId}/{page}/{column}", request);

        public static UniTask<List<API.Row>> GetPageRowsAsync(string sheetId, int page, int start, int count)
            => Get<List<API.Row>>($"/sheets/{sheetId}/{page}/rows?start={start}&count={count}");
        public static UniTask SetPageRowAsync(string sheetId, int page, int row, API.SetRowRequest request)
            => Put($"/sheets/{sheetId}/{page}/rows/{row}", request);
        public static UniTask<API.AddRowResponse> AddPageRowAsync(string sheetId, int page)
            => Get<API.AddRowResponse>($"/sheets/{sheetId}/{page}/rows/add");

        public static UniTask ClearPageReadableDataAsync(string sheetId, int page)
            => Get($"/sheets/{sheetId}/{page}/clear");

        public static UniTask SetCompleteProgress(string sheetId, string path)
            => Get($"/sheets/{sheetId}/progress-completion/complete/{path}");
        public static UniTask SetUnmarkProgress(string sheetId, string path)
            => Get($"/sheets/{sheetId}/progress-completion/unmark/{path}");
        public static UniTask<float> GetProgressCompletion(string sheetId)
            => Get<float>($"/sheets/{sheetId}/progress-completion");
        #endregion

        #region 使用紀錄
        public static UniTask<API.ProjectUsageRecordResponse> CreateProjectUsageRecord(string projectId, string userId, API.SetProjectUsageRecordRequest request)
            => Post<API.ProjectUsageRecordResponse>($"/projects/{projectId}/users/{userId}/usage", request);
        public static UniTask ProjectUsageRecordHeartbeat(int trackingId)
            => Post($"/usage/{trackingId}/heartbeat", trackingId);
        #endregion

        #region 排行榜
        // 這個專案下 目標組織的組織排名
        public static UniTask<API.UserRankingResult> GetProjectUserRanking(string projectId, string orgId, int start, int count)
            => Get<API.UserRankingResult>($"/projects/{projectId}/organizations/{orgId}/rankings?start={start}&count={count}");

        // 這個專案下 目標組織的使用者排名
        public static UniTask<API.OrgRankingResult> GetProjectOrgRanking(string projectId, int start, int count)
            => Get<API.OrgRankingResult>($"/projects/{projectId}/organizations/rankings?start={start}&count={count}");
        #endregion
    }
}