using System;

using static EWova.LearningPortfolio.LearningPortfolio;

namespace EWova.LearningPortfolio
{
    public class Response<T, TReason> where TReason : struct, Enum
    {
        public T Data { get; set; }
        public TReason FailureReason { get; set; }
        public Exception Exception { get; set; }
        /// <summary>
        /// Server 端錯誤訊息，Client 端不會主動填寫，通常為非 Exception 類型的預期錯誤資訊
        /// </summary>
        public string ServerErrorMessage { get; set; }
        public bool IsManuallyCancel => Convert.ToInt32(FailureReason) == 2;
        public bool IsSuccess => Convert.ToInt32(FailureReason) == 0 && Exception == null;
    }

    public class CheckAvailabilityResponse : Response<Api.Project, CheckAvailabilityFailureReason>
    {
        public CheckAvailabilityResponse() { }
        public static CheckAvailabilityResponse Success(Api.Project info) => new() { Data = info };
    }
    public enum CheckAvailabilityFailureReason
    {
        None = 0,
        Unknown = 1,
        ManuallyCancel = 2,

        /* 裝置錯誤 */
        PlatformNotSupportLogin = 100,

        /* 本地錯誤，通常是 SDK 配置問題，導致無法正常發出請求 */
        /// <summary>
        /// 學習歷程 API 預設設定物件載入失敗，可能是因為資源丟失或路徑錯誤。
        /// </summary>
        DefaultSettingsLoadFailed = 100,

        /* 網路錯誤，通常是網路連線問題或後端服務不可用 */
        /// <summary>
        /// 後端服務不可用，可能是因為服務維護、過載或網路問題導致無法連接到學習歷程 API。
        /// </summary>
        ApiCheckApiHealthFailed = 200,
        /// <summary>
        /// API 金鑰驗證失敗，可能是因為金鑰已過期
        /// </summary>
        ApiGetApiKeyValidInfoFailed = 201,

        /* 網路成功，但後端回應錯誤，通常是請求格式錯誤或服務端問題 */
        /// <summary>
        /// 可能是 API 金鑰無效，請確認 API 金鑰是否格式正確與與後台設定相符合。
        /// </summary>
        ApiKeyInvalid = 300,
        /// <summary>
        /// 取得專案失敗，可能是因為專案 ID 不存在。
        /// </summary>
        GetProjectFailed = 301,
    }

    public class ConnectResponse : Response<LearningPortfolio, ConnectFailureReason>
    {
        public ConnectResponse() { }
        public static ConnectResponse Success(LearningPortfolio data) => new() { Data = data };
    }
    public enum ConnectFailureReason
    {
        None = 0,
        Unknown = 1,
        ManuallyCancel = 2,

        /// <summary>
        /// 使用者認證失敗
        /// </summary>
        UserAuthFlowFailed = 101,

        /// <summary>
        /// 檢查學習歷程 API 可用性失敗，可能是因為網路連線問題、後端服務不可用，或 API 金鑰驗證失敗等原因導致無法成功檢查學習歷程 API 的可用性。
        /// </summary>
        CheckAvailabilityFailed = 200,
        /// <inheritdoc cref="CheckAvailabilityFailureReason.DefaultSettingsLoadFailed"/>/>
        CheckAvailability_DefaultSettingsLoadFailed = 201,
        /// <inheritdoc cref="CheckAvailabilityFailureReason.ApiCheckApiHealthFailed"/>/>
        CheckAvailability_ApiCheckApiHealthFailed = 202,
        /// <inheritdoc cref="CheckAvailabilityFailureReason.ApiGetApiKeyValidInfoFailed"/>/>
        CheckAvailability_ApiGetApiKeyValidInfoFailed = 203,
        /// <inheritdoc cref="CheckAvailabilityFailureReason.ApiKeyInvalid"/>/>
        CheckAvailability_ApiKeyInvalid = 301,
        /// <inheritdoc cref="CheckAvailabilityFailureReason.GetProjectFailed"/>/>
        CheckAvailability_GetProjectFailed = 302,

        /// <summary>
        /// 建立專案使用紀錄失敗，可能是因為請求格式錯誤、使用者權限不足，或後端服務異常導致無法成功記錄使用紀錄。
        /// </summary>
        CreateProjectUsageSheetFailed = 300,

        /// <summary>
        /// 取得專案紀錄失敗，可能是因為專案紀錄不存在、使用者權限不足，或後端服務異常導致無法返回專案紀錄資料。
        /// </summary>
        FetchProjectSheetFailed = 400,
        FetchProjectSheet_FindSheetsFailed = 401,
        FetchProjectSheet_GetSheetFailed = 402,
        FetchProjectSheet_InternalHandleSheetFailed = 403,
    }

    public class UpdatingUserProjectRecordResponse : Response<UserProjectRecordSheet, UpdatingUserProjectSheetFailureReason>
    {
        public UpdatingUserProjectRecordResponse() { }

        public static UpdatingUserProjectRecordResponse Success(UserProjectRecordSheet data) => new() { Data = data };
    }
    public enum UpdatingUserProjectSheetFailureReason
    {
        None = 0,
        Unknown = 1,
        ManuallyCancel = 2,

        /* Client 操作錯誤 */
        /// <summary>
        /// 尚未連接到學習歷程 API，請先呼叫 ConnectAsync 並確保連接成功後再進行專案紀錄更新操作。
        /// </summary>
        NotConnected = 100,
        /// <summary>
        /// 正在進行專案紀錄更新，請等待當前更新完成後再嘗試進行新的更新操作，以避免衝突或資料不一致。
        /// </summary>
        ProjectSheetUpdateInProgress = 101,

        /* 後端服務錯誤 */
        /// <summary>
        /// 尋找專案紀錄失敗，可能是因為專案紀錄不存在、使用者權限不足，或後端服務異常導致無法返回專案紀錄資料。
        /// </summary>
        FindSheetsFailed = 200,
        /// <summary>
        /// 取得專案紀錄失敗，可能是因為專案紀錄不存在、使用者權限不足，或後端服務異常導致無法返回專案紀錄資料。
        /// </summary>
        GetSheetFailed = 201,

        /// <summary>
        /// 處理專案紀錄失敗，可能是因為請求格式錯誤，可能是資料格式錯誤或是資料處理途中出現異常，導致無法成功更新專案紀錄。
        /// 通常不是權限與後端服務的問題
        /// </summary>
        InternalHandleSheetFailed = 300,
    }
}
