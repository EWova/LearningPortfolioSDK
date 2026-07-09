using UnityEngine;
using UnityEngine.Events;

using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using EWova.Auth;
using UnityEngine.XR;
using System.Collections.Generic;

namespace EWova.LearningPortfolio
{
    [RequireComponent(typeof(EWovaLoginPlaneUI))]
    public class EWovaLoginPlane : MonoBehaviour
    {
        private static readonly Logger Logger = new("EWovaLoginPlane ", LogLevel.Full);

        public enum Status
        {
            None,

            // 不支援登入，因為 SDK 不支援 DeepLink 或其他必要功能
            // 需要透過 launch_ticket 如 EWova 應用程式 或 網頁端 來跳轉登入
            NotSupportLogin,

            CheckAvailabilityFailed,
            CheckAvailabilityProcessing,
            CheckAvailabilityOK,

            LPConnectProcessing,
            LPConnectGettingUserData,
            LPConnectOK,
        }

        public enum Page
        {
            Login = 1,
            CheckAccount = 2,
        }

        public EWovaLoginPlaneUI UI;

        [Tooltip("遊戲開始 True=有使用使用者 False=無使用使用者")]
        public UnityEvent<bool> OnGameStart;

        [Header("ReadOnly")]
        public string ReadonlySheetGuid;
        [HideInInspector]
        public Status CurrentStatus
        {
            get => _currentStatus;
            set
            {
                if (_currentStatus == value)
                    return;
                _currentStatus = value;
                Reprint();
            }
        }
        private Status _currentStatus;

        private void OnValidate()
        {
            UI = GetComponent<EWovaLoginPlaneUI>();
        }
        private void OnEnable()
        {
            if (LearningPortfolio.IsConnected)
                CurrentStatus = Status.LPConnectOK;
            else
                TryCheckingAvailability();
        }
        private void OnDisable()
        {
            CurrentStatus = Status.None;
        }
        private void Awake()
        {
            //按下連線按鈕
            UI.ConnectingButton.onClick.AddListener(TryCheckingAvailability);
            //按下登入按鈕
            UI.LoginButton.onClick.AddListener(TryLoginWithUrl);
            //按下取消登入按鈕
            UI.CancelLoginButton.onClick.AddListener(TryCancelConnect);
            // 按下取消取得使用者資料按鈕
            UI.CancelGettingUserDataButton.onClick.AddListener(TryCancelConnect);
            //切換使用者
            UI.LoginInfoChangeUserButton.onClick.AddListener(TryLogout);

            //檢視使用者學習歷程資料
            UI.CheckAccountViewLearningPortfolioButton.onClick.AddListener(() =>
            {
                LearningPortfolio.CreateUserProjectRecordShower((RectTransform)transform);
            });
            //按下開始遊戲按鈕
            UI.CheckAccountStartButton.onClick.AddListener(() =>
            {
                OnGameStart?.Invoke(true);
            });
            //按下跳過登入按鈕
            UI.LoginSkipButton.onClick.AddListener(() =>
            {
                OnGameStart?.Invoke(false);
            });
        }

        private void TryCheckingAvailability()
        {
            if (CurrentStatus == Status.CheckAvailabilityProcessing)
            {
                if (Logger.WarnEnabled)
                    Logger.Warn("正在檢查專案可用性，請勿重複點擊");
                return;
            }

            if (CurrentStatus != Status.None && CurrentStatus != Status.CheckAvailabilityFailed)
            {
                if (Logger.WarnEnabled)
                    Logger.Warn("正在檢查專案可用性或已經連接，無法重複檢查");
                return;
            }

            CurrentStatus = Status.CheckAvailabilityProcessing;

            CheckAvailabilityProcess process = new();
            process.OnCompleted += (result) =>
            {
                try
                {
                    if (result.IsSuccess)
                    {
                        CurrentStatus = Status.CheckAvailabilityOK;
                    }
                    else
                    {
                        if (result.IsManuallyCancel)
                        {
                            if (Logger.InfoEnabled)
                                Logger.Info("使用者取消流程");

                            UI.SetLoginStateText("已取消登入", LogType.Log);
                            CurrentStatus = Status.None;
                        }
                        else if (result.Status == CheckAvailabilityStatus.PlatformNotSupportLogin)
                        {
                            if (Logger.WarnEnabled)
                                Logger.Warn("目前環境不支援跳轉登入，請檢查相關設定，否則將無法登入使用學習歷程相關功能");
                            CurrentStatus = Status.NotSupportLogin;
                        }
                        else
                        {
                            if (Logger.ErrorEnabled)
                            {
                                bool serErr = !string.IsNullOrEmpty(result.ServerErrorMessage);
                                bool cliErr = !string.IsNullOrEmpty(result.ClientErrorMessage);
                                string str = $"無法取得專案資料 Status:{result.Status}";
                                if (serErr || cliErr)
                                {
                                    str += ", ErrorMsg ";
                                    if (serErr)
                                        str += $"Server:{result.ServerErrorMessage} ";
                                    if (cliErr)
                                        str += $"Client:{result.ClientErrorMessage} ";
                                }
                                Logger.Err(str);
                            }

                            if (result.Exception != null)
                                Debug.LogException(result.Exception);

                            // 面向使用者的錯誤訊息
                            // TODO: 待本地化
                            switch (result.Status)
                            {
                                case CheckAvailabilityStatus.DefaultSettingsLoad:
                                    UI.SetLoginStateText("系統初始化失敗，請嘗試重新開啟應用程式。\n若問題持續，請聯絡開發團隊。", LogType.Error);
                                    break;
                                case CheckAvailabilityStatus.ApiCheckApiHealth or
                                    CheckAvailabilityStatus.ApiGetApiKeyValidInfo:
                                    UI.SetLoginStateText("目前無法連接到伺服器，請檢查您的網路連線，或稍後再試。\n若問題持續，請聯絡 EWova 團隊。", LogType.Error);
                                    break;
                                case CheckAvailabilityStatus.CheckApiKeyInvalid or
                                    CheckAvailabilityStatus.GetProject:
                                    UI.SetLoginStateText("專案初始化失敗，請嘗試重新開啟應用程式。\n若問題持續，請聯絡開發團隊。", LogType.Error);
                                    break;
                                default:
                                    if (!string.IsNullOrEmpty(result.ServerErrorMessage))
                                        UI.SetLoginStateText($"系統發生未知的錯誤，請稍後再試。\n除錯資訊 Status:{result.Status} {result.ServerErrorMessage}", LogType.Error);
                                    else
                                        UI.SetLoginStateText($"系統發生未知的錯誤，請稍後再試。\n除錯資訊 Status:{result.Status}", LogType.Error);
                                    break;
                            }
                            CurrentStatus = Status.None;
                        }
                    }
                }
                catch (Exception ex)
                {
                    CurrentStatus = Status.CheckAvailabilityFailed;
                    UI.SetLoginStateText("系統發生未知的錯誤，請稍後再試。", LogType.Error);
                    if (Logger.ErrorEnabled)
                        Logger.Err("檢查專案可用性時發生例外");
                    Debug.LogException(ex);
                }
            };
            LearningPortfolio.CheckAvailability(process, destroyCancellationToken);
        }
        private CancellationTokenSource _loginHandler;
        private void TryCancelConnect()
        {
            _loginHandler?.Cancel();
            _loginHandler?.Dispose();
            _loginHandler = null;
        }
        // 連線處理中，但尚未完成取得使用者資料，用於在 UI 上顯示連線處理中的使用者資訊 (此時連線尚未完成)
        private Auth.UserProfile _connectingPendingUserData = null;
        private void TryLoginWithUrl()
        {
            if (CurrentStatus == Status.CheckAvailabilityProcessing)
            {
                if (Logger.WarnEnabled)
                    Logger.Warn("正在檢查專案可用性，尚未準備好連線，請稍後");
                return;
            }

            if (CurrentStatus is Status.LPConnectProcessing or Status.LPConnectGettingUserData)
            {
                if (Logger.WarnEnabled)
                    Logger.Warn("正在登入中，請勿重複呼叫");
                return;
            }

            if (CurrentStatus != Status.CheckAvailabilityOK)
            {
                if (Logger.WarnEnabled)
                    Logger.Warn("尚未準備好連線，無法登入");
                return;
            }

            CurrentStatus = Status.LPConnectProcessing;

            TryCancelConnect();

            _loginHandler = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            ConnectProcess process = new();
            process.OnProgressChanged += (progress) =>
            {
                UI.GettingUserDataStateText.text = progress.ToString("P0");
            };
            process.OnStatusChanged += (status) =>
            {
                if (status == ConnectStatus.UserAuthFlowOK)
                {
                    _connectingPendingUserData = process.PendingAuthUserProfile;
                    CurrentStatus = Status.LPConnectGettingUserData;
                }
            };
            process.OnCompleted += (result) =>
            {
                try
                {
                    if (result.IsSuccess)
                    {
                        UI.ClearLoginStateText();
                        CurrentStatus = Status.LPConnectOK;
                    }
                    else
                    {
                        if (result.IsManuallyCancel)
                        {
                            if (Logger.InfoEnabled)
                                Logger.Info("使用者取消了登入流程");

                            UI.SetLoginStateText("已取消登入。", LogType.Log);
                            CurrentStatus = Status.CheckAvailabilityOK;
                        }
                        else
                        {
                            if (result.Exception != null)
                                Debug.LogException(result.Exception);

                            if (Logger.ErrorEnabled)
                            {
                                bool serErr = !string.IsNullOrEmpty(result.ServerErrorMessage);
                                bool cliErr = !string.IsNullOrEmpty(result.ClientErrorMessage);
                                string str = $"無法取得專案資料 Cause:{result.Status}";
                                if (serErr || cliErr)
                                {
                                    str += ", ErrorMsg ";
                                    if (serErr)
                                        str += $"Server:{result.ServerErrorMessage} ";
                                    if (cliErr)
                                        str += $"Client:{result.ClientErrorMessage} ";
                                }
                                Logger.Err(str);
                            }

                            var cause = result.Status;
                            // 面向使用者的錯誤訊息
                            // TODO: 待本地化
                            switch (cause)
                            {
                                case ConnectStatus.ConnectBlockedByCustomLogic:
                                    UI.SetLoginStateText($"目前不允許連線，請稍後再試。若問題持續，請聯絡開發團隊。\n{result.ClientErrorMessage}", LogType.Error);
                                    break;

                                case ConnectStatus.CheckAvailability_DefaultSettingsLoad:
                                    UI.SetLoginStateText("系統初始化失敗，請嘗試重新開啟應用程式。\n若問題持續，請聯絡開發團隊。", LogType.Error);
                                    break;

                                // 網路與連線錯誤
                                case ConnectStatus.CheckAvailability_ApiCheckApiHealth:
                                case ConnectStatus.CheckAvailability_ApiGetApiKeyValidInfo:
                                    UI.SetLoginStateText("目前無法連接到伺服器，請檢查您的網路連線稍後再試。\n若問題持續，請聯絡 EWova 團隊。", LogType.Error);
                                    break;

                                // 使用者驗證錯誤
                                case ConnectStatus.UserAuthFlow:
                                    UI.SetLoginStateText("使用者登入失敗，請重新驗證", LogType.Error);
                                    break;

                                // 後端驗證錯誤 (對使用者來說都是軟體驗證問題)
                                case ConnectStatus.CheckAvailability_ApiKeyInvalid:
                                case ConnectStatus.CheckAvailability_GetProject:
                                    UI.SetLoginStateText("專案初始化失敗，請嘗試重新開啟應用程式。\n若問題持續，請聯絡開發團隊。", LogType.Error);
                                    break;

                                case ConnectStatus.CreateProjectUsageSheet:
                                    UI.SetLoginStateText("無法建立專案使用紀錄，或稍後再試。", LogType.Error);
                                    break;

                                case ConnectStatus.ManuallyCancel:
                                    UI.SetLoginStateText("已取消登入。", LogType.Log);
                                    break;

                                // 未知或未預期的錯誤
                                default:
                                    if (!string.IsNullOrEmpty(result.ServerErrorMessage))
                                        UI.SetLoginStateText($"系統發生未知的錯誤，請稍後再試。\n除錯資訊: {result.Status}:{result.ServerErrorMessage}", LogType.Error);
                                    else
                                        UI.SetLoginStateText($"系統發生未知的錯誤，請稍後再試。\n除錯資訊: {result.Status}", LogType.Error);
                                    break;
                            }
                            CurrentStatus = Status.CheckAvailabilityOK;
                        }
                    }
                }
                catch
                {
                    CurrentStatus = Status.CheckAvailabilityFailed;
                    throw;
                }
                finally
                {
                    TryCancelConnect();
                }
            };

            LearningPortfolio.Connect(process, cancellationToken: _loginHandler.Token);
        }
        private void TryLogout()
        {
            if (_currentStatus != Status.LPConnectOK)
            {
                if (Logger.WarnEnabled)
                    Logger.Warn("尚未登入成功，無法登出");
                return;
            }

            CurrentStatus = Status.CheckAvailabilityOK;
            LearningPortfolio.Disconnect();
        }
        private float _connectingTimer = 0f;
        private bool _isConnectingExecuting = false;
        private void Update()
        {
            if (CurrentStatus == Status.LPConnectProcessing)
            {
                if (!_isConnectingExecuting)
                    _isConnectingExecuting = true;

                _connectingTimer += Time.deltaTime;

                // 8 秒後，若使用者仍在等待登入，則顯示提示訊息 ( 僅 PC 端顯示，行動裝置不顯示 )
                if (_connectingTimer > 8f)
                {
                    UI.LoginRedirectPCIssueTipText.SetActive(true);
                }

                // 60 秒後，若使用者仍在等待登入，則視為逾時，取消登入流程
                if (_connectingTimer > 60f)
                {
                    _connectingTimer = 0f;
                    UI.SetLoginStateText("連線逾時，請檢查網路連線或稍後再試。", LogType.Error);
                    TryCancelConnect();
                    CurrentStatus = Status.CheckAvailabilityOK;
                }
            }
            else
            {
                if (_isConnectingExecuting)
                {
                    UI.LoginRedirectPCIssueTipText.SetActive(false);
                    _connectingTimer = 0f;
                    _isConnectingExecuting = false;
                }
            }
        }
        private void Reprint()
        {
            UI.NotSupportLoginRoot.SetActive(false);

            UI.LoginRoot.SetActive(false);
            UI.ReconnectButton.interactable = false;
            UI.ReconnectButton.gameObject.SetActive(false);
            UI.ConnectingButton.interactable = false;
            UI.ConnectingButton.gameObject.SetActive(false);
            UI.LoginButton.interactable = false;
            UI.LoginButton.gameObject.SetActive(false);
            UI.LoginSkipButton.interactable = false;
            UI.LoginSkipButton.gameObject.SetActive(false);

            UI.LoginRedirectRoot.SetActive(false);

            UI.GettingUserDataRoot.SetActive(false);

            UI.CheckAccountRoot.SetActive(false);
            UI.CheckAccountViewLearningPortfolioButton.gameObject.SetActive(false);
            UI.CheckAccountStartButton.gameObject.SetActive(false);

            UI.LoginInfoRoot.SetActive(false);
            UI.LoginInfoChangeUserButton.interactable = false;
            UI.LoginInfoAccountOrg.text = string.Empty;
            UI.LoginInfoAccountName.text = string.Empty;

            switch (_currentStatus)
            {
                case Status.None:
                    UI.LoginRoot.SetActive(true);
                    UI.ReconnectButton.gameObject.SetActive(true);
                    UI.ReconnectButton.interactable = true;
                    UI.LoginSkipButton.gameObject.SetActive(true);
                    UI.LoginSkipButton.interactable = true;
                    break;

                case Status.NotSupportLogin:
                    UI.LoginRoot.SetActive(true);
                    UI.NotSupportLoginRoot.SetActive(true);
                    UI.LoginSkipButton.gameObject.SetActive(true);
                    UI.LoginSkipButton.interactable = true;
                    break;

                case Status.CheckAvailabilityFailed:
                    UI.LoginRoot.SetActive(true);
                    UI.ReconnectButton.gameObject.SetActive(true);
                    UI.LoginSkipButton.gameObject.SetActive(true);
                    UI.LoginSkipButton.interactable = true;
                    break;

                case Status.CheckAvailabilityProcessing:
                    UI.SetLoginStateText("");
                    UI.LoginRoot.SetActive(true);
                    UI.ConnectingButton.gameObject.SetActive(true);
                    UI.LoginSkipButton.gameObject.SetActive(true);
                    UI.LoginSkipButton.interactable = true;
                    break;

                case Status.CheckAvailabilityOK:
                    UI.LoginRoot.SetActive(true);
                    UI.LoginButton.gameObject.SetActive(true);
                    UI.LoginButton.interactable = true;
                    UI.LoginSkipButton.gameObject.SetActive(true);
                    UI.LoginSkipButton.interactable = true;
                    break;

                case Status.LPConnectProcessing:
                    UI.SetLoginStateText("");
                    UI.LoginRoot.SetActive(true);
                    UI.LoginRedirectRoot.SetActive(true);
                    break;

                case Status.LPConnectGettingUserData:
                    UI.LoginInfoRoot.SetActive(true);
                    UI.LoginInfoAccountOrg.text = _connectingPendingUserData.OrgName;
                    UI.LoginInfoAccountName.text = _connectingPendingUserData.Name;

                    UI.SetLoginStateText("");
                    UI.LoginRoot.SetActive(true);
                    UI.GettingUserDataRoot.SetActive(true);
                    break;

                case Status.LPConnectOK:
                    UI.LoginInfoRoot.SetActive(true);
                    UI.LoginInfoAccountOrg.text = LearningPortfolio.LoginUserData.OrgName;
                    UI.LoginInfoAccountName.text = LearningPortfolio.LoginUserData.Name;
                    UI.LoginInfoChangeUserButton.interactable = true;

                    UI.CheckAccountRoot.SetActive(true);
                    UI.CheckAccountViewLearningPortfolioButton.gameObject.SetActive(true);
                    UI.CheckAccountStartButton.gameObject.SetActive(true);
                    break;
            }
        }
    }
}