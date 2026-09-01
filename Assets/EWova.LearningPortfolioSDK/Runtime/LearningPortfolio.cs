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

        /// <summary>
        /// 儲存當前的 API 設定，供實例或擴充方法內部使用
        /// </summary>
        public static ProjectSettings? CurrentProjectSettings;

        /// <summary>
        /// 檢查當前的 API 設定是否有效，若無效則無法進行 API 請求
        /// </summary>
        public static bool IsProjectSettingsValid => CurrentProjectSettings?.IsValid(out _) ?? false;

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
