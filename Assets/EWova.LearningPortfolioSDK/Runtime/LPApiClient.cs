using Cysharp.Threading.Tasks;

using EWova.Auth;
using EWova.Networking;

using System;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;

namespace EWova.LearningPortfolio
{
    /// <summary>
    /// 學習履歷 API 客戶端，負責與後端服務進行通信，處理認證和請求頭的設置。
    /// </summary>
    public partial class LPApiClient : AuthApiClient
    {
        public static string LPServiceUrl
        {
            get
            {
                if (Environment.DeploymentMode is DeploymentMode.Development)
                {
                    return "https://api-learning-app.ewova.dev/";
                }
                else
                {
#if UNITY_EDITOR
                    Authoring.EditorLogger.Warn("你正在編輯器中使用正式環境的 API URL，請確認是否有意這麼做，避免對正式環境造成不必要的影響。可到 EWova/Editor/DeveloymentMode 切換回 Development 開發環境。");
#endif
                    return "https://api-learning-app.ewova.com/";
                }
            }
        }

        protected LearningPortfolioEWovaAuth CurrentAuth;
        protected ProjectSettings CurrentProjectSettings;

        internal LPApiClient(LearningPortfolioEWovaAuth auth, Logger logger = null)
            : base(auth, LPServiceUrl, logger)
        {
            if(auth.IsProjectSettingsValid == false)
                throw new ArgumentException("Invalid ProjectSettings in LearningPortfolioEWovaAuth. Please ensure that the ProjectSettings are valid before creating an instance of LPApiClient.");

            CurrentAuth = auth;
            // Set the API key in the headers for authentication
            AdditionalHeaders["x-api-key"] = CurrentProjectSettings.APIKey;
        }

        protected override void CollectPackages(List<SdkPackageInfo> list)
        {
            base.CollectPackages(list);

            list.Add(new SdkPackageInfo
            {
                Name = PackageInfo.Name,
                Version = PackageInfo.Version
            });
        }

        public TimeSpan HeartbeatIntervalSeconds { get; set; } = TimeSpan.FromSeconds(2);

        protected override async UniTask OnDisposeAsync()
        {
            _projectUsageRecordCts?.Cancel();

            if (_heartbeatProcessCompletionSource != null)
            {
                try
                {
                    await _heartbeatProcessCompletionSource.Task;
                }
                catch { }
            }
        }

        private CancellationTokenSource _projectUsageRecordCts;
        private UniTaskCompletionSource _heartbeatProcessCompletionSource;

        public UniTask KeepLoginUsageRecordHeartbeatAsyncProcess(
                int projectUsageRecordTrackingId,
                CancellationToken ct = default)
        {
            // 如果已經在執行，直接返回現有的 Task
            if (_heartbeatProcessCompletionSource != null)
                return _heartbeatProcessCompletionSource.Task;

            if (_logger.InfoEnabled)
                _logger.Info("Starting send Heartbeat");

            _heartbeatProcessCompletionSource = new UniTaskCompletionSource();
            _projectUsageRecordCts = new CancellationTokenSource();

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _projectUsageRecordCts.Token);

            HeartbeatLoopAsync(projectUsageRecordTrackingId, linkedCts).Forget();

            return _heartbeatProcessCompletionSource.Task;
        }
        public void StopLoginUsageRecordHeartbeat()
        {
            if (_projectUsageRecordCts != null)
            {
                _projectUsageRecordCts.Cancel();
                _heartbeatProcessCompletionSource = null;
                if (_logger.InfoEnabled)
                    _logger.Info("Requested to stop Heartbeat");
            }
        }

        private async UniTaskVoid HeartbeatLoopAsync(int trackingId, CancellationTokenSource linkedCts)
        {
            var token = linkedCts.Token;
            Exception exceptionToReport = null;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await UniTask.Delay(HeartbeatIntervalSeconds, ignoreTimeScale: true, cancellationToken: token);

                    var resp = await ProjectUsageRecordHeartbeatAsync(trackingId, token);
                    if (!resp.Success)
                    {
                        throw new ApiUsageException(ApiAction.ProjectUsageRecordHeartbeat, $"Heartbeat failed for tracking ID: {trackingId}.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，不視為異常
            }
            catch (Exception ex)
            {
                exceptionToReport = ex;
            }
            finally
            {
                if (_logger.InfoEnabled)
                    _logger.Info("傳送最後一次 Heartbeat，結束使用紀錄");

                try
                {
                    if (_auth.IsAuthenticated)
                    {
                        // 最後一次心跳不帶入已取消的 token
                        await ProjectUsageRecordHeartbeatAsync(trackingId, CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    if (_logger.InfoEnabled)
                        _logger.Info("Failed to send final Heartbeat: " + ex);
                }

                if (_logger.InfoEnabled)
                    _logger.Info("Stopped send Heartbeat");

                linkedCts.Dispose();

                _projectUsageRecordCts?.Dispose();
                _projectUsageRecordCts = null;

                var utcs = _heartbeatProcessCompletionSource;
                _heartbeatProcessCompletionSource = null;

                if (exceptionToReport != null)
                    utcs?.TrySetException(exceptionToReport);
                else
                    utcs?.TrySetResult();
            }
        }
    }
}