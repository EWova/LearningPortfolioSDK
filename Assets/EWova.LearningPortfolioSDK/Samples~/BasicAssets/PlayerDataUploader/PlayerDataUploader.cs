using UnityEngine;
using System.Collections.Generic;

using EWova.LearningPortfolio;

public class PlayerDataUploader : MonoBehaviour
{
    public static PlayerDataUploader Instance { get; private set; }

    public GameObject UploadingAlert;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        UploadingAlert.SetActive(false);
    }
    private void Update()
    {
        if (!LearningPortfolio.IsLoggedIn)
            return;
        var sheet = LearningPortfolio.LoggedUserProjectRecordSheet;

        // 如果有任何網路服務正在請求寫入資料，顯示上傳中警告
        if (sheet != null)
            UploadingAlert.SetActive(sheet.IsAnyNetSerivceRequesting);
    }

    #region 1。分頁資料_清空方法

    [ContextMenu("[ 01範例_清除總覽資料 ]")]
    public void ClearOverviewData()
    {
        //總覽頁=第0頁 清除第0頁即可
        ClearPageData(page: 0);
    }

    [ContextMenu("[ 02範例_清除第一單元資料 ]")]
    public void ExampleClearData_Unit1()
    {
        ClearPageData(page: 1);
    }

    /// <summary>
    /// 清除某一分頁資料寫法
    /// </summary>
    /// <param name="page">分頁</param>
    public void ClearPageData(int page)
    {
        if (!LearningPortfolio.IsLoggedIn)
            return;

        var sheet = LearningPortfolio.LoggedUserProjectRecordSheet;

        LearningPortfolio.Page targetPage = sheet.Pages[page];
        targetPage.ClearReadableData.Request
        (
            onSuccess: () => { Debug.Log("成功清空頁所有資料"); },
            onFailure: (msg) => { Debug.LogError("清空頁資料失敗 因為:" + msg); },
            onException: (ex) => { Debug.LogException(ex); }
        );
    }

    #endregion

    #region 2。儲存格欄位_累加方法

    [ContextMenu("[ 03範例_寫入第0分頁，第2欄第1列的儲存格，存儲累加屬值1 ]")]
    public void ExampleUpdatePlayCount()//如果後台沒有這一格欄位會存儲失敗
    {
        AddValueToCell(page: 0, column: 2, row: 1, value: 1);
    }

    /// <summary>
    /// 修改寫入指定欄位數值累加
    /// </summary>
    /// <param name="page">分頁</param>
    /// <param name="column">直欄</param>
    /// <param name="row">橫列</param>
    /// <param name="value">此欄位要累加的值</param>
    public void AddValueToCell(int page, int column, int row, int value)
    {
        if (!LearningPortfolio.IsLoggedIn)
            return;

        var sheet = LearningPortfolio.LoggedUserProjectRecordSheet;

        //目標頁
        LearningPortfolio.Page targetPage = sheet.Pages[page];
        //目標頁中的目標列
        LearningPortfolio.Row pageTargetRow = targetPage.Rows[row];
        // 列資料暫存
        string[] pageTargetRowCellsLabel = pageTargetRow.GetCellsText();

        float originValue = float.TryParse(pageTargetRowCellsLabel[column], out float parsedValue) ? parsedValue : 0.0f;
        pageTargetRowCellsLabel[column] = (originValue + value).ToString(); //累加 
        pageTargetRow.SetCells.Request
        (
            new API.SetRowRequest()
            {
                Cells = pageTargetRowCellsLabel
            },
            onSuccess: () => { Debug.Log("成功寫入列資料"); },
            onFailure: (msg) => { Debug.LogError("寫入列資料失敗 因為:" + msg); },
            onException: (ex) => { Debug.LogException(ex); }
        );
    }

    #endregion

    #region 3。儲存格欄位_整列新增方法

    [ContextMenu("[ 04範例_單元一資料寫入 ]")]
    public void ExampleWriteData_Unit1()
    {
        string[] playerData = new string[] { "10", "20", "30", "40", "50", "60" };
        WriteUnitRowData(page: 1, data: playerData);
    }

    /// <summary>
    /// 在特定單元寫入一列資料
    /// </summary>
    /// <param name="page">分頁</param>
    /// <param name="data">玩家完成的單元整列資料</param>
    public void WriteUnitRowData(int page, string[] data)
    {
        if (!LearningPortfolio.IsLoggedIn)
            return;

        var Sheet = LearningPortfolio.LoggedUserProjectRecordSheet;

        LearningPortfolio.Page Page = Sheet.Pages[page];

        // 1.1 版本更新
        //新增列
        Page.AddRowAndSetCells.Request
        (
            value: new API.SetRowRequest()
            {
                Cells = data
            },
            onSuccess: (response) => { Debug.Log($"成功在第 {response.RowIndex}列 寫入新增列資料"); },
            onFailure: (msg) => { Debug.LogError("新增新列+寫入失敗 因為:" + msg); },
            onException: (ex) => { Debug.LogException(ex); }
        );
    }

    #endregion

    #region 4。教材完成進度_更新與歸零方法

    // 1.1 版本更新完成進度
    [ContextMenu("[ 05範例_教材完成進度更新 ]")]
    public void ExampleUpdateCourseProgress()
    {
        if (!LearningPortfolio.IsLoggedIn)
            return;

        var Sheet = LearningPortfolio.LoggedUserProjectRecordSheet;

        // 找不到節點就不處理
        if (!Sheet.FindProgressNodeByPath("clear/start", out LearningPortfolio.ProgressNode foundNode))
            return;

        foundNode.SetComplete.Request
        (
            onSuccess: () => { Debug.Log($"成功更新教材完成進度 {foundNode.Path}"); },
            onFailure: (msg) => { Debug.LogError($"更新教材完成進度失敗 {foundNode.Path} 因為:" + msg); },
            onException: (ex) => { Debug.LogException(ex); }
        );
    }

    // 1.1 版本更新完成進度
    [ContextMenu("[ 06範例_移除教材完成進度更新 ]")]
    public void ExampleResetCourseProgress()
    {
        if (!LearningPortfolio.IsLoggedIn)
            return;

        var Sheet = LearningPortfolio.LoggedUserProjectRecordSheet;

        // 目前完成的所有路徑
        IReadOnlyList<string> allCompletePath = Sheet.ProgressCompletions;

        // 移除所有完成進度
        foreach (var path in allCompletePath)
        {
            Sheet.SetUnmarkIncludeNonNode.Request
            (
                value: path,
                onSuccess: () => { Debug.Log($"成功取消進度完成標記 {path}"); },
                onFailure: (msg) => { Debug.LogError($"取消進度完成標記失敗 {path} 因為:" + msg); },
                onException: (ex) => { Debug.LogException(ex); }
            );
        }
    }

    #endregion
}
