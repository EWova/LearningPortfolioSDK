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
    public struct ProjectSettings
    {
        public ProjectSettings(string apiKey)
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
            EWovaAuth = new LearningPortfolioEWovaAuth();
#if UNITY_EDITOR
            Authoring.EditorDomainReleaseHelper.CleanupOneShot += () =>
            {
                OnUserLogin = null;
                OnUserLogout = null;
                OnUserProjectRecordUpdated = null;
                CurrentProjectSettings = null;
                ConnectBlocker.Clear();
                if (s_instance != null)
                {
                    DestroyImmediate(s_instance);
                    s_instance = null;
                }
                EWovaAuth?.Dispose();
                EWovaAuth = null;
            };
#endif
        }

        public static readonly string Name = "[EWova]LearningPortfolio";
        public static LearningPortfolioEWovaAuth EWovaAuth { get; private set; }

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
            }
        }

        /// <summary>
        /// 儲存當前的 API 設定，供實例或擴充方法內部使用
        /// </summary>
        public static ProjectSettings? CurrentProjectSettings;

        /// <summary>
        /// 檢查當前的 API 設定是否有效，若無效則無法進行 API 請求
        /// </summary>
        public static bool IsProjectSettingsValid => CurrentProjectSettings?.IsValid(out _) ?? false;

        private LPApiClient m_apiClient;
        [SerializeField] private UserData m_loginUserData;
        private Api.Project m_connectedProject;
        private NetServiceRequestHandler m_netServiceRequestHandler;
        private UserProjectRecordSheet m_currentUserProjectSheet;
        private int m_projectUsageRecordTrackingId;

        private bool m_isUpdatingUserSheet;
        private CancellationTokenSource m_heartbeatCts;

        public static bool IsConnected => Instance != null;
        public static bool IsHasUserProjectRecord => IsConnected && Instance.m_currentUserProjectSheet != null;
        public static bool IsUpdatingUserProjectRecord => IsConnected && Instance.m_isUpdatingUserSheet;
        public static UserData LoginUserData => IsConnected ? Instance.m_loginUserData : null;
        /// <summary>
        /// 當前專案資訊
        /// </summary>
        public static Api.Project ConnectedProject => IsConnected ? Instance.m_connectedProject : null;
        /// <summary>
        /// 登入中的使用者專案紀錄表
        /// </summary>
        public static UserProjectRecordSheet LoggedUserProjectRecordSheet => IsConnected ? Instance.m_currentUserProjectSheet : null;
        /// <summary>
        /// 自訂的連線前置檢查要求，若有任何一個回傳 true，則 Connect 將會被阻擋，並回傳對應的錯誤訊息。
        /// </summary>
        /// <remarks>
        /// 預設不會阻擋連線，但開發者有需求可以在此加入自訂的檢查邏輯，如：遊戲進行中，不允許後續連線到學習歷程服務等需求
        /// </remarks>
        public static readonly List<Func<(bool isBlocked, string blockedMsg)>> ConnectBlocker = new();
        public static (bool isBlocked, string blockedMsg) IsConnectBlockedByCustomLogic
        {
            get
            {
                if (ConnectBlocker.Count == 0)
                    return (false, null);

                foreach (var require in ConnectBlocker)
                {
                    (bool isBlocked, string blockedMsg) = require.Invoke();
                    if (isBlocked)
                        return (true, blockedMsg);
                }
                return (false, null);
            }
        }

        /// <summary>
        /// <see cref="ChartCellViewProvider"/> 的回傳結果：圖表儲存格要顯示的文字，以及（可選的）文字對齊方式覆寫。
        /// </summary>
        public readonly struct ChartCellDisplay
        {
            public readonly string LabelText;
            public readonly TMPro.TextAlignmentOptions? OverrideAlignment;

            public ChartCellDisplay(string labelText, TMPro.TextAlignmentOptions? overrideAlignment = null)
            {
                LabelText = labelText;
                OverrideAlignment = overrideAlignment;
            }
        }

        /// <summary>
        /// 自訂 <see cref="ProjectRecordShower"/> 圖表中，每個儲存格的原始文字 (<paramref name="text"/>) 依欄位型別
        /// (<paramref name="fieldType"/>) 轉換為顯示用的 <see cref="ChartCellDisplay"/>（LabelText / 對齊方式）。
        /// </summary>
        /// <remarks>
        /// 預設值為 <see cref="DefaultChartCellViewProvider"/>；第三方可以直接改指派這個委派，換成自己的顯示邏輯
        /// （例如自訂日期格式、加上單位、改變對齊方式等），或是在自訂邏輯內先呼叫
        /// <see cref="DefaultChartCellViewProvider"/> 再微調回傳結果。刻意只開放 <see cref="FieldType"/> 與原始文字
        /// 兩個輸入，不直接暴露 <see cref="Column"/>/<see cref="Cell"/>。
        /// </remarks>
        public static Func<FieldType, string, ChartCellDisplay> ChartCellViewProvider = DefaultChartCellViewProvider;

        /// <summary>
        /// <see cref="ChartCellViewProvider"/> 的預設實作。
        /// </summary>
        public static ChartCellDisplay DefaultChartCellViewProvider(FieldType fieldType, string text)
        {
            const string TEXT = "#374151";
            const string SECONDARY = "#6B7280";
            const string UNIT = "#6B7280";
            const string NUMBER = "#2563EB";

            string labelText;
            TMPro.TextAlignmentOptions? overrideAlignment;

            switch (fieldType)
            {
                case FieldType.String:
                    labelText = $"<color={TEXT}>{text}</color>";
                    overrideAlignment = TMPro.TextAlignmentOptions.Center;
                    break;

                case FieldType.Number:
                    if (SheetHelper.TryParseAny<double>(text, out var d))
                    {
                        labelText = $"<color={NUMBER}>{d.ToString("0.##")}</color>";
                        overrideAlignment = TMPro.TextAlignmentOptions.Left;
                    }
                    else
                    {
                        labelText = text;
                        overrideAlignment = null;
                    }
                    break;

                case FieldType.Boolean:
                    if (SheetHelper.TryParseAny<bool>(text, out var b))
                    {
                        labelText = b ? "✓" : "✗";
                        overrideAlignment = null;
                    }
                    else
                    {
                        labelText = text;
                        overrideAlignment = null;
                    }
                    break;

                case FieldType.Percentage:
                    labelText = $"<color={SECONDARY}>{text}</color>";
                    overrideAlignment = TMPro.TextAlignmentOptions.Left;
                    break;

                case FieldType.DurationSeconds:
                    if (SheetHelper.TryParseAny<double>(text, out var dSec))
                    {
                        labelText = $"<color={NUMBER}>{dSec.ToString("0.##")}</color> <color={UNIT}>sec</color>";
                        overrideAlignment = TMPro.TextAlignmentOptions.Left;
                    }
                    else
                    {
                        labelText = text;
                        overrideAlignment = null;
                    }
                    break;

                case FieldType.DurationMinutes:
                    if (SheetHelper.TryParseAny<double>(text, out var dMin))
                    {
                        labelText = $"<color={NUMBER}>{dMin.ToString("0.##")}</color> <color={UNIT}>min</color>";
                        overrideAlignment = TMPro.TextAlignmentOptions.Left;
                    }
                    else
                    {
                        labelText = text;
                        overrideAlignment = null;
                    }
                    break;

                case FieldType.DurationMilliseconds:
                    if (SheetHelper.TryParseAny<double>(text, out var dMs))
                    {
                        labelText = $"<color={NUMBER}>{dMs.ToString("0.##")}</color> <color={UNIT}>ms</color>";
                        overrideAlignment = TMPro.TextAlignmentOptions.Left;
                    }
                    else
                    {
                        labelText = text;
                        overrideAlignment = null;
                    }
                    break;

                case FieldType.DateTimeOffset:
                    if (SheetHelper.TryParseAny<DateTimeOffset>(text, out var dto))
                    {
                        labelText = dto.ToString("yyyy-MM-dd HH:mm:ss");
                        overrideAlignment = TMPro.TextAlignmentOptions.Center;
                    }
                    else
                    {
                        labelText = text;
                        overrideAlignment = null;
                    }
                    break;

                default:
                    labelText = $"<color={SECONDARY}>{text}</color>";
                    overrideAlignment = null;
                    break;
            }

            return new ChartCellDisplay(labelText, overrideAlignment);
        }

        public static event Action<UserData> OnUserLogin;
        public static event Action OnUserLogout;
        public static event Action<UserProjectRecordSheet> OnUserProjectRecordUpdated;

        private void Update()
        {
            UpdateUserProjectRecordShower();
        }
        private void OnDestroy()
        {
            if (EWovaAuth != null)
                EWovaAuth.OnAuthStateChanged -= OnAuthStateChanged;

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

        public static void CheckAvailability(CheckAvailabilityProcess process, CancellationToken ct = default)
            => CheckAvailabilityAsync(process, ct).Forget();
        public static async UniTask CheckAvailabilityAsync(CheckAvailabilityProcess process, CancellationToken ct = default)
        {
            using var scope = Scope<CheckAvailabilityProcess>.Warp(process);

            if (process == null)
                throw new ArgumentNullException(nameof(process));

            ct.ThrowIfCancellationRequested();

            process.Status = CheckAvailabilityStatus.DefaultSettingsLoad;
            if (!LoadProjectSettings(out string errorMsg))
            {
                process.ClientErrorMessage = $"讀取學習歷程專案設定失敗: {errorMsg}";
                return;
            }

            if (Instance != null)
            {
                process.Status = CheckAvailabilityStatus.Success;
                process.Data = Instance.m_connectedProject;
                return;
            }

            if (!EWovaAuth.IsSupportAuthorizeViaDeepLink)
            {
                process.Status = CheckAvailabilityStatus.PlatformNotSupportLogin;
                process.ClientErrorMessage = "當前環境不支援使用系統瀏覽器進行 DeepLink 跳轉授權，無法使用學習歷程服務。";
                return;
            }

            process.Status = CheckAvailabilityStatus.ApiCheckApiHealth;
            var client = new LPApiClient(EWovaAuth, logger: ApiClientLogger);
            await InternalCheckAvailabilityAsync(process, client, ct);

            if (!process.IsSuccess)
            {
                client.Dispose();
                UnityEngine.Debug.LogException(process.Exception);
            }
            else
            {
                // 這個 client 僅用於檢查可用性，Connect 流程會另外建立一個新的 client，故此處必須釋放，避免資源洩漏。
                client.Dispose();
                process.Status = CheckAvailabilityStatus.Success;
            }

        }

        private static bool _isConnecting = false;
        /// <summary>
        /// 目前真正在執行 RunConnectFlowAsync 的那個 process，供併發呼叫的其他 ConnectAsync 等待完成後鏡射其真實結果。
        /// </summary>
        private static ConnectProcess _activeConnectProcess;

        public static void Connect(ConnectProcess process, CancellationToken cancellationToken = default)
            => ConnectAsync(process, cancellationToken).Forget();
        public static async UniTask ConnectAsync(
            ConnectProcess process,
            CancellationToken cancellationToken = default)
        {
            using var scope = Scope<ConnectProcess>.Warp(process);

            (bool isBlocked, string blockedMessage) = IsConnectBlockedByCustomLogic;

            if (isBlocked)
            {
                process.Status = ConnectStatus.ConnectBlockedByCustomLogic;
                process.ClientErrorMessage = blockedMessage;
                return;
            }

            if (process == null)
                throw new ArgumentNullException(nameof(process));

            cancellationToken.ThrowIfCancellationRequested();

            if (Instance != null)
            {
                process.Status = ConnectStatus.Success;
                process.Data = Instance;
                return;
            }

            // 已有另一個 ConnectAsync 呼叫正在進行中，等待其完成後直接鏡射「真正在執行連線」的那個 process 的完整結果，
            // 避免重複建立 LPApiClient / GameObject / 心跳迴圈導致資源洩漏，也避免用 Instance 是否非 null 猜測結果而掩蓋真實的失敗原因。
            if (_isConnecting)
            {
                ConnectProcess activeProcess = _activeConnectProcess;

                await UniTask.WaitUntil(() => !_isConnecting, cancellationToken: cancellationToken);

                if (activeProcess != null)
                {
                    process.Status = activeProcess.Status;
                    process.Data = activeProcess.Data;
                    process.Exception = activeProcess.Exception;
                    process.ClientErrorMessage = activeProcess.ClientErrorMessage;
                    process.ServerErrorMessage = activeProcess.ServerErrorMessage;
                }
                else if (Instance != null)
                {
                    process.Status = ConnectStatus.Success;
                    process.Data = Instance;
                }
                else
                {
                    process.Status = ConnectStatus.UserAuthFlow;
                    process.ClientErrorMessage = "另一個同時進行的連線流程未能成功建立連線，請重新嘗試呼叫 ConnectAsync。";
                }
                return;
            }

            _isConnecting = true;
            _activeConnectProcess = process;
            try
            {
                await RunConnectFlowAsync(process, cancellationToken);
            }
            finally
            {
                _activeConnectProcess = null;
                _isConnecting = false;
            }
        }

        private static async UniTask RunConnectFlowAsync(
            ConnectProcess process,
            CancellationToken cancellationToken)
        {
            process.Status = ConnectStatus.CheckAvailability_DefaultSettingsLoad;
            if (!LoadProjectSettings(out string errorMsg))
            {
                process.ClientErrorMessage = $"讀取學習歷程專案設定失敗: {errorMsg}";
                return;
            }

            process.Progress = 0.05f;

            if (!EWovaAuth.IsAuthenticated)
            {
                process.Status = ConnectStatus.UserAuthFlow;
                process.Progress = 0.1f;

                try
                {
                    AuthorizeViaBrowserOptions option = AuthorizeViaBrowserOptions.Default;
#if UNITY_EDITOR
                    if (Authoring.LearningPortfolioEditorPrefs.DisableForceLogin)
                    {
                        option.LoginBehavior = LoginBehavior.Standard;
                        Authoring.DevelopTip.Info("目前已關閉強制登入，若需切換登入帳號，可以到 EWova/Editor/Learning Portfolio/Disable Force Login 關閉此設定。");
                    }
                    else
                    {
                        Authoring.DevelopTip.Info("編輯器開發時，可啟用 EWova/Editor/Learning Portfolio/Disable Force Login 關閉強制登入，在瀏覽器驗證過的情況下可以直接完成驗證，方便開發者重複登入。");
                    }
#endif
                    AuthorizeResult loginResult = await EWovaAuth.AuthorizeViaBrowserAsync(option, cancellationToken: cancellationToken);
                    if (loginResult.Status != AuthorizeProcessResult.Success)
                    {
                        if (loginResult.Status == AuthorizeProcessResult.Cancelled)
                        {
                            process.Status = ConnectStatus.ManuallyCancel;
                        }
                        else if (loginResult.Status == AuthorizeProcessResult.Failed)
                        {
                            process.Status = ConnectStatus.UserAuthFlow;
                            process.Exception = loginResult.Exception ?? new Exception(loginResult.ErrorMessage ?? "未知的授權錯誤");
                        }
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    process.Status = ConnectStatus.ManuallyCancel;
                    return;
                }
                catch (Exception ex)
                {
                    process.Status = ConnectStatus.UserAuthFlow;
                    process.Exception = ex;
                    return;
                }
            }
            process.Progress = 0.2f;

            var client = new LPApiClient(EWovaAuth, logger: ApiClientLogger);
            var pendingUserData = client.AuthenticatedUserProfile;
            var instance = new GameObject().AddComponent<LearningPortfolio>();
            instance.gameObject.name = $"{Name} ({pendingUserData.Nickname}) connecting...";
            instance.enabled = false;
            instance.m_apiClient = client;

            process.PendingAuthUserProfile = pendingUserData;
            process.Status = ConnectStatus.UserAuthFlowOK;

            ConnectProcess internalProcess = new();
            internalProcess.OnProgressChanged += p => process.Progress = 0.2f + (p * 0.7f);
            internalProcess.OnStatusChanged += s => process.Status = s;
            await instance.InternalConnectAsync(internalProcess, cancellationToken);

            if (!internalProcess.IsSuccess)
            {
                process.Exception = internalProcess.Exception;
                process.ClientErrorMessage = internalProcess.ClientErrorMessage;
                process.ServerErrorMessage = internalProcess.ServerErrorMessage;

                // 只要連線與獲取資料失敗，則登出以確保狀態一致
                GameObject.Destroy(instance);
                client.Dispose();
                EWovaAuth.Logout();
            }
            else
            {
                instance.gameObject.name = $"{Name} ({pendingUserData.Nickname})";
                instance.enabled = true;
                instance.KeepLoginUsageRecordHeartbeat().Forget();

                process.Progress = 1.0f;
                process.Status = ConnectStatus.Success;
                EWovaAuth.ProjectId = instance.m_connectedProject.Id.ToString();
                Instance = instance;
                OnUserLogin.InvokeSafely(Instance.m_loginUserData, onThrow: ex =>
                {
                    if (Logger.ErrorEnabled)
                        Logger.Err("OnUserLogin handler exception:" + ex);
                    UnityEngine.Debug.LogException(ex);
                });
            }
        }
        private static bool _isDisconnecting = false;

        public static void Disconnect()
            => DisconnectAsync().Forget();
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

                EWovaAuth.Logout();
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

        public static ProjectRecordShower CreateUserProjectSheetShower(RectTransform rectTransform)
        {
            if (!IsConnected)
                throw new Exception("尚未連線到學習歷程服務。請先 Connect。");

            ProjectRecordShower plane = ProjectRecordShower.InstantiatePlane(rectTransform);

            if (Instance.m_currentUserProjectSheet == null)
                return plane;

            Instance.m_managedProjectRecordShowers.Add(plane);
            InjectDataToShower(plane, Instance.m_currentUserProjectSheet);
            return plane;
        }
        public static async UniTask FetchUserProjectSheet(FetchProjectSheetProcess process, CancellationToken ct)
        {
            using var scope = Scope<FetchProjectSheetProcess>.Warp(process);

            if (!IsConnected)
            {
                process.Status = FetchProjectSheetStatus.FailedNotConnected;
                process.ClientErrorMessage = "尚未連線到學習歷程服務，無法取得使用者專案紀錄表。請先呼叫 ConnectAsync 並確保連線成功。";
                return;
            }

            if (Instance.m_isUpdatingUserSheet)
            {
                await UniTask.WaitUntil(() => !Instance.m_isUpdatingUserSheet, cancellationToken: ct);
                process.Status = FetchProjectSheetStatus.Success;
                process.Data = Instance.m_currentUserProjectSheet;
                return;
            }

            process.Status = FetchProjectSheetStatus.FailedFetchProjectSheetInProgress;

            Instance.m_isUpdatingUserSheet = true;
            try
            {
                await Instance.InternalFetchProjectSheetAsync(process, ct);
            }
            finally
            {
                Instance.m_isUpdatingUserSheet = false;
            }

            if (!process.IsSuccess)
            {
                Debug.LogException(process.Exception);
                return;
            }

            Instance.m_currentUserProjectSheet = process.Data;
            OnUserProjectRecordUpdated.InvokeSafely(Instance.m_currentUserProjectSheet, onThrow: ex =>
            {
                if (Logger.ErrorEnabled)
                    Logger.Err("OnUserProjectRecordUpdated handler exception:" + ex);
                UnityEngine.Debug.LogException(ex);
            });
        }

        private static void OnAuthStateChanged(AuthState newState)
        {
            if (newState == AuthState.Unauthenticated)
                DisconnectAsync().Forget();
        }
        private static async UniTask InternalCheckAvailabilityAsync(CheckAvailabilityProcess process, LPApiClient client, CancellationToken ct = default)
        {
            using var scope = Scope<CheckAvailabilityProcess>.Warp(process);

            ct.ThrowIfCancellationRequested();
            try
            {
                // 1. 檢查 API 健康狀態
                process.Progress = 0.05f;
                process.Status = CheckAvailabilityStatus.ApiCheckApiHealth;
                await client.CheckApiHealthAsync(ct);

                // 2. 驗證 API 金鑰並取得專案資訊
                process.Progress = 0.50f;
                process.Status = CheckAvailabilityStatus.ApiGetApiKeyValidInfo;
                Api.VerifyProjectInfo valid = await client.GetApiKeyValidInfoAsync(ct);

                process.Status = CheckAvailabilityStatus.CheckApiKeyInvalid;
                if (!valid.IsValid)
                {
                    process.ServerErrorMessage = valid.ErrorMessage;
                    return;
                }

                // 3. 取得專案資訊
                process.Progress = 0.75f;
                process.Status = CheckAvailabilityStatus.GetProject;
                Api.Project project = await client.GetProjectAsync(valid.ProjectId, ct);

                process.Progress = 1.0f;
                process.Status = CheckAvailabilityStatus.Success;
                process.Data = project;
                return;
            }
            catch (OperationCanceledException)
            {
                client.Dispose();
                process.Status = CheckAvailabilityStatus.ManuallyCancel;
                return;
            }
            catch (Exception ex)
            {
                client.Dispose();
                process.Exception = ex;
                return;
            }
        }
        private async UniTask InternalConnectAsync(ConnectProcess process, CancellationToken ct = default)
        {
            using var scope = Scope<ConnectProcess>.Warp(process);

            var client = m_apiClient;
            try
            {
                process.Progress = 0.05f;
                CheckAvailabilityProcess checkProAvaProcess = new();
                checkProAvaProcess.OnProgressChanged += p => process.Progress = 0.05f + (p * 0.35f);
                checkProAvaProcess.OnStatusChanged += s =>
                {
                    process.Status = s switch
                    {
                        CheckAvailabilityStatus.DefaultSettingsLoad
                            => ConnectStatus.CheckAvailability_DefaultSettingsLoad,
                        CheckAvailabilityStatus.ApiCheckApiHealth
                            => ConnectStatus.CheckAvailability_ApiCheckApiHealth,
                        CheckAvailabilityStatus.ApiGetApiKeyValidInfo
                            => ConnectStatus.CheckAvailability_ApiGetApiKeyValidInfo,
                        CheckAvailabilityStatus.CheckApiKeyInvalid
                            => ConnectStatus.CheckAvailability_ApiKeyInvalid,
                        CheckAvailabilityStatus.GetProject
                            => ConnectStatus.CheckAvailability_GetProject,
                        _
                            => ConnectStatus.CheckAvailability
                    };
                };
                await InternalCheckAvailabilityAsync(checkProAvaProcess, client, ct);

                if (!checkProAvaProcess.IsSuccess)
                    return;

                Api.Project project = checkProAvaProcess.Data;

                // 建立專案使用紀錄
                process.Progress = 0.4f;
                process.Status = ConnectStatus.CreateProjectUsageSheet;
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

                process.Status = ConnectStatus.FetchProjectSheet;

                EWovaAuth.OnAuthStateChanged += OnAuthStateChanged;

                process.Progress = 0.5f;
                FetchProjectSheetProcess fetchProSheet = new();
                fetchProSheet.OnProgressChanged += p => process.Progress = 0.5f + (p * 0.4f);
                fetchProSheet.OnStatusChanged += s =>
                {
                    process.Status = fetchProSheet.Status switch
                    {
                        FetchProjectSheetStatus.FindSheets
                            => ConnectStatus.FetchProjectSheet_FindSheets,
                        FetchProjectSheetStatus.GetSheet
                            => ConnectStatus.FetchProjectSheet_GetSheet,
                        FetchProjectSheetStatus.InternalHandleSheet
                            => ConnectStatus.FetchProjectSheet_InternalHandleSheet,
                        FetchProjectSheetStatus.ManuallyCancel
                            => ConnectStatus.ManuallyCancel,
                        _
                            => ConnectStatus.FetchProjectSheet
                    };
                };
                await InternalFetchProjectSheetAsync(fetchProSheet, ct);

                if (!fetchProSheet.IsSuccess)
                {
                    if (fetchProSheet.Status == FetchProjectSheetStatus.ManuallyCancel)
                    {
                        if (Logger.WarnEnabled)
                            Logger.Warn("Fetch project sheet cancelled by user.");
                    }
                    else
                    {
                        if (Logger.ErrorEnabled)
                            Logger.Err($"Fetch project sheet failed. Failure reason:{fetchProSheet.Status} ClientErrMsg:{fetchProSheet.ClientErrorMessage} ServerErrMsg:{fetchProSheet.ServerErrorMessage}");
                    }

                    EWovaAuth.OnAuthStateChanged -= OnAuthStateChanged;

                    process.Exception = fetchProSheet.Exception;
                    process.ClientErrorMessage = fetchProSheet.ClientErrorMessage;
                    process.ServerErrorMessage = fetchProSheet.ServerErrorMessage;
                    return;
                }

                // callback
                m_currentUserProjectSheet = fetchProSheet.Data;

                process.Exception = null;
                process.Status = ConnectStatus.Success;
                process.Data = this;
                process.Progress = 1.0f;
            }
            catch (LearningPortfolioApiException apiEx)
            {
                process.Exception = apiEx;
                if (apiEx.SourceApiEx.IsServerError)
                    process.ServerErrorMessage = apiEx.SourceApiEx.Message;
            }
            catch (OperationCanceledException)
            {
                process.Status = ConnectStatus.ManuallyCancel;
            }
            catch (Exception ex)
            {
                process.Exception = ex;
            }
        }
        private async UniTask InternalFetchProjectSheetAsync(FetchProjectSheetProcess process, CancellationToken ct)
        {
            using var scope = Scope<FetchProjectSheetProcess>.Warp(process);

            Dictionary<string, List<Action<Texture2D>>> texResourceHandle = new();

            UserProjectRecordSheet RESULT = null;
            try
            {
                process.Status = FetchProjectSheetStatus.FindSheets;
                #region 1. 尋找使用者所有紀錄，並選擇第一個；若無紀錄將會自動建立一筆新的紀錄。
                process.Progress = 0.05f;
                List<string> FoundSheets = await m_apiClient.FindSheetsAsync(m_connectedProject.Id.ToString(), ct);
                if (FoundSheets == null || FoundSheets.Count == 0)
                    throw new ApiSheetException(ApiAction.FindSheets, null, $"專案 {m_connectedProject.Id} 未回傳任何使用者紀錄，無法取得學習歷程紀錄。");
                string targetSheet = FoundSheets[0];
                process.Progress = 0.10f;
                #endregion

                process.Status = FetchProjectSheetStatus.GetSheet;
                #region 2. 取得紀錄內容
                Api.Sheet _rawSheet = await m_apiClient.GetSheetAsync(targetSheet, ct);
                process.Progress = 0.20f;
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

                process.Status = FetchProjectSheetStatus.InternalHandleSheet;
                #region 3. 初始化路徑節點節點的標記與取消標記方法
                RESULT.SetProgressMark = new NetServiceRequest<string>
                (
                    handler: RESULT.NetServiceHandler,
                    func: (path, ct) => m_apiClient.SetCompleteProgressAsync
                    (
                        sheetId: RESULT.SheetId,
                        path: path,
                        ct: ct
                    ),
                    onRespond: async (path, ct) =>
                    {
                        RESULT.CompletionProgress = await m_apiClient.GetProgressCompletionAsync(RESULT.SheetId, ct);
                        Dictionary<string, DateTime> obj = (Dictionary<string, DateTime>)RESULT.AllMarkedProgressDic;
                        obj.TryAdd(path, DateTime.Now);
                    }
                );
                RESULT.SetProgressUnmark = new NetServiceRequest<string>
                (
                    handler: RESULT.NetServiceHandler,
                    func: (path, ct) => m_apiClient.SetUnmarkProgressAsync
                    (
                        sheetId: RESULT.SheetId,
                        path: path,
                        ct: ct
                    ),
                    onRespond: async (path, ct) =>
                    {
                        RESULT.CompletionProgress = await m_apiClient.GetProgressCompletionAsync(RESULT.SheetId, ct);
                        Dictionary<string, DateTime> obj = (Dictionary<string, DateTime>)RESULT.AllMarkedProgressDic;
                        obj.Remove(path);
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
                    pNode.SetMark = new NetServiceVoid
                    (
                        handler: RESULT.NetServiceHandler,
                        func: (ct) => m_apiClient.SetCompleteProgressAsync
                        (
                            sheetId: RESULT.SheetId,
                            path: path,
                            ct: ct
                        ),
                        onRespond: async (ct) =>
                        {
                            RESULT.CompletionProgress = await m_apiClient.GetProgressCompletionAsync(RESULT.SheetId, ct);
                            Dictionary<string, DateTime> obj = (Dictionary<string, DateTime>)RESULT.AllMarkedProgressDic;
                            obj.TryAdd(path, DateTime.Now);
                        }
                    );
                    pNode.SetUnmark = new NetServiceVoid
                    (
                        handler: RESULT.NetServiceHandler,
                        func: (ct) => m_apiClient.SetUnmarkProgressAsync
                        (
                            sheetId: RESULT.SheetId,
                            path: path,
                            ct: ct
                        ),
                        onRespond: async (ct) =>
                        {
                            RESULT.CompletionProgress = await m_apiClient.GetProgressCompletionAsync(RESULT.SheetId, ct);
                            Dictionary<string, DateTime> obj = (Dictionary<string, DateTime>)RESULT.AllMarkedProgressDic;
                            obj.Remove(path);
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
                    var items = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

                    foreach (var item in _rawSheet.ProgressCompletions)
                    {
                        if (string.IsNullOrWhiteSpace(item.Path))
                            continue;

                        items.Add(item.Path, item.DateTime.ToLocalTime());
                    }

                    RESULT.AllMarkedProgressDic = items;
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
                    page.AddRow = new NetServiceRespond<Api.AddRowResponse>
                    (
                        handler: RESULT.NetServiceHandler,
                        func: (ct) => m_apiClient.AddPageRowAsync
                        (
                            sheetId: RESULT.SheetId,
                            page: CURRENT_PAGE,
                            ct: ct
                        ),
                        onRespond: (respond, ct) =>
                        {
                            return UniTask.Create(async (innerCt) =>
                            {
                                //當使用者呼叫了AddRow 則+一頁
                                await AddRow(respond.RowIndex, 1, innerCt);
                            }, ct);
                        }
                    );
                    page.AddRowAndSetCells = new NetService<Api.SetRowRequest, Api.AddRowResponse>
                    (
                        requestHandler: RESULT.NetServiceHandler,
                        func: (request, ct) => m_apiClient.AddPageRowAsync
                        (
                            sheetId: RESULT.SheetId,
                            page: CURRENT_PAGE,
                            ct: ct
                        ),
                        onRespond: async (tuple, ct) =>
                        {
                            //當使用者呼叫了AddRow 則+一頁
                            await AddRow(tuple.Respond.RowIndex, 1, ct);
                            int newRowIndex = tuple.Respond.RowIndex;
                            //寫入列
                            page.Rows[newRowIndex].SetCells.Request
                            (
                                request: tuple.Request,
                                onSuccess: () => { Debug.Log("成功寫入新增列資料"); },
                                onFailure: (msg) => { Debug.LogError("寫入新增列資料失敗 因為:" + msg); },
                                onException: (ex) => { Debug.LogException(ex); }
                            );
                        }
                    );
                    page.ClearReadableData = new NetServiceVoid
                    (
                        handler: RESULT.NetServiceHandler,
                        func: (ct) => m_apiClient.ClearPageReadableDataAsync
                        (
                            sheetId: RESULT.SheetId,
                            page: CURRENT_PAGE
                            , ct: ct
                        ),
                        onRespond: (ct) =>
                        {
                            page.Cells.Clear();
                            var rows = (SortedDictionary<int, Row>)page.Rows;
                            rows.Clear();
                            return UniTask.Create(async (innerCt) =>
                            {
                                await LoadCurrentPageAllColumnSummary(innerCt);
                                await LoadFirstPageColumnSummary(innerCt);
                            }, ct);
                        }
                    );

                    Api.Column[] _rawColumns = await m_apiClient.GetPageColumnsAsync(RESULT.SheetId, CURRENT_PAGE, ct);

                    // 處理 Column 此處不具備編輯Cell能力 並長度是固定不變的
                    for (int j = 0; j < page.Columns.Length; j++)
                    {
                        Api.Column _rawColumn = _rawColumns[j];
                        int CURRENT_COLUMN = j;
                        FieldType TryParseFieldType(string fieldType)
                        {
                            return fieldType?.ToLowerInvariant() switch
                            {
                                "number" => FieldType.Number,
                                "string" => FieldType.String,
                                "boolean" => FieldType.Boolean,
                                "percentage" => FieldType.Percentage,
                                "duration_seconds" => FieldType.DurationSeconds,
                                "duration_minutes" => FieldType.DurationMinutes,
                                "duration_ms" => FieldType.DurationMilliseconds,
                                "datetimeoffset" => FieldType.DateTimeOffset,
                                _ => FieldType.String
                            };
                        }

                        Column column = page.Columns[CURRENT_COLUMN] = new Column
                        {
                            RootPage = page,
                            Index = CURRENT_COLUMN,
                            Label = _rawColumn.Label,
                            IsReadOnly = _rawColumn.IsReadOnly,
                            FieldType = TryParseFieldType(_rawColumn.FieldType),
                        };
#pragma warning disable CS0618 // 類型或成員已經過時
                        column.Edit = new NetServiceRequest<Api.SetColumnRequest>
                        (
                            handler: RESULT.NetServiceHandler,
                            func: (request, ct) =>
                            {
                                Debug.LogWarning("Column.Edit 已棄用，欄位型別已改由後台管理，此呼叫不會實際送出請求。");
                                return UniTask.CompletedTask;
                            }
                        );
#pragma warning restore CS0618 // 類型或成員已經過時
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

                            newRow.SetCells = new NetServiceRequest<Api.SetRowRequest>
                            (
                                handler: RESULT.NetServiceHandler,
                                func: (request, ct) => m_apiClient.SetPageRowAsync
                                (
                                    sheetId: RESULT.SheetId,
                                    page: newRow.RootPage.Index,
                                    row: newRow.Index,
                                    request: request,
                                    ct: ct
                                ),
                                onRespond: (newValue, ct) =>
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
                        {
                            // 若該頁籤沒有欄位，則直接將所有欄位的總結設為空字串
                            foreach (var col in page.Columns)
                                col.CellsSummary = string.Empty;
                            return;
                        }

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
                        {
                            foreach (var col in page.Columns)
                                col.CellsSummary = string.Empty;
                            return;
                        }

                        for (int i = 0; i < page.Columns.Length; i++)
                        {
                            int CURRENT_COLUMN = i;
                            Api.ColumnSummary rawColumnSummary = rawColumnSummaries[CURRENT_COLUMN];
                            page.Columns[CURRENT_COLUMN].CellsSummary = rawColumnSummary.DisplayValue;
                        }
                    }

                    // 階段 3：分頁載入進度計算（權重從 20% 分配到 50%，共佔 30%）
                    if (totalPages > 0)
                    {
                        float pageProgress = 0.20f + ((float)(i + 1) / totalPages) * 0.30f;
                        process.Progress = pageProgress;
                    }
                }

                // 階段 4：載入首頁特定欄位總結（完成後達到 80%）
                await LoadFirstPageColumnSummary(ct);
                process.Progress = 0.80f;

                async UniTask LoadFirstPageColumnSummary(CancellationToken ct = default)
                {
                    if (RESULT.Pages.Length == 0)
                    {
                        if (Logger.WarnEnabled)
                            Logger.Warn("首頁欄位不足，略過首頁欄位總結的計算。");
                        return;
                    }

                    Column column = RESULT.Pages[0].Columns[1];
                    Cell[] cells = column.Cells.ToArray();
                    foreach (var page in RESULT.Pages)
                    {
                        if (page.Index == 0)
                            continue;

                        // 個別頁總結
                        cells[page.Index - 1].Text = page.Columns[0].CellsSummary;
                    }
                    try
                    {
                        // 總覽頁總結
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
                    Texture2D tex;
                    try
                    {
                        tex = await m_apiClient.GetTex2D(res.Key, isAbsoluteUrl: true, null, ct);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        if (Logger.WarnEnabled)
                        {
                            Logger.Warn($"下載圖示資源失敗（{res.Key}），將略過此圖示，不影響其餘學習歷程資料。");
                            Debug.LogException(ex);
                        }
                        continue;
                    }
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
                    process.Progress = texProgress;

                    await UniTask.Yield(ct);
                }
                #endregion

                process.Status = FetchProjectSheetStatus.Success;
                process.Data = RESULT;

                // 最終完成確保報告 1.0
                process.Progress = 1.0f;
            }
            catch (LearningPortfolioApiException apiEx)
            {
                RESULT?.Dispose();
                process.ServerErrorMessage = apiEx.Message;
                process.Exception = apiEx;
            }
            catch (OperationCanceledException)
            {
                RESULT?.Dispose();
                process.Status = FetchProjectSheetStatus.ManuallyCancel;
            }
            catch (Exception ex)
            {
                RESULT?.Dispose();
                process.Exception = ex;
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
            bool isUploading = m_currentUserProjectSheet.IsAnyNetServiceRequesting;
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
        private string footerText;
        private static void InjectDataToShower(ProjectRecordShower plane, UserProjectRecordSheet userProjectRecord)
        {
            plane.Clear();

            // Progress Graph
            {
                static ProjectRecordShower.GraphContent.Node Convert(ProgressNode pn)
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
                        IsMarked = pn.IsMarked,
                        IsComplete = pn.IsCompleted,
                        CheckDateTimeText = pn.MarkedTime.HasValue ? pn.MarkedTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
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
                    Columns = page.Columns.Select(_column => new ProjectRecordShower.ChartContent.Column()
                    {
                        Label = _column.Label,
                        Cells = _column.Cells.Select(cell =>
                        {
                            ChartCellDisplay display = ChartCellViewProvider(_column.FieldType, cell.Text);
                            return new ProjectRecordShower.ChartContent.Cell
                            {
                                IsReadOnly = _column.IsReadOnly,
                                LabelText = display.LabelText,
                                OverrideAlignment = display.OverrideAlignment
                            };
                        }).ToArray(),
                        CellsSummaryLabel = _column.Cells.Any() ? _column.CellsSummary : null,
                    }).ToArray()
                };
                plane.AddPage(page.Label, content);
            }

            plane.Footer.text = $"正在檢視 <color=#F80>{EWovaAuth.CurrentUser.Nickname}</color> 的學習資料！ 目前的學習完成度為 <color=#F80>{(int)(userProjectRecord.CompletionProgress * 100f)}%</color> ！";
        }

        public static bool TryLoadProjectSettings(out ProjectSettings? profile, out string errorMessage)
        {
            LearningPortfolioProfile loadedProfile = null;

            if (loadedProfile == null)
                loadedProfile = Resources.Load<LearningPortfolioProfile>("EWova/LearningPortfolioProfile");

            if (loadedProfile == null)
            {
                errorMessage = "無法從 Resources 中載入 ProjectSettings，請確認 LearningPortfolioProfile 是否存在於 Resources 資料夾中，並且已正確設定。";
                profile = null;
                return false;
            }
            else if (!loadedProfile.ProjectSettings.IsValid(out string innerErrorMessage))
            {
                errorMessage = $"載入的 ProjectSettings 不符合規範: {innerErrorMessage}";
                profile = null;
                return false;
            }
            else
            {
                errorMessage = null;
                profile = loadedProfile.ProjectSettings;
                return true;
            }
        }

        public static bool LoadProjectSettings(out string errorMessage)
        {
            bool result = TryLoadProjectSettings(out var proSetting, out errorMessage);
            if (result)
            {
                CurrentProjectSettings = proSetting;
                EWovaAuth.ApiKey = CurrentProjectSettings.Value.APIKey;
            }
            return result;
        }
    }
}