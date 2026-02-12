using UnityEngine;
using UnityEngine.Events;

using System.Collections.Generic;

namespace EWova.LearningPortfolio
{
    [RequireComponent(typeof(EWovaLoginPlaneUI))]
    public partial class EWovaLoginPlane : MonoBehaviour
    {
        public enum Status
        {
            NetServiceConnecting,
            NetServiceConnectedAndWaitForLogin,
            LoginProcessing,
            LoggedIn,
        }

        public enum Page
        {
            Login = 1,
            CheckAccount = 2,
        }

        private const string EWovaAccountPlayerPrefsKey = "EWovaAccount";

        public EWovaLoginPlaneUI UI;

        [Tooltip("遊戲開始 True=有使用使用者 False=無使用使用者")]
        public UnityEvent<bool> OnGameStart;

        public enum LoginWay
        {
            AccountPassword,
            Token
        }
        public UnityEvent<LoginWay> OnLogin;

        [Header("ReadOnly")]
        public string ReadonlySheetGuid;
        [HideInInspector] public Status CurrentStatus;
        [HideInInspector] public Page CurrentPage;

        private Dictionary<Page, GameObject> PageRoots;

        private void OnValidate()
        {
            UI = GetComponent<EWovaLoginPlaneUI>();
        }
        private void Awake()
        {
            LearningPortfolio.HttpDebugger.PrintLevel = Debug.Level.Error;
            LearningPortfolio.IsLogException = false;
            InitDeepLink();
            PageRoots = new()
            {
                { Page.Login, UI.LoginRoot },
                { Page.CheckAccount, UI.CheckAccountRoot },
            };
        }
        private void Start()
        {
            if (!LearningPortfolio.IsConnected || !LearningPortfolio.IsLoggedIn)
                ShowPage(Page.Login);
            else
                ShowPage(Page.CheckAccount);

            //按下連線按鈕
            UI.ConnectButton.onClick.AddListener(() =>
            {
                void SetSuccess(bool enable)
                {
                    UI.LoginButton.gameObject.SetActive(enable);
                    UI.LoginAccountInput.interactable = enable;
                    UI.LoginPasswordInput.interactable = enable;
                    UI.ConnectButton.interactable = !enable;
                    UI.ConnectButton.gameObject.SetActive(!enable);
                }

                if (LearningPortfolio.IsConnected)
                {
                    SetSuccess(true);
                }
                else
                {
                    UI.SetLoginStateText("檢查連線中...");

                    UI.LoginButton.gameObject.SetActive(false);
                    UI.LoginAccountInput.interactable = false;
                    UI.LoginPasswordInput.interactable = false;
                    UI.ConnectButton.interactable = false;

                    LearningPortfolio.ConnectWithDefaultProfile
                    (
                        onSuccess: (instance) =>
                        {
                            SetSuccess(true);
                            UI.LoginPasswordInput.Select();

                            UI.ClearLoginStateText();
                        }, onError: (msg) =>
                        {
                            SetSuccess(false);

                            UI.SetLoginStateTextWithException(msg);
                        }
                    );
                }
            });
            //按下跳過登入按鈕
            UI.LoginSkipButton.onClick.AddListener(() =>
            {
                OnGameStart?.Invoke(false);
            });
            //按下登入按鈕
            UI.LoginButton.onClick.AddListener(() =>
            {
                void Success()
                {
                    ShowPage(Page.CheckAccount);
                }
                void Failed()
                {
                    UI.LoginPasswordInput.Select();
                    UI.LoginPasswordInput.ActivateInputField();
                }

                if (string.IsNullOrWhiteSpace(UI.LoginAccountInput.text) || string.IsNullOrWhiteSpace(UI.LoginPasswordInput.text))
                {
                    UI.SetLoginStateText("帳號或密碼不可為空", LogType.Warning);
                    Failed();
                    return;
                }
                var account = UI.LoginAccountInput.text.Trim();
                var password = UI.LoginPasswordInput.text.Trim();

                UI.SetLoginStateText("登入中...");
                UI.VirtualKeyboard.Hide();

                LearningPortfolio.Login(account, password,
                    onSuccess: () =>
                    {
                        PlayerPrefs.SetString(EWovaAccountPlayerPrefsKey, account);

                        UI.SetLoginStateText("取得歷程紀錄中...");
                        LearningPortfolio.UpdatingUserProjectRecord(
                            onSuccess: (sheet) =>
                            {
                                ReadonlySheetGuid = sheet.SheetId;
                                Success();
                                OnLogin?.Invoke(LoginWay.AccountPassword);
                            }, onError: (msg) =>
                            {
                                UI.SetLoginStateTextWithException(msg);
                                Failed();
                            }
                        );
                    },
                    onError: (msg) =>
                    {
                        UI.VirtualKeyboard.Show();

                        LearningPortfolio.Logout();
                        UI.SetLoginStateTextWithException(msg);
                        Failed();
                    }
                );
            });
            //密碼顯示切換
            UI.LoginPasswordInputShow.onValueChanged.AddListener(isShow =>
            {
                UI.LoginPasswordInput.contentType = isShow ? TMPro.TMP_InputField.ContentType.Standard : TMPro.TMP_InputField.ContentType.Password;
                UI.LoginPasswordInput.ForceLabelUpdate();
            });
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
            //登出
            UI.LoginInfoChangeUserButton.onClick.AddListener(() =>
            {
                LearningPortfolio.Logout();
                ShowPage(Page.Login);
            });

            //直接按下連線按鈕
            UI.ConnectButton.onClick?.Invoke();
        }
        private void OnDestroy()
        {
            ReleaseDeepLink();
        }

        public void Login(string account, string password)
        {
            if (!string.IsNullOrWhiteSpace(account))
            {
                UI.LoginAccountInput.text = account.Trim();
            }
            if (!string.IsNullOrWhiteSpace(password))
            {
                UI.LoginPasswordInput.text = password.Trim();
            }
            UI.LoginButton.onClick?.Invoke();
        }
        public void LoginWithToken(string token)
        {
            UI.SetLoginStateText("登入中...");
            void Success()
            {
                ShowPage(Page.CheckAccount);
            }
            void Failed()
            {
                UI.LoginAccountInput.Select();
            }

            LearningPortfolio.Login(token,
                onSuccess: () =>
                {
                    UI.SetLoginStateText("取得歷程紀錄中...");
                    LearningPortfolio.UpdatingUserProjectRecord(
                        onSuccess: (sheet) =>
                        {
                            Success();
                            OnLogin?.Invoke(LoginWay.Token);
                        }, onError: (msg) =>
                        {
                            UI.SetLoginStateTextWithException(msg);
                            Failed();
                        }
                    );
                },
                onError: (msg) =>
                {
                    LearningPortfolio.Logout();
                    UI.SetLoginStateTextWithException(msg);
                    Failed();
                }
            );
        }
        public void ClearAllSavedData()
        {
            PlayerPrefs.DeleteKey(EWovaAccountPlayerPrefsKey);
        }
        private void Update()
        {
            if (!LearningPortfolio.IsConnected)
            {
                CurrentStatus = Status.NetServiceConnecting;
            }
            else
            {
                switch (CurrentPage)
                {
                    case Page.Login:
                        CurrentStatus = Status.NetServiceConnectedAndWaitForLogin;
                        break;
                    case Page.CheckAccount:
                        CurrentStatus = Status.LoggedIn;
                        break;
                    default:
                        break;
                }
            }


            if (LearningPortfolio.IsConnected && LearningPortfolio.IsLoggedIn)
            {
                UI.LoginInfoRoot.SetActive(true);
                UI.LoginInfoAccountOrg.text = LearningPortfolio.LoginUserData.OrgName;
                UI.LoginInfoAccountName.text = LearningPortfolio.LoginUserData.Name;
            }
            else
            {
                UI.LoginInfoRoot.SetActive(false);
            }

            if (LearningPortfolio.IsConnected)
            {
                bool processing = LearningPortfolio.IsLoginProcessing || LearningPortfolio.IsUpdatingUserProjectRecord;

                UI.LoginAccountInput.interactable = !processing;
                UI.LoginPasswordInput.interactable = !processing;
                UI.LoginButton.interactable = !processing;
                UI.LoginSkipButton.interactable = !processing;
            }
        }
        public void ShowPage(Page page)
        {
            foreach (var kvp in PageRoots)
                kvp.Value.SetActive(kvp.Key == page);

            UI.VirtualKeyboard.Hide();

            switch (page)
            {
                case Page.Login:
                    CurrentPage = Page.Login;
                    UI.VirtualKeyboard.Show();

                    UI.LoginStateText.text = string.Empty;
                    UI.LoginPasswordInputShow.isOn = false;

                    if (PlayerPrefs.HasKey(EWovaAccountPlayerPrefsKey))
                    {
                        UI.LoginAccountInput.text = PlayerPrefs.GetString(EWovaAccountPlayerPrefsKey);
                    }
                    else
                    {
                        UI.LoginAccountInput.Select();
                    }

                    break;
                case Page.CheckAccount:
                    CurrentPage = Page.CheckAccount;
                    UI.VirtualKeyboard.Hide();
                    break;
                default:
                    UI.VirtualKeyboard.Hide();
                    break;
            }
        }
    }
}