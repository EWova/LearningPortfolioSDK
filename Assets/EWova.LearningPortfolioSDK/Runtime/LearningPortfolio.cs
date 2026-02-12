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

using EWova.NetService;
using EWova.NetService.Model;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace EWova.LearningPortfolio
{
    public partial class LearningPortfolio : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Init()
        {
            OnUserLogin = null;
            OnUserLogout = null;
            OnUserProjectRecordUpdated = null;
        }

        [Serializable]
        public struct APISettings
        {
            public string ServiceUrl;
            public string APIKey;
        }

        public static readonly Debug Debug = new(Name, Debug.Level.Error | Debug.Level.Warn);
        public static readonly string Name = $"[EWova] {nameof(LearningPortfolio)} ";
        private static LearningPortfolio Instance;
        public static bool IsLogException = true;
        public static void ConnectWithDefaultProfile(Action<LearningPortfolio> onSuccess, Action<Exception> onError = null)
        {
            if (Instance != null)
            {
                OnError(new Exception("連線失敗，已有 LearningPortfolio 已連線完成，請呼叫 LearningPortfolio.Instance。"), onError);
                return;
            }

            LearningPortfolioProfile profile = Resources.Load<LearningPortfolioProfile>("EWova/LearningPortfolioProfile");
            if (profile == null)
            {
                OnError(new("未找到 Create LearningPortfolioProfile，請在 Resources 資料夾下建立 Create Profile"), onError);
                return;
            }

            Connect(profile, onSuccess, onError);
        }
        public static void Connect(LearningPortfolioProfile profile, Action<LearningPortfolio> onSuccess, Action<Exception> onError = null)
        {
            if (Instance != null)
            {
                OnError(new Exception("連線失敗，已有 LearningPortfolio 已連線完成，請呼叫 LearningPortfolio.Instance。"), onError);
                return;
            }

            if (profile == null)
            {
                OnError(new("Profile 不可為空"), onError);
                return;
            }

            Connect(profile.APISettings, onSuccess, onError);
        }
        public static void Connect(APISettings settings, Action<LearningPortfolio> onSuccess, Action<Exception> onError = null)
        {
            if (Instance != null)
            {
                OnError(new Exception("連線失敗，已有 LearningPortfolio 已連線完成，請呼叫 LearningPortfolio.Instance。"), onError);
                return;
            }

            GetApiKeyValidInfo(
                settings.ServiceUrl
                , settings.APIKey
                , onSuccess: (valid) =>
                {
                    if (!valid.IsValid)
                    {
                        OnError(new($"連線失敗，詳細資訊:{valid.ErrorMessage}"), onError);
                        return;
                    }

                    GetProject(
                            settings.ServiceUrl
                            , settings.APIKey
                            , projectId: valid.ProjectId
                            , onSuccess: (project) =>
                            {
                                Instance = CreateInstance();
                                ConnectingProject = project;
                                CurrentAPISettings = settings;
                                Debug.Log("成功連上專案學習歷程");
                                onSuccess?.Invoke(Instance);
                            }, onError: (msg) =>
                            {
                                OnError(new($"連線失敗，詳細資訊 {msg.Message}"), onError);
                            });
                },
                onError: (ex) =>
                {
                    OnError(new("連線失敗，請檢查 API 金鑰是否正確"), onError);
                }
            );
        }
        private static LearningPortfolio CreateInstance()
        {
            GameObject gameObject = new(Name, typeof(LearningPortfolio));
            var instance = gameObject.GetComponent<LearningPortfolio>();
            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
            void PlayModeStateChanged(UnityEditor.PlayModeStateChange mode)
            {
                if (mode == UnityEditor.PlayModeStateChange.EnteredEditMode)
                {

                    UnityEditor.EditorApplication.playModeStateChanged -= PlayModeStateChanged;
                    DestroyImmediate(instance);
                    instance = null;
                }
            }
            UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChanged;
#endif
            return instance;
        }

        [SerializeField] private UserData m_loginUserData;
        private UserProjectRecordSheet m_loggedUserProjectRecord;
        private int m_projectUsageRecordTrackingId;
        [SerializeField] private NetServiceRequestHandler m_sheetNetServiceHandler;

        public static bool IsConnected => Instance != null;
        public static bool IsLoggedIn => IsConnected && Instance.m_loginUserData != null;
        public static bool IsHasUserProjectRecord => IsLoggedIn && Instance.m_loggedUserProjectRecord != null;

        public static bool IsLoginProcessing { get; private set; }
        public static bool IsUpdatingUserProjectRecord { get; private set; }

        public static APISettings CurrentAPISettings { get; private set; }
        public static API.Project ConnectingProject { get; private set; }
        public static UserData LoginUserData => IsLoggedIn ? Instance.m_loginUserData : null;
        /// <summary>
        /// 登入中的使用者專案紀錄表
        /// </summary>
        public static UserProjectRecordSheet LoggedUserProjectRecordSheet => IsLoggedIn ? Instance.m_loggedUserProjectRecord : null;

        public static event Action<UserData> OnUserLogin;
        public static event Action OnUserLogout;
        public static event Action<UserProjectRecordSheet> OnUserProjectRecordUpdated;

        private void Update()
        {
            UpdateUserProjectRecordShower();
        }

        public static void Login(string account, string password, LoginRequestData overrideData = null, Action onSuccess = null, Action<Exception> onError = null)
        {
            if (!IsConnected)
            {
                IsLoginProcessing = false;
                OnError(new("LearningPortfolio 未連接，請先呼叫 LearningPortfolio.Connect。"), onError);
                return;
            }
            if (IsLoginProcessing)
            {
                OnError(new("登入處理中，請稍後再試"), onError);
                return;
            }
            if (IsLoggedIn)
            {
                OnError(new("已登入"), onError);
                return;
            }
            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(password))
            {
                OnError(new("帳號或密碼不可為空"), onError);
                return;
            }

            account = account.Trim();
            password = password.Trim();

            Client m_client = Client.CreateEmptyUser;
            UniTask.Void(async () =>
            {
                IsLoginProcessing = true;
                bool result = await m_client.Login(account, password, keepAlive: false);
                if (result)
                    Instance.Internal_Login(m_client, overrideData, onSuccess, onError);
                else
                {
                    OnError(new("登入失敗，帳號或密碼錯誤"), onError);
                    IsLoginProcessing = false;
                }
            });
        }
        public static void Login(string token, LoginRequestData overrideData = null, Action onSuccess = null, Action<Exception> onError = null)
        {
            if (!IsConnected)
            {
                IsLoginProcessing = false;
                OnError(new("LearningPortfolio 未連接，請先呼叫 LearningPortfolio.Connect。"), onError);
                return;
            }
            if (IsLoginProcessing)
            {
                OnError(new("登入處理中，請稍後再試"), onError);
                return;
            }
            if (IsLoggedIn)
            {
                OnError(new("已登入"), onError);
                return;
            }
            if (string.IsNullOrWhiteSpace(token))
            {
                OnError(new("Token 不能為空"), onError);
                return;
            }

            token = token.Trim();

            Client m_client = Client.CreateEmptyUser;
            UniTask.Void(async () =>
            {
                IsLoginProcessing = true;
                bool result = await m_client.Login(token, keepAlive: false);
                if (result)
                    Instance.Internal_Login(m_client, overrideData, onSuccess, onError);
                else
                {
                    OnError(new("登入失敗，Token 無效或過期"), onError);
                    IsLoginProcessing = false;
                }
            });
        }
        public static void Login(Client client, LoginRequestData overrideData = null, Action onSuccess = null, Action<Exception> onError = null)
        {
            if (!IsConnected)
            {
                IsLoginProcessing = false;
                OnError(new("LearningPortfolio 未連接，請先呼叫 LearningPortfolio.Connect。"), onError);
                return;
            }
            if (IsLoginProcessing)
            {
                OnError(new("登入處理中，請稍後再試"), onError);
                return;
            }
            if (IsLoggedIn)
            {
                OnError(new("已登入"), onError);
                return;
            }

            IsLoginProcessing = true;
            Instance.Internal_Login(client, overrideData, onSuccess, onError);
        }
        private void Internal_Login(Client client, LoginRequestData requestData, Action onSuccess = null, Action<Exception> onError = null)
        {
            if (client == null)
            {
                IsLoginProcessing = false;
                OnError(new("Client 不能為空"), onError);
                return;
            }
            if (!client.IsLogin)
            {
                IsLoginProcessing = false;
                OnError(new("Client 端口未登入"), onError);
                return;
            }

            requestData ??= LoginRequestData.Create();

            UniTask.Void(async () =>
            {
                try
                {
                    IsLoginProcessing = true;
                    var cancellationToken = this.GetCancellationTokenOnDestroy();
                    bool isSuccess = await client.CheckAlive(cancellationToken);
                    if (isSuccess)
                    {
                        UserProfile userProfile = await client.GetProfile(cancellationToken);
                        OrganizationProfile orgProfile = await client.GetOrganization(cancellationToken);

                        if (userProfile != null && orgProfile != null)
                        {

                            var loginUserData = new UserData();
                            loginUserData.Name = userProfile.name;
                            loginUserData.Nickname = userProfile.nickname;
                            loginUserData.Guid = userProfile.guid.ToString();
                            loginUserData.OrgName = orgProfile.name;
                            loginUserData.OrgGuid = orgProfile.guid.ToString();

                            API.ProjectUsageRecordResponse record = await CreateProjectUsageRecord
                            (
                                ConnectingProject.Id.ToString(),
                                loginUserData.Guid,
                                new API.SetProjectUsageRecordRequest()
                                {
                                    UsingDeviceId = requestData.UsingDeviceId
                                }
                            );
                            m_projectUsageRecordTrackingId = record.TrackingID;

                            if (KeepLoginUsageRecordHeartbeatCoro != null)
                                StopCoroutine(KeepLoginUsageRecordHeartbeatCoro);
                            KeepLoginUsageRecordHeartbeatCoro = StartCoroutine(KeepLoginUsageRecordHeartbeat());

                            IsLoginProcessing = false;
                            m_loginUserData = loginUserData;
                            OnUserLogin.InvokeSafely(m_loginUserData, @throw: ex => Debug.LogException(ex, "OnUserLogout handler exception:"));
                            onSuccess?.Invoke();
                        }
                        else
                        {
                            IsLoginProcessing = false;
                            OnError(new("回傳使用者資料為空。"), onError);
                        }
                    }
                    else
                    {
                        IsLoginProcessing = false;
                        OnError(new("Client 無法保持登入狀態，請重新登入。"), onError);
                    }
                }
                catch (OperationCanceledException)
                {
                    IsLoginProcessing = false;
                    OnError(new("已取消登入操作。"), onError);
                }
                catch (Exception ex)
                {
                    IsLoginProcessing = false;
                    OnError(new("登入意外失敗。"), onError);
                    Debug.LogException(ex);
                }
            });
        }
        Coroutine KeepLoginUsageRecordHeartbeatCoro;
        IEnumerator KeepLoginUsageRecordHeartbeat()
        {
            while (true)
            {
                if (!IsLoggedIn)
                {
                    KeepLoginUsageRecordHeartbeatCoro = null;
                    yield break;
                }

                ProjectUsageRecordHeartbeat(m_projectUsageRecordTrackingId).Forget();
                yield return new WaitForSeconds(60f);
            }
        }

        public static void Logout(Action onSuccess = null, Action<Exception> onError = null)
        {
            if (IsLoginProcessing)
            {
                OnError(new("登入中，請稍後再試。"), onError);
                return;
            }

            if (!IsLoggedIn)
            {
                OnError(new("未登入。"), onError);
                return;
            }

            Instance.m_loginUserData = null;
            OnUserLogout.InvokeSafely(@throw: ex => Debug.LogException(ex, "OnUserLogout handler exception:"));
            if (Instance.m_loggedUserProjectRecord != null)
            {
                Instance.m_loggedUserProjectRecord.Dispose();
                Instance.m_loggedUserProjectRecord = null;
            }
            onSuccess?.Invoke();
        }

        public static void UpdatingUserProjectRecord(Action<UserProjectRecordSheet> onSuccess,
                                                     Action<Exception> onError)
        {
            if (!IsLoggedIn)
            {
                OnError(new("未登入，請先登入。"), onError);
                return;
            }
            if (IsUpdatingUserProjectRecord)
            {
                OnError(new("正在更新使用者紀錄，請稍後再試。"), onError);
                return;
            }

            Dictionary<string, List<Action<Texture2D>>> texResourceHandle = new();

            IsUpdatingUserProjectRecord = true;
            UniTask.Void(async () =>
            {
                UserProjectRecordSheet RESULT = null;
                try
                {
                    // 尋找所有使用者有的報表
                    List<string> FoundSheets;
                    try
                    {
                        FoundSheets = await FindSheetsAsync(ConnectingProject.Id.ToString(), Instance.m_loginUserData.Guid);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"尋找使用者紀錄失敗\nDetail:{ex.Message}", ex);
                    }
                    string TargetSheet = FoundSheets[0];

                    // 結果
                    Instance.m_sheetNetServiceHandler = new();

                    // 處理報表
                    API.Sheet _rawSheet;
                    try
                    {
                        _rawSheet = await GetSheetAsync(TargetSheet);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"讀取使用者紀錄(0)失敗\nDetail:{ex.Message}", ex);
                    }

                    RESULT = new UserProjectRecordSheet
                    {
                        Owner = Instance.m_loginUserData,
                        NetServiceHandler = Instance.m_sheetNetServiceHandler,

                        UserId = _rawSheet.UserId.ToString(),

                        Name = _rawSheet.Name,
                        SheetId = _rawSheet.Id.ToString(),
                        ProjectId = _rawSheet.ProjectId.ToString(),
                        LastUpdatedLocal = _rawSheet.LastUpdated.ToLocalTime(),
                        CompletionProgress = _rawSheet.CompletionProgress,

                        Pages = new Page[_rawSheet.PageLabels.Length]
                    };

                    RESULT.SetCompleteIncludeNonNode = new NetSerivceRequest<string>
                    (
                        requestHandler: RESULT.NetServiceHandler,
                        func: path => SetCompleteProgress
                        (
                            sheetId: RESULT.SheetId,
                            path: path
                        ),
                        newValueFunc: async path =>
                        {
                            RESULT.CompletionProgress = await GetProgressCompletion(RESULT.SheetId);

                            if (RESULT.ProgressCompletions.Contains(path))
                                return;

                            ((List<string>)RESULT.ProgressCompletions).Add(path);
                            ((List<DateTime>)RESULT.ProgressCompletionsLocalDateTime).Add(DateTime.Now);
                        }
                    );
                    RESULT.SetUnmarkIncludeNonNode = new NetSerivceRequest<string>
                    (
                        requestHandler: RESULT.NetServiceHandler,
                        func: path => SetUnmarkProgress
                        (
                            sheetId: RESULT.SheetId,
                            path: path
                        ),
                        newValueFunc: async path =>
                        {
                            RESULT.CompletionProgress = await GetProgressCompletion(RESULT.SheetId);

                            int index = ((List<string>)RESULT.ProgressCompletions).IndexOf(path);

                            if (index >= 0)
                            {
                                ((List<string>)RESULT.ProgressCompletions).RemoveAt(index);
                                ((List<DateTime>)RESULT.ProgressCompletionsLocalDateTime).RemoveAt(index);
                            }
                        }
                    );

                    ProgressNode progressNodeTemp = null;
                    float totalScoreWeight = 0f;
                    void SetProgressNode(ref ProgressNode pNode, API.ProgressNode rawNode, ProgressNode parent)
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
                            func: () => SetCompleteProgress
                            (
                                sheetId: RESULT.SheetId,
                                path: path
                            ),
                            respondFunc: async () =>
                            {
                                RESULT.CompletionProgress = await GetProgressCompletion(RESULT.SheetId);
                                if (RESULT.ProgressCompletions.Contains(path))
                                    return;

                                ((List<string>)RESULT.ProgressCompletions).Add(path);
                                ((List<DateTime>)RESULT.ProgressCompletionsLocalDateTime).Add(DateTime.Now);
                            }
                        );
                        pNode.SetUnmark = new NetSerivceVoid
                        (
                            requestHandler: RESULT.NetServiceHandler,
                            func: () => SetUnmarkProgress
                            (
                                sheetId: RESULT.SheetId,
                                path: path
                            ),
                            respondFunc: async () =>
                            {
                                RESULT.CompletionProgress = await GetProgressCompletion(RESULT.SheetId);


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

                    // 處理頁
                    for (int i = 0; i < _rawSheet.PageLabels.Length; i++)
                    {
                        int CURRENT_PAGE = i;
                        // Page
                        API.Page _rawPage;
                        try
                        {
                            _rawPage = await GetPageAsync(RESULT.SheetId, CURRENT_PAGE);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"讀取使用者紀錄 Sheet.Page{CURRENT_PAGE} 失敗\nDetail:{ex.Message}", ex);
                        }
                        Page page = RESULT.Pages[CURRENT_PAGE] = new Page
                        {
                            RootSheet = RESULT,
                            Index = CURRENT_PAGE,
                            Label = _rawPage.Label,
                            Columns = new Column[_rawPage.ColumnLabels == null ? 0 : _rawPage.ColumnLabels.Length],
                            Rows = new SortedDictionary<int, Row>(),
                            Cells = new(),
                        };
                        page.AddRow = new NetSerivceRespond<API.AddRowResponse>
                        (
                            requestHandler: RESULT.NetServiceHandler,
                            func: () => AddPageRowAsync
                            (
                                sheetId: RESULT.SheetId,
                                page: CURRENT_PAGE
                            ),
                            respondFunc: respond =>
                            {
                                return UniTask.Create(async () =>
                                {
                                    //當使用者呼叫了AddRow 則+一頁
                                    await AddRow(respond.RowIndex, 1);
                                });
                            }
                        );
                        page.AddRowAndSetCells = new NetSerivceRequestRespond<API.SetRowRequest, API.AddRowResponse>
                        (
                            requestHandler: RESULT.NetServiceHandler,
                            func: (request) => AddPageRowAsync
                            (
                                sheetId: RESULT.SheetId,
                                page: CURRENT_PAGE
                            ),
                            respondAndNewValueFunc: async tuple =>
                            {
                                //當使用者呼叫了AddRow 則+一頁
                                await AddRow(tuple.respond.RowIndex, 1);
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
                            func: () => ClearPageReadableDataAsync
                            (
                                sheetId: RESULT.SheetId,
                                page: CURRENT_PAGE
                            ),
                            respondFunc: () =>
                            {
                                page.Cells.Clear();
                                var rows = (SortedDictionary<int, Row>)page.Rows;
                                rows.Clear();
                                return UniTask.CompletedTask;
                            }
                        );

                        API.Column[] _rawColumns;
                        try
                        {
                            _rawColumns = await GetPageColumnsAsync(RESULT.SheetId, CURRENT_PAGE);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"讀取使用者紀錄 Sheet.Page{CURRENT_PAGE}.Columns({page.Columns.Length}筆) 失敗\nDetail:{ex.Message}", ex);
                        }

                        // 處理 Column 此處不具備編輯Cell能力 並長度是固定不變的
                        for (int j = 0; j < page.Columns.Length; j++)
                        {
                            API.Column _rawColumn = _rawColumns[j];
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
                            column.Edit = new NetSerivceRequest<API.SetColumnRequest>
                            (
                                requestHandler: RESULT.NetServiceHandler,
                                func: request => SetPageColumnAsync
                                (
                                    sheetId: RESULT.SheetId,
                                    page: CURRENT_PAGE,
                                    column: CURRENT_COLUMN,
                                    request: request
                                ),
                                newValueFunc: newValue =>
                                {
                                    column.FieldType = TryParseFieldType(newValue.FieldType);
                                    return UniTask.CompletedTask;
                                }
                            );
                        }

                        // 列從1開始查找獲取 0找不到東西
                        AddRow(1, _rawPage.RowCount).Forget();
                        async UniTask AddRow(int start, int count)
                        {
                            if (count == 0)
                                return;

                            List<API.Row> _rawRows;
                            try
                            {
                                _rawRows = await GetPageRowsAsync(RESULT.SheetId, CURRENT_PAGE, start, count);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"讀取使用者紀錄 Sheet.Page{CURRENT_PAGE}.Rows 失敗\nDetail:{ex.Message}", ex);
                            }

                            for (int i = 0; i < count; i++)
                            {
                                int CURRENT_ROW = i;
                                GetRow(_rawRows[CURRENT_ROW], start + CURRENT_ROW);
                            }

                            void GetRow(API.Row _rawRow, int targetIndex)
                            {
                                Row newRow = new()
                                {
                                    RootPage = page,
                                    Index = targetIndex,
                                };
                                ((SortedDictionary<int, Row>)page.Rows).Add(newRow.Index, newRow);

                                newRow.SetCells = new NetSerivceRequest<API.SetRowRequest>
                                (
                                    requestHandler: RESULT.NetServiceHandler,
                                    func: request => SetPageRowAsync
                                    (
                                        sheetId: RESULT.SheetId,
                                        page: newRow.RootPage.Index,
                                        row: newRow.Index,
                                        request: request
                                    ),
                                    newValueFunc: newValue =>
                                    {
                                        return UniTask.Create(async () =>
                                        {
                                            int index = Mathf.Min(newValue.Cells.Length, newRow.Cells.Count);
                                            for (int i = 0; i < index; i++)
                                            {
                                                var cell = newRow.Cells[i];
                                                if (cell.IsReadOnly)
                                                    continue;
                                                cell.Text = newValue.Cells[i];
                                            }
                                            await LoadCurrentPageAllColumnSummary();
                                            await LoadFirstPageColumnSummary();
                                        });
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
                            await LoadCurrentPageAllColumnSummary();

                        async UniTask LoadCurrentPageAllColumnSummary()
                        {
                            if (page.Columns.Length == 0)
                                return;

                            API.ColumnSummary[] rawColumnSummaries;
                            try
                            {
                                rawColumnSummaries = await GetPageColumnsSummaryAsync
                                (
                                    sheetId: RESULT.SheetId,
                                    page: CURRENT_PAGE
                                );
                            }
                            catch (ErrorHandleException)
                            {
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
                                API.ColumnSummary rawColumnSummary = rawColumnSummaries[i];
                                page.Columns[CURRENT_COLUMN].CellsSummary = rawColumnSummary.Label;
                            }
                        }
                    }
                    async UniTask LoadFirstPageColumnSummary()
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
                            API.ColumnSummary rawColumnSummary = await GetPageColumnSummaryAsync
                            (
                                sheetId: RESULT.SheetId,
                                page: 0,
                                column: 1
                            );
                            column.CellsSummary = rawColumnSummary.Label;
                        }
                        catch (ErrorHandleException)
                        {
                            column.CellsSummary = string.Empty;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"讀取使用者紀錄 Sheet.Page0.ColumnSummary 失敗\nDetail:{ex.Message}", ex);
                        }
                    }

                    foreach (var res in texResourceHandle)
                    {
                        if (res.Value == null || res.Value.Count == 0)
                            continue;

                        if (string.IsNullOrWhiteSpace(res.Key))
                            continue;

                        Texture2D tex = await GetTex2D(res.Key);
                        if (tex == null)
                            continue;

                        tex.wrapMode = TextureWrapMode.Clamp;

                        foreach (var apply in res.Value)
                            apply(tex);
                        RESULT.ManagedObjects.Add(tex);
                        await UniTask.Yield();
                    }

                    Instance.m_loggedUserProjectRecord = RESULT;
                    OnUserProjectRecordUpdated.InvokeSafely(Instance.m_loggedUserProjectRecord, @throw: ex => Debug.LogException(ex, "OnUserProjectRecordUpdated handler exception:"));
                    onSuccess?.Invoke(Instance.m_loggedUserProjectRecord);
                }
                catch (Exception ex)
                {
                    RESULT?.Dispose();
                    OnError(ex, onError);
                }
                finally
                {
                    IsUpdatingUserProjectRecord = false;
                }
            });
        }

        private readonly List<ProjectRecordShower> m_managedProjectRecordShowers = new();
        private bool m_loggedUserProjectRecordShowerUpdated = false;
        private void UpdateUserProjectRecordShower()
        {
            if (m_managedProjectRecordShowers.Count == 0)
                return;

            List<ProjectRecordShower> toRemove = null;
            bool isUploading = m_loggedUserProjectRecord.IsAnyNetSerivceRequesting;
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
                    InjectDataToShower(item, m_loggedUserProjectRecord);
                }
            }

            if (toRemove != null)
            {
                foreach (var item in toRemove)
                    m_managedProjectRecordShowers.Remove(item);
            }
        }
        public static ProjectRecordShower CreateUserProjectRecordShower(RectTransform rectTransform)
        {
            ProjectRecordShower plane = ProjectRecordShower.InstantiatePlane(rectTransform);

            if (Instance.m_loggedUserProjectRecord == null)
                return plane;

            Instance.m_managedProjectRecordShowers.Add(plane);
            InjectDataToShower(plane, Instance.m_loggedUserProjectRecord);
            return plane;
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
                        CellsSummaryLabel = _column.Cells.Any() ? (_column.CellsSummary != "0" ? _column.CellsSummary : string.Empty) : null,
                    }).ToArray()
                };
                plane.AddPage(page.Label, content);
            }

            plane.Footer.text = $"你的完成進度為 {(int)(userProjectRecord.CompletionProgress * 100f)}% ！";
        }

        private static void OnError(Exception ex, Action<Exception> action)
        {
            if (IsLogException)
                Debug.LogException(ex);
            action?.Invoke(ex);
        }
    }
}