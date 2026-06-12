/*******************************************************************************
 * Copyright (c) 2025 EWova
 * 專案名稱: 學習歷程系統 LearningPortfolio
 *
 * 授權聲明:
 * 本程式碼及相關文件可免費使用、修改及分享，僅限於非商業用途。
 * 禁止任何透過本程式碼或其衍生品進行營利行為，或作為商業產品的一部分。
 * 使用者在傳播、學習或教育用途上，無需額外許可。
 *
 * 如欲進行商業用途，請聯絡 EWova.com 以取得授權。
 ******************************************************************************/

using Cysharp.Threading.Tasks;

using EWova.Auth;
using EWova.Networking;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using UnityEngine;

namespace EWova.LearningPortfolio
{
    [Serializable]
    public struct ApiSettings
    {
        public ApiSettings(string apiKey)
        {
            APIKey = apiKey;
        }
        public string APIKey;

        public readonly void EnsureValid()
        {
            if (!IsValid(out string msg))
                throw new ArgumentException(msg, nameof(APIKey));
        }
        public readonly bool IsValid(out string errorMessage)
        {
            if (string.IsNullOrEmpty(APIKey))
            {
                errorMessage = "API Key cannot be null or empty.";
                return false;
            }
            errorMessage = null;
            return true;
        }
    }

    public partial class LearningPortfolio : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Init()
        {
            s_instance = null;
            s_loadedProfile = null;
            OnUserLogin = null;
            OnUserLogout = null;
            OnUserProjectRecordUpdated = null;
        }

        public static readonly string Name = "[EWova]LearningPortfolio";
        private static readonly Logger Logger = new(Name + ' ', LogLevel.Full);
        private static readonly Logger ApiClientLogger = new(Name + "-ApiClient ", LogLevel.Warn | LogLevel.Error);
        /// <summary>
        /// 學習歷程用的日誌等級
        /// </summary>
        public static LogLevel LoggerLevel
        {
            get => Logger.PrintLevel;
            set => Logger.PrintLevel = value;
        }
        /// <summary>
        /// 學習歷程 API 請求相關的日誌等級
        /// </summary>
        public static LogLevel ApiClientLoggerLevel
        {
            get => ApiClientLogger.PrintLevel;
            set => ApiClientLogger.PrintLevel = value;
        }

        private static LearningPortfolio s_instance;
        private static LearningPortfolio Instance
        {
            get => s_instance;
            set
            {
                if (value == null)
                {
                    s_instance = null;
                    return;
                }

                if (s_instance != null)
                    return;

                s_instance = value;
                DontDestroyOnLoad(s_instance.gameObject);

#if UNITY_EDITOR
                UnityEditor.EditorApplication.playModeStateChanged -= PlayModeStateChanged;
                UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChanged;
#endif
            }
        }

#if UNITY_EDITOR
        private static void PlayModeStateChanged(UnityEditor.PlayModeStateChange mode)
        {
            if (mode == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                if (s_instance == null)
                    return;

                DestroyImmediate(s_instance);
                s_instance = null;
            }
        }
#endif

        private static LearningPortfolioProfile s_loadedProfile;

        private LPApiClient m_apiClient;
        [SerializeField] private UserData m_loginUserData;
        private Api.Project m_connectedProject;
        private NetServiceRequestHandler m_netServiceRequestHandler;
        private UserProjectRecordSheet m_currentUserProjectSheet;
        private int m_projectUsageRecordTrackingId;

        private bool m_isUpdatingUserSheet;
        private CancellationTokenSource m_heartbeatCts;

        public static bool IsConnected => Instance != null;
        [Obsolete("現在的 ConnectAsync 已經包含了認證檢查，請直接使用 IsConnected 屬性就可以知道是否已連線。")]
        public static bool IsLoggedIn => Instance != null;
        public static bool IsHasUserProjectRecord => IsConnected && Instance.m_currentUserProjectSheet != null;
        public static bool IsUpdatingUserProjectRecord => IsConnected && Instance.m_isUpdatingUserSheet;
        public static UserData LoginUserData => IsConnected ? Instance.m_loginUserData : null;
        /// <summary>
        /// 登入中的使用者專案紀錄表
        /// </summary>
        public static UserProjectRecordSheet LoggedUserProjectRecordSheet => IsConnected ? Instance.m_currentUserProjectSheet : null;

        public static event Action<UserData> OnUserLogin;
        public static event Action OnUserLogout;
        public static event Action<UserProjectRecordSheet> OnUserProjectRecordUpdated;

        private void Update()
        {
            UpdateUserProjectRecordShower();
        }
        private void OnDestroy()
        {
            if (EwovaAuthManager.Instance != null)
                EwovaAuthManager.Instance.OnAuthStateChanged -= OnAuthStateChanged;

            if (s_instance == this)
            {
                m_currentUserProjectSheet?.Dispose();
                m_netServiceRequestHandler?.CancelAll();
                m_apiClient?.Dispose();

                if (Logger.InfoEnabled)
                    Logger.Info("已斷開與學習歷程服務的連線，並清理相關資源。");
                s_instance = null;
            }
        }

        private async UniTaskVoid KeepLoginUsageRecordHeartbeat()
        {
            if (m_heartbeatCts != null)
                return;

            m_heartbeatCts = new CancellationTokenSource();
            CancellationToken token = CancellationTokenSource.CreateLinkedTokenSource(m_heartbeatCts.Token, destroyCancellationToken).Token;

            if (Logger.InfoEnabled)
                Logger.Info("開始傳送心跳事件以定時紀錄專案使用狀態。");
            try
            {
                await m_apiClient.KeepLoginUsageRecordHeartbeatAsyncProcess(m_projectUsageRecordTrackingId, token);
            }
            catch (OperationCanceledException)
            {
                if (Logger.InfoEnabled)
                    Logger.Info("已取消心跳事件的傳送，停止紀錄專案使用狀態。");
            }
            catch (ApiUsageException usageEx)
            {
                if (Logger.WarnEnabled)
                    Logger.Warn($"心跳事件傳送失敗，可能導致專案使用狀態無法正確紀錄。錯誤訊息: {usageEx.Message}");
            }
            catch (Exception ex)
            {
                if (Logger.ErrorEnabled)
                    UnityEngine.Debug.LogException(ex);
            }

        }

        public static async UniTask<CheckAvailabilityResponse> CheckAvailabilityAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            s_loadedProfile = LoadOrGetProfile();
            if (s_loadedProfile == null)
            {
                return new CheckAvailabilityResponse()
                {
                    FailureReason = CheckAvailabilityFailureReason.DefaultSettingsLoadFailed,
                    ClientErrorMessage = "找不到 Resources/EWova/LearningPortfolioProfile 必要專案設定，請確認資源存在路徑正確且 API Key 正確。"
                };
            }
            if (!s_loadedProfile.APISettings.IsValid(out string errorMessage))
            {
                return new CheckAvailabilityResponse()
                {
                    FailureReason = CheckAvailabilityFailureReason.DefaultSettingsLoadFailed,
                    ClientErrorMessage = $"檢測到學習歷程 Api key 不合規範: {errorMessage} 請檢查 LearningPortfolioProfile.asset"
                };
            }

            var client = new LPApiClient(s_loadedProfile.APISettings, logger: ApiClientLogger);

            if (Instance != null)
            {
                return new CheckAvailabilityResponse()
                {
                    Data = Instance.m_connectedProject,
                    FailureReason = CheckAvailabilityFailureReason.None,
                    Exception = null
                };
            }

            CheckAvailabilityResponse checkAvailabilityResponse = new();
            if (!EwovaAuthManager.Instance.IsSupportAuthorizeViaDeepLink)
            {
                checkAvailabilityResponse.FailureReason = CheckAvailabilityFailureReason.PlatformNotSupportLogin;
                checkAvailabilityResponse.ClientErrorMessage = "當前平台不支援使用系統瀏覽器進行 DeepLink 跳轉授權，無法使用學習歷程服務。";
                client.Dispose();
                return checkAvailabilityResponse;
            }

            checkAvailabilityResponse.FailureReason = CheckAvailabilityFailureReason.ApiCheckApiHealthFailed;
            await InternalCheckAvailabilityAsync(checkAvailabilityResponse, client, ct);

            if (!checkAvailabilityResponse.IsSuccess)
            {
                client.Dispose();
                UnityEngine.Debug.LogException(checkAvailabilityResponse.Exception);
            }

            return checkAvailabilityResponse;
        }
        public static async UniTask<ConnectResponse> ConnectAsync(
            Action<UserProfile> onTriggerLoginProcessOkIfRequired = null,
            CancellationToken cancellationToken = default,
            IProgress<float> progress = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Instance != null)
                return new() { Data = Instance };

            s_loadedProfile = LoadOrGetProfile();
            if (s_loadedProfile == null)
            {
                return new ConnectResponse()
                {
                    FailureReason = ConnectFailureReason.CheckAvailability_DefaultSettingsLoadFailed,
                    ClientErrorMessage = "找不到 Resources/EWova/LearningPortfolioProfile 必要專案設定，請確認資源存在路徑正確且 API Key 正確。"
                };
            }

            if (!s_loadedProfile.APISettings.IsValid(out string errorMessage))
            {
                return new ConnectResponse()
                {
                    FailureReason = ConnectFailureReason.CheckAvailability_DefaultSettingsLoadFailed,
                    ClientErrorMessage = $"檢測到學習歷程 Api key 不合規範: {errorMessage} 請檢查 LearningPortfolioProfile.asset"
                };
            }

            progress.Report(0.05f);
            var client = new LPApiClient(s_loadedProfile.APISettings, logger: ApiClientLogger);

            ConnectResponse connectResp = new();
            if (!EwovaAuthManager.Instance.IsAuthenticated)
            {
                connectResp.FailureReason = ConnectFailureReason.UserAuthFlowFailed;
                progress.Report(0.1f);

                try
                {
                    AuthorizeViaBrowserOptions option = AuthorizeViaBrowserOptions.Default;
#if UNITY_EDITOR
                    if (Authoring.LearningPortfolioEditorPrefs.DisableForceLogin)
                    {
                        option.LoginBehavior = LoginBehavior.Standard;
                        if (Authoring.DevelopTip.IsEnabled)
                            Authoring.EditorLogger.Info("💡 目前已關閉強制登入，若需切換登入帳號，可以到 EWova/Editor/Learning Portfolio/Disable Force Login 關閉此設定。");
                    }
                    else
                    {
                        if (Authoring.DevelopTip.IsEnabled)
                            Authoring.EditorLogger.Info("💡 編輯器開發時，可啟用 EWova/Editor/Learning Portfolio/Disable Force Login 關閉強制登入，在瀏覽器驗證過的情況下可以直接完成驗證，方便開發者重複登入。");
                    }
#endif
                    AuthorizeResult loginResult = await EwovaAuthManager.Instance.AuthorizeViaBrowserAsync(option, cancellationToken: cancellationToken);
                    if (loginResult.Status != AuthorizeProcessResult.Success)
                    {
                        if (loginResult.Status == AuthorizeProcessResult.Cancelled)
                        {
                            connectResp.FailureReason = ConnectFailureReason.ManuallyCancel;
                        }
                        else if (loginResult.Status == AuthorizeProcessResult.Failed)
                        {
                            connectResp.FailureReason = ConnectFailureReason.UserAuthFlowFailed;
                            connectResp.Exception = loginResult.Exception ?? new Exception(loginResult.ErrorMessage ?? "未知的授權錯誤");
                        }
                        return connectResp;
                    }
                }
                catch (OperationCanceledException)
                {
                    connectResp.FailureReason = ConnectFailureReason.ManuallyCancel;
                    return connectResp;
                }
                catch (Exception ex)
                {
                    connectResp.FailureReason = ConnectFailureReason.UserAuthFlowFailed;
                    connectResp.Exception = ex;
                    return connectResp;
                }
            }
            progress.Report(0.2f);

            var pendingUserData = client.AuthenticatedUserProfile;
            var instance = new GameObject().AddComponent<LearningPortfolio>();
            instance.gameObject.name = $"{Name} ({pendingUserData.Nickname}) connecting...";
            instance.enabled = false;
            instance.m_apiClient = client;

            onTriggerLoginProcessOkIfRequired?.Invoke(pendingUserData);
            await instance.InternalConnectAsync(connectResp, cancellationToken
                , Progress.Create<float>(p => progress?.Report(0.2f + (p * 0.7f))));

            if (!connectResp.IsSuccess)
            {
                // 只要連線與獲取資料失敗，則登出以確保狀態一致
                GameObject.Destroy(instance);
                client.Dispose();
                EwovaAuthManager.Instance.Logout();
            }
            else
            {
                instance.gameObject.name = $"{Name} ({pendingUserData.Nickname})";
                instance.enabled = true;
                instance.KeepLoginUsageRecordHeartbeat().Forget();

                progress?.Report(1.0f);
                Instance = instance;
                OnUserLogin.InvokeSafely(Instance.m_loginUserData, onThrow: ex =>
                {
                    if (Logger.ErrorEnabled)
                        Logger.Err("OnUserLogin handler exception:" + ex);
                    UnityEngine.Debug.LogException(ex);
                });
            }

            return connectResp;
        }
        private static bool _isDisconnecting = false;
        public static async UniTask DisconnectAsync()
        {
            if (_isDisconnecting)
                return;

            if (s_instance == null)
                return;

            try
            {
                _isDisconnecting = true;
                if (s_instance.m_isUpdatingUserSheet)
                    await UniTask.WaitUntil(() => !s_instance.m_isUpdatingUserSheet);

                var go = s_instance.gameObject;
                if (go != null)
                    GameObject.Destroy(go);

                EwovaAuthManager.Instance.Logout();
            }
            finally
            {
                _isDisconnecting = false;
                OnUserLogout.InvokeSafely(onThrow: ex =>
                {
                    if (Logger.ErrorEnabled)
                        Logger.Err("OnUserLogout handler exception:" + ex);
                    UnityEngine.Debug.LogException(ex);
                });

                // 確保實例被銷毀
                if (s_instance != null)
                {
                    var go = s_instance.gameObject;
                    if (go != null)
                        GameObject.Destroy(go);
                }
            }
        }
        public static void Disconnect() => DisconnectAsync().Forget();
        public static ProjectRecordShower CreateUserProjectRecordShower(RectTransform rectTransform)
        {
            ProjectRecordShower plane = ProjectRecordShower.InstantiatePlane(rectTransform);

            if (Instance.m_currentUserProjectSheet == null)
                return plane;

            Instance.m_managedProjectRecordShowers.Add(plane);
            InjectDataToShower(plane, Instance.m_currentUserProjectSheet);
            return plane;
        }
        public static async UniTask<UpdatingUserProjectRecordResponse> UpdatingUserProjectRecord(CancellationToken ct)
        {
            if (!IsConnected)
                return new() { FailureReason = UpdatingUserProjectSheetFailureReason.NotConnected };

            if (Instance.m_isUpdatingUserSheet)
            {
                await UniTask.WaitUntil(() => !Instance.m_isUpdatingUserSheet, cancellationToken: ct);
                return UpdatingUserProjectRecordResponse.Success(Instance.m_currentUserProjectSheet);
            }

            var response = new UpdatingUserProjectRecordResponse();

            Instance.m_isUpdatingUserSheet = true;
            try
            {
                await Instance.InternalFetchProjectSheetAsync(response, ct);
            }
            finally
            {
                Instance.m_isUpdatingUserSheet = false;
            }

            if (!response.IsSuccess)
            {
                Debug.LogException(response.Exception);
                return response;
            }

            Instance.m_currentUserProjectSheet = response.Data;
            OnUserProjectRecordUpdated.InvokeSafely(Instance.m_currentUserProjectSheet, onThrow: ex =>
            {
                if (Logger.ErrorEnabled)
                    Logger.Err("OnUserProjectRecordUpdated handler exception:" + ex);
                UnityEngine.Debug.LogException(ex);
            });
            return response;
        }

        private static void OnAuthStateChanged(AuthState newState)
        {
            if (newState == AuthState.Unauthenticated)
                DisconnectAsync().Forget();
        }
        private static async UniTask InternalCheckAvailabilityAsync(CheckAvailabilityResponse response, LPApiClient client, CancellationToken ct = default, IProgress<float> progress = null)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                progress?.Report(0.05f);

                // 1. 檢查 API 健康狀態
                progress?.Report(0.25f);
                response.FailureReason = CheckAvailabilityFailureReason.ApiCheckApiHealthFailed;
                await client.CheckApiHealthAsync(ct);

                // 2. 驗證 API 金鑰並取得專案資訊
                progress?.Report(0.50f);
                response.FailureReason = CheckAvailabilityFailureReason.ApiGetApiKeyValidInfoFailed;
                Api.VerifyProjectInfo valid = await client.GetApiKeyValidInfoAsync(ct);

                if (!valid.IsValid)
                {
                    response.FailureReason = CheckAvailabilityFailureReason.ApiKeyInvalid;
                    response.ServerErrorMessage = valid.ErrorMessage;
                    return;
                }

                // 3. 取得專案資訊
                progress?.Report(0.75f);
                response.FailureReason = CheckAvailabilityFailureReason.GetProjectFailed;
                Api.Project project = await client.GetProjectAsync(valid.ProjectId, ct);

                progress?.Report(1.0f);
                response.FailureReason = CheckAvailabilityFailureReason.None;
                response.Data = project;
                return;
            }
            catch (OperationCanceledException)
            {
                client.Dispose();
                response.FailureReason = CheckAvailabilityFailureReason.ManuallyCancel;
                return;
            }
            catch (Exception ex)
            {
                client.Dispose();

                if (response.FailureReason == CheckAvailabilityFailureReason.None)
                    response.FailureReason = CheckAvailabilityFailureReason.Unknown;

                response.Exception = ex;
                return;
            }
        }
        private async UniTask InternalConnectAsync(ConnectResponse response, CancellationToken ct = default, IProgress<float> progress = null)
        {
            var client = m_apiClient;
            try
            {
                progress?.Report(0.05f);
                CheckAvailabilityResponse checkProAvaRsp = new CheckAvailabilityResponse();
                await InternalCheckAvailabilityAsync(checkProAvaRsp, client, ct
                    , Progress.Create<float>(p => progress?.Report(0.05f + (p * 0.35f))));

                if (!checkProAvaRsp.IsSuccess)
                {
                    response.FailureReason = checkProAvaRsp.FailureReason switch
                    {
                        CheckAvailabilityFailureReason.DefaultSettingsLoadFailed
                            => ConnectFailureReason.CheckAvailability_DefaultSettingsLoadFailed,
                        CheckAvailabilityFailureReason.ApiCheckApiHealthFailed
                            => ConnectFailureReason.CheckAvailability_ApiCheckApiHealthFailed,
                        CheckAvailabilityFailureReason.ApiGetApiKeyValidInfoFailed
                            => ConnectFailureReason.CheckAvailability_ApiGetApiKeyValidInfoFailed,
                        CheckAvailabilityFailureReason.ApiKeyInvalid
                            => ConnectFailureReason.CheckAvailability_ApiKeyInvalid,
                        CheckAvailabilityFailureReason.GetProjectFailed
                            => ConnectFailureReason.CheckAvailability_GetProjectFailed,
                        _
                            => ConnectFailureReason.Unknown
                    };
                    response.ServerErrorMessage = checkProAvaRsp.ServerErrorMessage;
                    return;
                }

                Api.Project project = checkProAvaRsp.Data;

                // 建立專案使用紀錄
                progress?.Report(0.4f);
                response.FailureReason = ConnectFailureReason.CreateProjectUsageSheetFailed;
                var info = DeviceHelper.GetDeviceInfo();
                var authUserProfile = client.AuthenticatedUserProfile;
                var record = await client.CreateProjectUsageRecordAsync
                (
                    project.Id.ToString(),
                    new Api.SetProjectUsageRecordRequest()
                    {
#pragma warning disable CS0618 // 類型或成員已經過時
                        UsingDeviceId = (int)info.UsingDeviceId,
#pragma warning restore CS0618 // 類型或成員已經過時

                        // 這邊回呼後台傳來的當前組織，尚未實作多組織切換
                        OrgId = authUserProfile.OrgId.ToString(),
                        Platform = info.Platform,
                        DeviceModel = info.DeviceModel,
                        IsXRActive = info.IsXRActive
                    }, ct
                );

                var userData = new UserData()
                {
                    Name = authUserProfile.Name,
                    Nickname = authUserProfile.Nickname,
                    Guid = authUserProfile.Id.ToString(),
                    OrgName = authUserProfile.OrgName,
                    OrgGuid = authUserProfile.OrgId.ToString(),
                };

                // 成功建立
                m_projectUsageRecordTrackingId = record.TrackingID;
                m_apiClient = client;
                m_loginUserData = userData;
                m_connectedProject = project;
                m_netServiceRequestHandler = new NetServiceRequestHandler();

                response.FailureReason = ConnectFailureReason.FetchProjectSheetFailed;

                EwovaAuthManager.Instance.OnAuthStateChanged += OnAuthStateChanged;

                progress?.Report(0.5f);
                UpdatingUserProjectRecordResponse fetchProSheet = new();
                await InternalFetchProjectSheetAsync(fetchProSheet, ct
                    , Progress.Create<float>(p => progress?.Report(0.5f + (p * 0.4f))));

                if (!fetchProSheet.IsSuccess)
                {
                    response.FailureReason = fetchProSheet.FailureReason switch
                    {
                        UpdatingUserProjectSheetFailureReason.FindSheetsFailed
                            => ConnectFailureReason.FetchProjectSheet_FindSheetsFailed,
                        UpdatingUserProjectSheetFailureReason.GetSheetFailed
                            => ConnectFailureReason.FetchProjectSheet_GetSheetFailed,
                        UpdatingUserProjectSheetFailureReason.InternalHandleSheetFailed
                            => ConnectFailureReason.FetchProjectSheet_InternalHandleSheetFailed,
                        UpdatingUserProjectSheetFailureReason.ManuallyCancel
                            => ConnectFailureReason.ManuallyCancel,
                        _
                            => ConnectFailureReason.FetchProjectSheetFailed
                    };

                    if (response.FailureReason == ConnectFailureReason.ManuallyCancel)
                    {
                        if (Logger.WarnEnabled)
                            Logger.Warn("Fetch project sheet cancelled by user.");
                    }
                    else
                    {
                        if (Logger.ErrorEnabled)
                            Logger.Err($"Fetch project sheet failed. Failure reason: {response.FailureReason}");

                        if (fetchProSheet.Exception != null)
                            UnityEngine.Debug.LogException(fetchProSheet.Exception);
                    }

                    EwovaAuthManager.Instance.OnAuthStateChanged -= OnAuthStateChanged;
                    return;
                }

                // callback
                m_currentUserProjectSheet = fetchProSheet.Data;

                response.Exception = null;
                response.FailureReason = ConnectFailureReason.None;
                response.Data = this;
                progress?.Report(1.0f);
            }
            catch (LearningPortfolioApiException apiEx)
            {
                response.Exception = apiEx;
                if (apiEx.SourceApiEx.IsServerError)
                    response.ServerErrorMessage = apiEx.SourceApiEx.Message;
            }
            catch (OperationCanceledException)
            {
                response.FailureReason = ConnectFailureReason.ManuallyCancel;
            }
            catch (Exception ex)
            {
                if (response.FailureReason == ConnectFailureReason.None)
                    response.FailureReason = ConnectFailureReason.Unknown;
                response.Exception = ex;
            }
        }
        private async UniTask InternalFetchProjectSheetAsync(UpdatingUserProjectRecordResponse response, CancellationToken ct, IProgress<float> progress = null)
        {
            Dictionary<string, List<Action<Texture2D>>> texResourceHandle = new();

            UserProjectRecordSheet RESULT = null;
            try
            {
                response.FailureReason = UpdatingUserProjectSheetFailureReason.FindSheetsFailed;
                #region 1. 尋找使用者所有紀錄，並選擇第一個；若無紀錄將會自動建立一筆新的紀錄。
                progress?.Report(0.05f);
                List<string> FoundSheets = await m_apiClient.FindSheetsAsync(m_connectedProject.Id.ToString(), ct);
                string targetSheet = FoundSheets[0];
                progress?.Report(0.10f);
                #endregion

                response.FailureReason = UpdatingUserProjectSheetFailureReason.GetSheetFailed;
                #region 2. 取得紀錄內容
                Api.Sheet _rawSheet = await m_apiClient.GetSheetAsync(targetSheet, ct);
                progress?.Report(0.20f);
                #endregion

                RESULT = new UserProjectRecordSheet(
                    sourceProject: m_connectedProject,
                    netServiceHandler: m_netServiceRequestHandler) // 網路服務請求，統一線程列隊處理
                {

                    Owner = m_loginUserData,

                    UserId = _rawSheet.UserId.ToString(),

                    Name = _rawSheet.Name,
                    SheetId = _rawSheet.Id.ToString(),
                    ProjectId = _rawSheet.ProjectId.ToString(),
                    LastUpdatedLocal = _rawSheet.LastUpdated.ToLocalTime(),
                    CompletionProgress = _rawSheet.CompletionProgress,

                    Pages = new Page[_rawSheet.PageLabels.Length]
                };

                response.FailureReason = UpdatingUserProjectSheetFailureReason.InternalHandleSheetFailed;
                #region 3. 初始化路徑節點節點的標記與取消標記方法
                RESULT.SetCompleteIncludeNonNode = new NetSerivceRequest<string>
                (
                    requestHandler: RESULT.NetServiceHandler,
                    func: (path, ct) => m_apiClient.SetCompleteProgressAsync
                    (
                        sheetId: RESULT.SheetId,
                        path: path,
                        ct: ct
                    ),
                    newValueFunc: async (path, ct) =>
                    {
                        RESULT.CompletionProgress = await m_apiClient.GetProgressCompletionAsync(RESULT.SheetId, ct);

                        if (RESULT.ProgressCompletions.Contains(path))
                            return;

                        ((List<string>)RESULT.ProgressCompletions).Add(path);
                        ((List<DateTime>)RESULT.ProgressCompletionsLocalDateTime).Add(DateTime.Now);
                    }
                );
                RESULT.SetUnmarkIncludeNonNode = new NetSerivceRequest<string>
                (
                    requestHandler: RESULT.NetServiceHandler,
                    func: (path, ct) => m_apiClient.SetUnmarkProgressAsync
                    (
                        sheetId: RESULT.SheetId,
                        path: path,
                        ct: ct
                    ),
                    newValueFunc: async (path, ct) =>
                    {
                        RESULT.CompletionProgress = await m_apiClient.GetProgressCompletionAsync(RESULT.SheetId, ct);

                        int index = ((List<string>)RESULT.ProgressCompletions).IndexOf(path);

                        if (index >= 0)
                        {
                            ((List<string>)RESULT.ProgressCompletions).RemoveAt(index);
                            ((List<DateTime>)RESULT.ProgressCompletionsLocalDateTime).RemoveAt(index);
                        }
                    }
                );
                #endregion

                #region 4. 處理進度節點，包含建立節點結構、計算分數權重、以及設定標記與取消標記的網路服務方法
                ProgressNode progressNodeTemp = null;
                float totalScoreWeight = 0f;
                void SetProgressNode(ref ProgressNode pNode, Api.ProgressNode rawNode, ProgressNode parent)
                {
                    pNode = new()
                    {
                        RootSheet = RESULT,
                        Parent = parent,
                        Id = rawNode.Id,
                        Label = rawNode.Label,
                        Description = rawNode.Description,
                        ScoreWeight = rawNode.ScoreWeight,
                        IsHidden = rawNode.Hidden,
                        Children = new ProgressNode[rawNode.Children?.Length ?? 0]
                    };
                    if (pNode.IsLeaf)
                        totalScoreWeight += rawNode.ScoreWeight;

                    if (!texResourceHandle.ContainsKey(rawNode.IconUrl))
                        texResourceHandle[rawNode.IconUrl] = new();
                    ProgressNode cache = pNode;
                    texResourceHandle[rawNode.IconUrl].Add((tex) =>
                    {
                        cache.IconTex = tex;
                    });

                    pNode.Path = parent == null ? pNode.Id : $"{parent.Path}/{pNode.Id}";
                    string path = pNode.Path;
                    pNode.SetComplete = new NetSerivceVoid
                    (
                        requestHandler: RESULT.NetServiceHandler,
                        func: (ct) => m_apiClient.SetCompleteProgressAsync
                        (
                            sheetId: RESULT.SheetId,
                            path: path,
                            ct: ct
                        ),
                        respondFunc: async (ct) =>
                        {
                            RESULT.CompletionProgress = await m_apiClient.GetProgressCompletionAsync(RESULT.SheetId, ct);
                            if (RESULT.ProgressCompletions.Contains(path))
                                return;

                            ((List<string>)RESULT.ProgressCompletions).Add(path);
                            ((List<DateTime>)RESULT.ProgressCompletionsLocalDateTime).Add(DateTime.Now);
                        }
                    );
                    pNode.SetUnmark = new NetSerivceVoid
                    (
                        requestHandler: RESULT.NetServiceHandler,
                        func: (ct) => m_apiClient.SetUnmarkProgressAsync
                        (
                            sheetId: RESULT.SheetId,
                            path: path,
                            ct: ct
                        ),
                        respondFunc: async (ct) =>
                        {
                            RESULT.CompletionProgress = await m_apiClient.GetProgressCompletionAsync(RESULT.SheetId, ct);

                            int index = ((List<string>)RESULT.ProgressCompletions).IndexOf(path);

                            ((List<string>)RESULT.ProgressCompletions).RemoveAt(index);
                            ((List<DateTime>)RESULT.ProgressCompletionsLocalDateTime).RemoveAt(index);
                        }
                    );

                    if (rawNode.Children != null)
                    {
                        for (int i = 0; i < rawNode.Children.Length; i++)
                            SetProgressNode(ref pNode.Children[i], rawNode.Children[i], pNode);
                    }
                }
                SetProgressNode(ref progressNodeTemp, _rawSheet.ProgressNode, null);

                var allProgressNodesPathMapTemp = new Dictionary<string, ProgressNode>(StringComparer.OrdinalIgnoreCase);
                void AfterProcessNode(ProgressNode pNode)
                {
                    allProgressNodesPathMapTemp[pNode.Path] = pNode;

                    pNode.CalculatedProgressScore = totalScoreWeight == 0 ? 0 : (pNode.ScoreWeight / totalScoreWeight);
                    if (pNode.Children != null)
                    {
                        for (int i = 0; i < pNode.Children.Length; i++)
                            AfterProcessNode(pNode.Children[i]);
                    }
                }
                AfterProcessNode(progressNodeTemp);

                RESULT.ProgressNode = progressNodeTemp;
                RESULT.AllProgressNodesPathMap = allProgressNodesPathMapTemp;

                if (_rawSheet.ProgressCompletions != null)
                {
                    var paths = new List<string>();
                    var localTimes = new List<DateTime>();

                    foreach (var item in _rawSheet.ProgressCompletions)
                    {
                        if (string.IsNullOrWhiteSpace(item.Path))
                            continue;

                        paths.Add(item.Path);
                        localTimes.Add(item.DateTime.ToLocalTime());
                    }

                    RESULT.ProgressCompletions = paths;
                    RESULT.ProgressCompletionsLocalDateTime = localTimes;
                }
                #endregion

                #region 5. 處理頁籤與欄位資料，包含建立頁籤結構、讀取欄位資料、以及設定編輯欄位與列的網路服務方法
                int totalPages = _rawSheet.PageLabels.Length;
                for (int i = 0; i < totalPages; i++)
                {
                    int CURRENT_PAGE = i;
                    Api.Page _rawPage = await m_apiClient.GetPageAsync(RESULT.SheetId, CURRENT_PAGE, ct);

                    Page page = RESULT.Pages[CURRENT_PAGE] = new Page
                    {
                        RootSheet = RESULT,
                        Index = CURRENT_PAGE,
                        Label = _rawPage.Label,
                        Columns = new Column[_rawPage.ColumnLabels == null ? 0 : _rawPage.ColumnLabels.Length],
                        Rows = new SortedDictionary<int, Row>(),
                        Cells = new(),
                    };
                    page.AddRow = new NetSerivceRespond<Api.AddRowResponse>
                    (
                        requestHandler: RESULT.NetServiceHandler,
                        func: (ct) => m_apiClient.AddPageRowAsync
                        (
                            sheetId: RESULT.SheetId,
                            page: CURRENT_PAGE,
                            ct: ct
                        ),
                        respondFunc: (respond, ct) =>
                        {
                            return UniTask.Create(async (innerCt) =>
                            {
                                //當使用者呼叫了AddRow 則+一頁
                                await AddRow(respond.RowIndex, 1, innerCt);
                            }, ct);
                        }
                    );
                    page.AddRowAndSetCells = new NetSerivceRequestRespond<Api.SetRowRequest, Api.AddRowResponse>
                    (
                        requestHandler: RESULT.NetServiceHandler,
                        func: (request, ct) => m_apiClient.AddPageRowAsync
                        (
                            sheetId: RESULT.SheetId,
                            page: CURRENT_PAGE,
                            ct: ct
                        ),
                        respondAndNewValueFunc: async (tuple, ct) =>
                        {
                            //當使用者呼叫了AddRow 則+一頁
                            await AddRow(tuple.respond.RowIndex, 1, ct);
                            int newRowIndex = tuple.respond.RowIndex;
                            //寫入列
                            page.Rows[newRowIndex].SetCells.Request
                            (
                                value: tuple.request,
                                onSuccess: () => { Debug.Log("成功寫入新增列資料"); },
                                onFailure: (msg) => { Debug.LogError("寫入新增列資料失敗 因為:" + msg); },
                                onException: (ex) => { Debug.LogException(ex); }
                            );
                        }
                    );
                    page.ClearReadableData = new NetSerivceVoid
                    (
                        requestHandler: RESULT.NetServiceHandler,
                        func: (ct) => m_apiClient.ClearPageReadableDataAsync
                        (
                            sheetId: RESULT.SheetId,
                            page: CURRENT_PAGE
                            , ct: ct
                        ),
                        respondFunc: (ct) =>
                        {
                            page.Cells.Clear();
                            var rows = (SortedDictionary<int, Row>)page.Rows;
                            rows.Clear();
                            return UniTask.CompletedTask;
                        }
                    );

                    Api.Column[] _rawColumns = await m_apiClient.GetPageColumnsAsync(RESULT.SheetId, CURRENT_PAGE, ct);

                    // 處理 Column 此處不具備編輯Cell能力 並長度是固定不變的
                    for (int j = 0; j < page.Columns.Length; j++)
                    {
                        Api.Column _rawColumn = _rawColumns[j];
                        int CURRENT_COLUMN = j;
                        FieldType TryParseFieldType(string fieldType) => Enum.TryParse(fieldType, true, out FieldType parsedFieldType) ? parsedFieldType : FieldType.String;
                        Column column = page.Columns[CURRENT_COLUMN] = new Column
                        {
                            RootPage = page,
                            Index = CURRENT_COLUMN,
                            Label = _rawColumn.Label,
                            IsReadOnly = _rawColumn.IsReadOnly,
                            FieldType = TryParseFieldType(_rawColumn.FieldType),
                        };
                        column.Edit = new NetSerivceRequest<Api.SetColumnRequest>
                        (
                            requestHandler: RESULT.NetServiceHandler,
                            func: (request, ct) => m_apiClient.SetPageColumnAsync
                            (
                                sheetId: RESULT.SheetId,
                                page: CURRENT_PAGE,
                                column: CURRENT_COLUMN,
                                request: request,
                                ct: ct
                            ),
                            newValueFunc: (newValue, ct) =>
                            {
                                column.FieldType = TryParseFieldType(newValue.FieldType);
                                return UniTask.CompletedTask;
                            }
                        );
                    }

                    // 列從1開始查找獲取 0找不到東西
                    await AddRow(1, _rawPage.RowCount, ct);
                    async UniTask AddRow(int start, int count, CancellationToken ct = default)
                    {
                        if (count == 0)
                            return;

                        List<Api.Row> _rawRows = await m_apiClient.GetPageRowsAsync(RESULT.SheetId, CURRENT_PAGE, start, count, ct);

                        for (int i = 0; i < count; i++)
                        {
                            int CURRENT_ROW = i;
                            GetRow(_rawRows[CURRENT_ROW], start + CURRENT_ROW);
                        }

                        void GetRow(Api.Row _rawRow, int targetIndex)
                        {
                            Row newRow = new()
                            {
                                RootPage = page,
                                Index = targetIndex,
                            };
                            ((SortedDictionary<int, Row>)page.Rows).Add(newRow.Index, newRow);

                            newRow.SetCells = new NetSerivceRequest<Api.SetRowRequest>
                            (
                                requestHandler: RESULT.NetServiceHandler,
                                func: (request, ct) => m_apiClient.SetPageRowAsync
                                (
                                    sheetId: RESULT.SheetId,
                                    page: newRow.RootPage.Index,
                                    row: newRow.Index,
                                    request: request,
                                    ct: ct
                                ),
                                newValueFunc: (newValue, ct) =>
                                {
                                    return UniTask.Create(async (innerCt) =>
                                    {
                                        int index = Mathf.Min(newValue.Cells.Length, newRow.Cells.Count);
                                        for (int i = 0; i < index; i++)
                                        {
                                            var cell = newRow.Cells[i];
                                            if (cell.IsReadOnly)
                                                continue;
                                            cell.Text = newValue.Cells[i];
                                        }
                                        await LoadCurrentPageAllColumnSummary(innerCt);
                                        await LoadFirstPageColumnSummary(innerCt);
                                    }, ct);
                                }
                            );
                            // 加入一筆資料
                            List<Cell> rowCells = _rawRow.Cells.Select((x, index) => new Cell()
                            {
                                Column = page.Columns.Length > index ? page.Columns[index] : null,
                                Row = newRow,
                                Text = x
                            }).ToList();
                            page.Cells.Add(targetIndex, rowCells);
                        }
                    }

                    //處理欄總結
                    if (_rawPage.RowCount > 0)
                        await LoadCurrentPageAllColumnSummary(ct);

                    async UniTask LoadCurrentPageAllColumnSummary(CancellationToken ct = default)
                    {
                        if (page.Columns.Length == 0)
                            return;

                        Api.ColumnSummary[] rawColumnSummaries;
                        try
                        {
                            rawColumnSummaries = await m_apiClient.GetPageColumnsSummaryAsync
                            (
                                sheetId: RESULT.SheetId,
                                page: CURRENT_PAGE,
                                ct
                            );
                        }
                        catch (ApiException apiEx)
                        {
                            if (Logger.WarnEnabled)
                            {
                                Logger.Warn($"讀取使用者紀錄 Sheet.Page{CURRENT_PAGE}/Columns/Summary 失敗，可能因為該頁籤的欄位沒有設定公式或是其他原因導致無法計算總結，已將該頁籤所有欄位的總結設為空字串。");
                                Debug.LogException(apiEx);
                            }

                            foreach (var col in page.Columns)
                                col.CellsSummary = string.Empty;

                            return;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"讀取使用者紀錄 Sheet.Page{CURRENT_PAGE}.Columns/Summary({page.Columns.Length}筆) 失敗\nDetail:{ex.Message}", ex);
                        }

                        if (rawColumnSummaries == null || rawColumnSummaries.Length == 0)
                            return;

                        for (int i = 0; i < page.Columns.Length; i++)
                        {
                            int CURRENT_COLUMN = i;
                            Api.ColumnSummary rawColumnSummary = rawColumnSummaries[i];
                            page.Columns[CURRENT_COLUMN].CellsSummary = rawColumnSummary.DisplayValue;
                        }
                    }

                    // 階段 3：分頁載入進度計算（權重從 20% 分配到 50%，共佔 30%）
                    if (totalPages > 0)
                    {
                        float pageProgress = 0.20f + ((float)(i + 1) / totalPages) * 0.30f;
                        progress?.Report(pageProgress);
                    }
                }

                // 階段 4：載入首頁特定欄位總結（完成後達到 80%）
                await LoadFirstPageColumnSummary(ct);
                progress?.Report(0.80f);

                async UniTask LoadFirstPageColumnSummary(CancellationToken ct = default)
                {
                    Column column = RESULT.Pages[0].Columns[1];
                    Cell[] cells = column.Cells.ToArray();
                    foreach (var page in RESULT.Pages)
                    {
                        if (page.Index == 0)
                            continue;

                        cells[page.Index - 1].Text = page.Columns[0].CellsSummary;
                    }
                    try
                    {
                        Api.ColumnSummary rawColumnSummary = await m_apiClient.GetPageColumnSummaryAsync
                        (
                            sheetId: RESULT.SheetId,
                            page: 0,
                            column: 1,
                            ct
                        );
                        column.CellsSummary = rawColumnSummary.DisplayValue;
                    }
                    catch (ApiSheetException apiEx)
                    {
                        if (Logger.WarnEnabled)
                        {
                            Logger.Warn($"讀取使用者紀錄 Sheet.Page0.Column1/Summary 失敗，可能因為該欄位沒有設定公式或是其他原因導致無法計算總結，已將該欄位總結設為空字串。");
                            Debug.LogException(apiEx);
                        }
                        column.CellsSummary = string.Empty;
                    }
                }

                // 階段 5：圖片下載進度（權重從 80% 分配到 100%，共佔 20%）
                var validResources = texResourceHandle
                    .Where(res => !string.IsNullOrWhiteSpace(res.Key) && res.Value != null && res.Value.Count > 0)
                    .ToList();

                int totalTextures = validResources.Count;
                int currentTextureCount = 0;

                foreach (var res in validResources)
                {
                    Texture2D tex = await m_apiClient.GetTex2D(res.Key, isAbsoluteUrl: true, ct);
                    if (tex == null)
                        continue;

                    tex.wrapMode = TextureWrapMode.Clamp;

                    foreach (var apply in res.Value)
                    {
                        apply(tex);
                    }
                    RESULT.ManagedObjects.Add(tex);

                    // 進度計算與更新（因為前面過濾了，totalTextures 必定 > 0）
                    currentTextureCount++;
                    float texProgress = 0.80f + ((float)currentTextureCount / totalTextures) * 0.20f;
                    progress?.Report(texProgress);

                    await UniTask.Yield(ct);
                }
                #endregion

                response.FailureReason = UpdatingUserProjectSheetFailureReason.None;
                response.Data = RESULT;

                // 最終完成確保報告 1.0
                progress?.Report(1.0f);
            }
            catch (LearningPortfolioApiException apiEx)
            {
                RESULT?.Dispose();

                response.FailureReason = UpdatingUserProjectSheetFailureReason.InternalHandleSheetFailed;
                response.ServerErrorMessage = apiEx.Message;
                response.Exception = apiEx;
            }
            catch (OperationCanceledException)
            {
                RESULT?.Dispose();

                response.FailureReason = UpdatingUserProjectSheetFailureReason.ManuallyCancel;
            }
            catch (Exception ex)
            {
                RESULT?.Dispose();

                if (response.FailureReason == UpdatingUserProjectSheetFailureReason.None)
                    response.FailureReason = UpdatingUserProjectSheetFailureReason.Unknown;

                response.Exception = ex;
                return;
            }
            finally
            {
            }
        }

        private readonly List<ProjectRecordShower> m_managedProjectRecordShowers = new();
        private bool m_loggedUserProjectRecordShowerUpdated = false;
        private void UpdateUserProjectRecordShower()
        {
            if (m_managedProjectRecordShowers.Count == 0)
                return;

            List<ProjectRecordShower> toRemove = null;
            bool isUploading = m_currentUserProjectSheet.IsAnyNetSerivceRequesting;
            if (isUploading)
                m_loggedUserProjectRecordShowerUpdated = true;

            bool isDirty = false;
            if (!isUploading && m_loggedUserProjectRecordShowerUpdated)
            {
                isDirty = true;
                m_loggedUserProjectRecordShowerUpdated = false;
            }

            foreach (var item in m_managedProjectRecordShowers)
            {
                if (item == null)
                {
                    toRemove ??= new();
                    toRemove.Add(item);
                    continue;
                }

                item.ShowLoadingCover = isUploading;
                if (isDirty)
                {
                    InjectDataToShower(item, m_currentUserProjectSheet);
                }
            }

            if (toRemove != null)
            {
                foreach (var item in toRemove)
                    m_managedProjectRecordShowers.Remove(item);
            }
        }
        private static Sprite GetSprite(Texture2D tex)
        {
            if (tex == null)
                return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        private static void InjectDataToShower(ProjectRecordShower plane, UserProjectRecordSheet userProjectRecord)
        {
            plane.Clear();

            // Progress Graph
            {
                ProjectRecordShower.GraphContent.Node Convert(ProgressNode pn)
                {
                    var children = new List<ProjectRecordShower.GraphContent.Node>();
                    foreach (var child in pn.Children)
                    {
                        if (child.IsHidden)
                            continue;
                        children.Add(Convert(child));
                    }
                    var result = new ProjectRecordShower.GraphContent.Node
                    {
                        LabelText = $"{pn.Label}",
                        DescriptionText = $"{pn.Description}",
                        IsCompleteSelf = pn.IsCompletedSelf,
                        IsComplete = pn.IsCompleted,
                        CheckDateTimeText = pn.CompleteTime.HasValue ? pn.CompleteTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                        Icon = GetSprite(pn.IconTex),
                        Children = children
                    };

                    if (pn.IsLeaf)
                        result.LabelText += $" ({Mathf.CeilToInt(pn.CalculatedProgressScore * 100f)}%)";

                    return result;
                }
                ProjectRecordShower.GraphContent content = new()
                {
                    Root = Convert(userProjectRecord.ProgressNode)
                };
                plane.SetGraph(content);
            }

            // Chart
            for (int i = 0; i < userProjectRecord.Pages.Length; i++)
            {
                Page page = userProjectRecord.Pages[i];
                ProjectRecordShower.ChartContent content = new()
                {
                    Columns = page.Columns.Select((Column _column) => new ProjectRecordShower.ChartContent.Column()
                    {
                        Label = _column.Label,
                        Cells = _column.Cells.Select(cell => new ProjectRecordShower.ChartContent.Cell()
                        {
                            IsReadOnly = _column.IsReadOnly,
                            LabelText = cell.Text,
                            OverrideAlignment = _column.FieldType switch
                            {
                                FieldType.Number => TMPro.TextAlignmentOptions.Left,
                                _ => TMPro.TextAlignmentOptions.Center
                            }
                        }).ToArray(),
                        CellsSummaryLabel = _column.Cells.Any() ? _column.CellsSummary : null,
                    }).ToArray()
                };
                plane.AddPage(page.Label, content);
            }

            plane.Footer.text = $"你的完成進度為 {(int)(userProjectRecord.CompletionProgress * 100f)}% ！";
        }
        private static LearningPortfolioProfile LoadOrGetProfile()
        {
            if (s_loadedProfile != null)
                return s_loadedProfile;

            LearningPortfolioProfile loadedProfile = null;

            if (s_loadedProfile == null)
                loadedProfile = Resources.Load<LearningPortfolioProfile>("EWova/LearningPortfolioProfile");

            if (loadedProfile == null)
                return null;

            s_loadedProfile = loadedProfile;
            return s_loadedProfile;
        }

    }
}