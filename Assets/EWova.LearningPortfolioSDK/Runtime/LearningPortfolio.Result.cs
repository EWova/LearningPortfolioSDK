using System;
using System.Collections.Generic;

using static EWova.LearningPortfolio.LearningPortfolio;

namespace EWova.LearningPortfolio
{
    public readonly struct Scope<T> : IDisposable where T : class, IScope
    {
        private readonly T _process;
        private readonly bool _isRootScope;

        public static Scope<T> Warp(T process)
        {
            if (process == null)
                return default;
            return new Scope<T>(process);
        }

        private Scope(T process)
        {
            _process = process;
            _isRootScope = (process.ScopeDepth == 0);
            process.ScopeDepth++;
        }

        void IDisposable.Dispose()
        {
            if (_process == null)
                return;
            _process.ScopeDepth--;
            if (_isRootScope && _process.ScopeDepth == 0)
            {
                _process.IsOperationFinished = true;
            }
        }
    }
    public interface IScope
    {
        int ScopeDepth { get; set; }
        int CurrentScopeId { get; internal set; }
        bool IsOperationFinished { get; internal set; }
    }
    public class Process<T, TStatus> : IScope where TStatus : struct, Enum
    {
        public T Data
        {
            get => _data;
            set
            {
                if (IsCompleted)
                    return;

                _data = value;
            }
        }
        public TStatus Status
        {
            get => _status;
            set
            {
                if (IsCompleted)
                    return;

                if (EqualityComparer<TStatus>.Default.Equals(_status, value))
                    return;

                _status = value;

                IsManuallyCancel = Convert.ToInt32(value) == STATUS.ManuallyCancel;
                IsSuccess = Convert.ToInt32(value) == STATUS.Success;

                OnStatusChanged?.Invoke(value);
            }
        }
        public Exception Exception
        {
            get => _exception;
            set
            {
                if (IsCompleted)
                    return;

                _exception = value;
            }
        }
        /// <summary>
        /// Server 端錯誤訊息，Client 端不會主動填寫，通常為非 Exception 類型的預期錯誤資訊
        /// </summary>
        public string ServerErrorMessage
        {
            get => _serverErrorMessage;
            set
            {
                if (IsCompleted)
                    return;

                _serverErrorMessage = value;
            }
        }
        /// <summary>
        /// Client 端錯誤訊息，Client 端在捕捉到 Exception 後可以選擇性填寫，提供給開發者更友善的錯誤資訊
        /// </summary>
        public string ClientErrorMessage
        {
            get => _clientErrorMessage;
            set
            {
                if (IsCompleted)
                    return;

                _clientErrorMessage = value;
            }
        }
        public float Progress
        {
            get => _progress;
            set
            {
                if (IsCompleted)
                    return;

                if (_progress == value)
                    return;
                _progress = value;
                OnProgressChanged?.Invoke(value);
            }
        }
        public bool IsManuallyCancel { get; private set; }
        public bool IsSuccess { get; private set; }
        int IScope.ScopeDepth { get; set; }
        bool IScope.IsOperationFinished
        {
            get => _isOperationFinished;
            set
            {
                if (_isOperationFinished)
                    return;

                _isOperationFinished = value;
                if (value)
                    OnCompleted?.Invoke(this);
            }
        }
        int IScope.CurrentScopeId { get; set; }
        public bool IsCompleted => _isOperationFinished;
        public Action<TStatus> OnStatusChanged { get; set; }
        public Action<float> OnProgressChanged { get; set; }
        public Action<Process<T, TStatus>> OnCompleted { get; set; }

        private TStatus _status;
        private float _progress;
        private T _data;
        private Exception _exception;
        private string _serverErrorMessage;
        private string _clientErrorMessage;
        private bool _isOperationFinished;
    }

    public class CheckAvailabilityProcess : Process<Api.Project, CheckAvailabilityStatus>
    {
        public CheckAvailabilityProcess() { }
        public static CheckAvailabilityProcess Success(Api.Project info) => new() { Data = info };
    }
    public static class STATUS
    {
        public const int Unknown = 0;
        public const int ManuallyCancel = 1;
        public const int Success = 2;
    }

    public enum CheckAvailabilityStatus
    {
        Unknown = STATUS.Unknown,
        ManuallyCancel = STATUS.ManuallyCancel,
        Success = STATUS.Success,

        /* 裝置錯誤 */
        PlatformNotSupportLogin = 50,

        /* 本地錯誤，通常是 SDK 配置問題，導致無法正常發出請求 */
        /// <summary>
        /// 學習歷程 API 預設設定物件載入失敗，可能是因為資源丟失或路徑錯誤。
        /// </summary>
        DefaultSettingsLoad = 100,

        /* 網路錯誤，通常是網路連線問題或後端服務不可用 */
        /// <summary>
        /// 後端服務不可用，可能是因為服務維護、過載或網路問題導致無法連接到學習歷程 API。
        /// </summary>
        ApiCheckApiHealth = 200,
        /// <summary>
        /// API 金鑰驗證失敗，可能是因為金鑰已過期
        /// </summary>
        ApiGetApiKeyValidInfo = 201,

        /* 網路成功，但後端回應錯誤，通常是請求格式錯誤或服務端問題 */
        /// <summary>
        /// 可能是 API 金鑰無效，請確認 API 金鑰是否格式正確與與後台設定相符合。
        /// </summary>
        CheckApiKeyInvalid = 300,
        /// <summary>
        /// 取得專案失敗，可能是因為專案 ID 不存在。
        /// </summary>
        GetProject = 301,
    }

    public class ConnectProcess : Process<LearningPortfolio, ConnectStatus>
    {
        public ConnectProcess() { }
        public static ConnectProcess Success(LearningPortfolio data) => new() { Data = data };

#nullable enable
        public Auth.UserProfile? PendingAuthUserProfile;
#nullable restore
    }
    public enum ConnectStatus
    {
        Unknown = STATUS.Unknown,
        ManuallyCancel = STATUS.ManuallyCancel,
        Success = STATUS.Success,

        /// <summary>
        /// 使用者認證
        /// </summary>
        UserAuthFlow = 101,
        UserAuthFlowOK = 102,

        /// <summary>
        /// 檢查學習歷程 API 可用性失敗，可能是因為網路連線問題、後端服務不可用，或 API 金鑰驗證失敗等原因導致無法成功檢查學習歷程 API 的可用性。
        /// </summary>
        CheckAvailability = 200,
        /// <inheritdoc cref="CheckAvailabilityStatus.DefaultSettingsLoad"/>/>
        CheckAvailability_DefaultSettingsLoad = 201,
        /// <inheritdoc cref="CheckAvailabilityStatus.ApiCheckApiHealth"/>/>
        CheckAvailability_ApiCheckApiHealth = 202,
        /// <inheritdoc cref="CheckAvailabilityStatus.ApiGetApiKeyValidInfo"/>/>
        CheckAvailability_ApiGetApiKeyValidInfo = 203,
        /// <inheritdoc cref="CheckAvailabilityStatus.CheckApiKeyInvalid"/>/>
        CheckAvailability_ApiKeyInvalid = 301,
        /// <inheritdoc cref="CheckAvailabilityStatus.GetProject"/>/>
        CheckAvailability_GetProject = 302,

        /// <summary>
        /// 建立專案使用紀錄失敗，可能是因為請求格式錯誤、使用者權限不足，或後端服務異常導致無法成功記錄使用紀錄。
        /// </summary>
        CreateProjectUsageSheet = 300,

        /// <summary>
        /// 取得專案紀錄失敗，可能是因為專案紀錄不存在、使用者權限不足，或後端服務異常導致無法返回專案紀錄資料。
        /// </summary>
        FetchProjectSheet = 400,
        FetchProjectSheet_FindSheets = 401,
        FetchProjectSheet_GetSheet = 402,
        FetchProjectSheet_InternalHandleSheet = 403,
    }

    public class FetchProjectSheetProcess : Process<UserProjectRecordSheet, FetchProjectSheetStatus>
    {
        public FetchProjectSheetProcess() { }
    }
    public enum FetchProjectSheetStatus
    {
        Unknown = STATUS.Unknown,
        ManuallyCancel = STATUS.ManuallyCancel,
        Success = STATUS.Success,

        /* Client 操作錯誤 */
        /// <summary>
        /// 尚未連接到學習歷程 API，請先呼叫 ConnectAsync 並確保連接成功後再進行專案紀錄更新操作。
        /// </summary>
        FailedNotConnected = 100,
        /// <summary>
        /// 正在進行專案紀錄更新，請等待當前更新完成後再嘗試進行新的更新操作，以避免衝突或資料不一致。
        /// </summary>
        FailedFetchProjectSheetInProgress = 101,

        /* 後端服務錯誤 */
        /// <summary>
        /// 尋找專案紀錄失敗，可能是因為專案紀錄不存在、使用者權限不足，或後端服務異常導致無法返回專案紀錄資料。
        /// </summary>
        FindSheets = 200,
        /// <summary>
        /// 取得專案紀錄失敗，可能是因為專案紀錄不存在、使用者權限不足，或後端服務異常導致無法返回專案紀錄資料。
        /// </summary>
        GetSheet = 201,

        /// <summary>
        /// 處理專案紀錄失敗，可能是因為請求格式錯誤，可能是資料格式錯誤或是資料處理途中出現異常，導致無法成功更新專案紀錄。
        /// 通常不是權限與後端服務的問題
        /// </summary>
        InternalHandleSheet = 300,
    }
}
