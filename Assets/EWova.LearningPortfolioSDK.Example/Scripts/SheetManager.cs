using EWova.LearningPortfolio;

using System;

using UnityEngine;

// § 6.3 創建表單 Sheet 管理
// https://wiki.ewova.com/zh-tw/LearningPortfolio/2026/Tutorial#h-63-%E5%89%B5%E5%BB%BA%E8%A1%A8%E5%96%AE-sheet-%E7%AE%A1%E7%90%86
public class SheetManager : MonoBehaviour
{
    #region § 6.3.1 創建表單 Sheet 管理
    private static SheetManager _instance;
    public static SheetManager Instance
    {
        get
        {
            if (!Application.isPlaying)
                throw new InvalidOperationException("SheetManager 只能在遊戲執行時使用");

            if (_instance == null)
            {
                GameObject go = new("SheetManager");
                _instance = go.AddComponent<SheetManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
    #endregion

    private void EnsureConnected()
    {
        if (!LearningPortfolio.IsConnected)
            throw new System.Exception("學習歷程尚未連接，請先登入連接學習歷程");
    }
    private void EnsureSheetNotUpdating()
    {
        if (LearningPortfolio.IsUpdatingUserProjectRecord)
            throw new System.Exception("學習歷程還正在上傳資訊中，稍後再試");
    }

    #region § 6.3.2 進度樹-讀寫節點標記
    /// <summary>
    /// 檢查該指定的進度節點，透過「父子關係」推算此節點是否完成
    /// </summary>
    public bool IsProgressNodeCompleted(
        ProjectScheme.ProgressNode node)
    {
        EnsureConnected();
        var currentSheet = LearningPortfolio.LoggedUserProjectRecordSheet;
        string nodePath = ProjectScheme.ProgressNodeMap[node];

        if (currentSheet.FindProgressNodeByPath(nodePath,
            out LearningPortfolio.ProgressNode foundNode))
            return foundNode.IsCompleted;

        return false;
    }

    /// <summary>
    /// 直接查詢進度標記是否有對應的 string Key，「忽略父子關係」
    /// </summary>
    public bool IsProgressNodeMarked(
        ProjectScheme.ProgressNode node)
    {
        EnsureConnected();
        var currentSheet = LearningPortfolio.LoggedUserProjectRecordSheet;
        string nodePath = ProjectScheme.ProgressNodeMap[node];

        if (currentSheet.FindProgressNodeByPath(nodePath,
            out LearningPortfolio.ProgressNode foundNode))
            return foundNode.IsMarked;

        return false;
    }

    /// <summary>
    /// 將指定的進度節點設為完成
    /// </summary>
    public void SetProgressNodeMarked(
        ProjectScheme.ProgressNode node,
        Action<bool> onFinished = null)
    {
        EnsureConnected();
        EnsureSheetNotUpdating();
        var currentSheet = LearningPortfolio.LoggedUserProjectRecordSheet;
        string nodePath = ProjectScheme.ProgressNodeMap[node];
        if (!currentSheet.FindProgressNodeByPath(nodePath,
            out LearningPortfolio.ProgressNode foundNode))
        {
            Debug.LogWarning($"找不到指定的進度節點: {node}. (path:{nodePath})");
            return;
        }
        foundNode.SetMark.Request
        (
            onSuccess: () =>
            {
                onFinished?.Invoke(true);
                Debug.Log($"已成功將進度節點設定完成標記: {node}. (path:{nodePath})");
            },
            onFailure: (msg) =>
            {
                onFinished?.Invoke(false);
                Debug.LogError($"無法將進度節點設定完成標記: {node}. (path:{nodePath})。錯誤訊息: {msg}");
            },
            onException: (ex) =>
            {
                onFinished?.Invoke(false);
                // 通常 `Api 錯誤` 與 `操作取消(OperationCanceledException)` 等預期錯誤會以 `onFailure` 的方式 `Callback`
                // 若從 `onException` 捉到例外，通常是內部程式邏輯錯誤或是 EWova 服務端等非預期錯誤
                // 持續無法解決可聯絡 EWova 官方支援
                Debug.LogException(ex);
                Debug.LogError($"無法將進度節點設定完成標記: {node}. (path:{nodePath})。發生例外請看上方");
            }
        );
    }
    /// <summary>
    /// 將指定的進度節點完成的標記移除
    /// </summary>
    public void SetProgressNodeUnmarked(
        ProjectScheme.ProgressNode node,
        Action<bool> onFinished = null)
    {
        EnsureConnected();
        EnsureSheetNotUpdating();
        var currentSheet = LearningPortfolio.LoggedUserProjectRecordSheet;
        string nodePath = ProjectScheme.ProgressNodeMap[node];
        if (!currentSheet.FindProgressNodeByPath(nodePath,
            out LearningPortfolio.ProgressNode foundNode))
        {
            Debug.LogWarning($"找不到指定的進度節點: {node}. (path:{nodePath})");
            return;
        }
        foundNode.SetUnmark.Request
        (
            onSuccess: () =>
            {
                onFinished?.Invoke(true);
                Debug.Log($"已成功將進度節點移除完成標記: {node}. (path:{nodePath})");
            },
            onFailure: (msg) =>
            {
                onFinished?.Invoke(false);
                Debug.LogError($"無法將進度節點移除完成標記: {node}. (path:{nodePath})。錯誤訊息: {msg}");
            },
            onException: (ex) =>
            {
                onFinished?.Invoke(false);
                // 通常 `Api 錯誤` 與 `操作取消(OperationCanceledException)` 等預期錯誤會以 `onFailure` 的方式 `Callback`
                // 若從 `onException` 捉到例外，通常是內部程式邏輯錯誤或是 EWova 服務端等非預期錯誤
                // 持續無法解決可聯絡 EWova 官方支援
                Debug.LogException(ex);
                Debug.LogError($"無法將進度節點移除完成標記: {node}. (path:{nodePath})。發生例外請看上方");
            }
        );
    }
    #endregion

    #region § 6.3.3 詳細資料-讀寫總覽頁面
    /// <summary>
    /// 取得指定頁面的總覽頁行資料
    /// </summary>
    public ProjectScheme.OverviewPageLevelRow GetLevelRowDataFromOverviewPage(
        ProjectScheme.Level targetLevel)
    {
        EnsureConnected();

        var currentSheet = LearningPortfolio.LoggedUserProjectRecordSheet;
        var currentPage = currentSheet.Pages[0]; // pages[0] 對應總覽頁面

        switch (targetLevel)
        {
            case ProjectScheme.Level.第一關:
            case ProjectScheme.Level.第二關:
            case ProjectScheme.Level.特殊測驗:
                return SheetHelper.CreateFromRow<ProjectScheme.OverviewPageLevelRow>(
                    currentPage.Rows[(int)targetLevel]);

            default:
                throw new System.Exception($"未知的關卡: {targetLevel}");
        }
    }
    /// <summary>
    /// 將指定頁面的總覽頁行資料覆寫 (若 overrideData 為 null，則清空該行資料)
    /// </summary>
    public void SetLevelRowDataFromOverviewPage(
        ProjectScheme.Level targetLevel,
        ProjectScheme.OverviewPageLevelRow overrideData,
        Action<bool> onFinished = null)
    {
        EnsureConnected();
        EnsureSheetNotUpdating();

        var currentSheet = LearningPortfolio.LoggedUserProjectRecordSheet;
        var currentPage = currentSheet.Pages[0]; // pages[0] 對應總覽頁面
        string[] cellValues = overrideData != null
             ? SheetHelper.AlignToColumns(overrideData, currentPage) // 將資料對齊總覽頁的欄位順序
             : new string[currentPage.Columns.Length]; // 若 overrideData 為 null，則清空該行資料

        currentPage.Rows[(int)targetLevel].SetCells.Request
        (
            request: new Api.SetRowRequest { Cells = cellValues },
            onSuccess: () =>
            {
                onFinished?.Invoke(true);
                Debug.Log($"已成功將總覽頁 {targetLevel}關卡行 資料覆寫");
            },
            onFailure: (msg) =>
            {
                onFinished?.Invoke(false);
                Debug.LogError($"無法將總覽頁 {targetLevel}關卡行 資料覆寫。錯誤訊息: {msg}");
            },
            onException: (ex) =>
            {
                onFinished?.Invoke(false);
                // 通常 `Api 錯誤` 與 `操作取消(OperationCanceledException)` 等預期錯誤會以 `onFailure` 的方式 `Callback`
                // 若從 `onException` 捉到例外，通常是內部程式邏輯錯誤或是 EWova 服務端等非預期錯誤
                // 持續無法解決可聯絡 EWova 官方支援
                Debug.LogException(ex);
                Debug.LogError($"無法將總覽頁 {targetLevel}關卡行 資料覆寫。發生例外請看上方");
            }
        );
    }
    #endregion

    #region § 6.3.4 詳細資料-讀寫個別關卡頁面
    /// <summary>
    /// 指定關卡頁面，在最後一行新增一行資料，並在完成後回傳新增的行資料 (若 writeData 為 null，則該行資料為空)
    /// </summary>
    public void AppendRowData<T>(
        T writeData,
        Action<LearningPortfolio.Row> onFinished = null) where T : ProjectScheme.LevelRowBase
    {
        EnsureConnected();
        EnsureSheetNotUpdating();

        var currentSheet = LearningPortfolio.LoggedUserProjectRecordSheet;
        var targetLevel = writeData != null ? (int)writeData.Level : (int)Activator.CreateInstance<T>().Level;
        var currentPage = currentSheet.Pages[targetLevel];

        string[] cellValues = writeData != null
            ? SheetHelper.AlignToColumns(writeData, currentPage) // 將資料對齊關卡頁的欄位順序
            : new string[currentPage.Columns.Length]; // 若 writeData 為 null，則清空該行資料

        currentPage.AddRowAndSetCells.Request
        (
            request: new Api.SetRowRequest
            {
                Cells = cellValues
            },
            onSuccess: (response) =>
            {
                onFinished?.Invoke(currentPage.Rows[response.RowIndex]);
                Debug.Log($"已成功將 {targetLevel}關卡頁面新增一行資料，索引位置為 {response.RowIndex}");
            },
            onFailure: (msg) =>
            {
                onFinished?.Invoke(null);
                Debug.LogError($"無法將 {targetLevel}關卡頁面新增一行資料。錯誤訊息: {msg}");
            },
            onException: (ex) =>
            {
                onFinished?.Invoke(null);
                Debug.LogException(ex);
                Debug.LogError($"無法將 {targetLevel}關卡頁面新增一行資料。發生例外請看上方");
            }
        );
    }
    /// <summary>
    /// 將指定關卡頁面覆寫一行資料 (若 writeData 為 null，則清空該行資料)
    /// </summary>
    public void SetRowData<T>(
        int rowIndex,
        T writeData,
        Action<bool> onFinished = null) where T : ProjectScheme.LevelRowBase
    {
        EnsureConnected();
        EnsureSheetNotUpdating();

        var currentSheet = LearningPortfolio.LoggedUserProjectRecordSheet;
        var targetLevel = writeData != null ? (int)writeData.Level : (int)Activator.CreateInstance<T>().Level;
        var currentPage = currentSheet.Pages[targetLevel];
        string[] cellValues = writeData != null
            ? SheetHelper.AlignToColumns(writeData, currentPage) // 將資料對齊關卡頁的欄位順序
            : new string[currentPage.Columns.Length]; // 若 writeData 為 null，則清空該行資料

        var targetRow = currentPage.Rows[rowIndex];
        targetRow.SetCells.Request
        (
            request: new Api.SetRowRequest { Cells = cellValues },
            onSuccess: () =>
            {
                Debug.Log($"已成功將 {targetLevel}關卡頁面行資料覆寫");
                onFinished?.Invoke(true);
            },
            onFailure: (msg) =>
            {
                Debug.LogError($"無法將 {targetLevel}關卡頁面行資料覆寫。錯誤訊息: {msg}");
                onFinished?.Invoke(false);
            },
            onException: (ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"無法將 {targetLevel}關卡頁面行資料覆寫。發生例外請看上方");
                onFinished?.Invoke(false);
            }
        );
    }
    /// <summary>
    /// 取得指定關卡頁面的一行資料
    /// </summary>
    public bool TryGetRowData<T>(int rowIndex, out T result) where T : ProjectScheme.LevelRowBase
    {
        EnsureConnected();

        var currentSheet = LearningPortfolio.LoggedUserProjectRecordSheet;

        T targetLevel = Activator.CreateInstance<T>();
        var currentPage = currentSheet.Pages[(int)targetLevel.Level];

        if (rowIndex < 0 || rowIndex >= currentPage.Rows.Count)
        {
            Debug.LogError($"無法取得 {targetLevel.Level}關卡頁面行資料，索引位置 {rowIndex} 超出範圍 (0 ~ {currentPage.Rows.Count - 1})");
            result = null;
            return false;
        }

        var targetRow = currentPage.Rows[rowIndex];

        SheetHelper.ReadFromRow(targetRow, ref targetLevel);
        result = targetLevel;
        return true;
    }
    /// <summary>
    /// 清除指定關卡頁面所有行資料
    /// </summary>
    public void ClearAllRowData<T>(
        Action<bool> onFinished = null) where T : ProjectScheme.LevelRowBase
    {
        EnsureConnected();
        EnsureSheetNotUpdating();
        var currentSheet = LearningPortfolio.LoggedUserProjectRecordSheet;
        T targetLevel = Activator.CreateInstance<T>();
        var currentPage = currentSheet.Pages[(int)targetLevel.Level];
        currentPage.ClearReadableData.Request
        (
            onSuccess: () =>
            {
                Debug.Log($"已成功將 {targetLevel.Level}關卡頁面所有行資料清除");
                onFinished?.Invoke(true);
            },
            onFailure: (msg) =>
            {
                Debug.LogError($"無法將 {targetLevel.Level}關卡頁面所有行資料清除。錯誤訊息: {msg}");
                onFinished?.Invoke(false);
            },
            onException: (ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"無法將 {targetLevel.Level}關卡頁面所有行資料清除。發生例外請看上方");
                onFinished?.Invoke(false);
            }
        );
    }
    #endregion
}
