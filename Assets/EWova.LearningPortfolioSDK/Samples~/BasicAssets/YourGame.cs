using UnityEngine;
using UnityEngine.UI;

using TMPro;
using System;
using System.Linq;
using Cysharp.Threading.Tasks;

using EWova.LearningPortfolio;

public class YourGame : MonoBehaviour
{
    public TextMeshProUGUI LoginOrg;
    public TextMeshProUGUI LoginName;
    public Button ViewUserProjectRecordButton;
    public Button BackToMenuButton;

    private ProjectRecordShower closeHandler;

    private void Awake()
    {
        // 從 Template.cs 邏輯來到接續的此 YourGame.cs 腳本
        if (LearningPortfolio.IsLoggedIn)
        {
            LoginOrg.text = LearningPortfolio.LoginUserData.OrgName;
            LoginName.text = LearningPortfolio.LoginUserData.Name;
        }
        else
        {
            LoginName.text = LoginOrg.text = "<color=#888><i>未使用登入</i></color>";
        }

        // 設定返回按鈕的點擊事件
        BackToMenuButton.onClick.AddListener(BackToScene);
        // 設定查看使用者學習歷程紀錄
        ViewUserProjectRecordButton.onClick.AddListener(() =>
        {
            if (closeHandler != null)
            {
                closeHandler.Close();
                closeHandler = null;
            }
            closeHandler = LearningPortfolio.CreateUserProjectRecordShower((RectTransform)transform.parent.transform);
        });
    }

    [ContextMenu("[ 修改 ]")]
    public void 修改寫入資料範例()
    {
        // 沒連線 沒登入 不可寫入！
        if (!LearningPortfolio.IsLoggedIn)
            return;

        /* --------------  Sheet = 使用者紀錄  -------------------------------------------------------*/

        // 使用者登入時下載的紀錄 可以透過以下方法修改
        LearningPortfolio.UserProjectRecordSheet Sheet = LearningPortfolio.LoggedUserProjectRecordSheet;

        // 如果你好奇這個資料表是否有數值正在寫入雲端
        // 你可以這樣取得
        bool isUploading = Sheet.IsAnyNetSerivceRequesting;

        // 取得所有頁面名稱
        string[] pageLabels = Sheet.GetPagesLabel();

        /* --------------  Sheet/CompletionProgress = 使用者紀錄底下的完成進度紀錄  -------------------------------------------------------*/
        // [讀] 完成進度百分比
        float CompletionProgress = Sheet.CompletionProgress;

        // [讀] 遍歷進度節點樹狀結構
        printNode(Sheet.ProgressNode, 0);
        static void printNode(LearningPortfolio.ProgressNode progressNode, int indent)
        {
            string indentStr = string.Concat(Enumerable.Repeat("  ", indent));
            Debug.LogError($"{indentStr}{progressNode.Path}");

            if (progressNode.Children != null)
            {
                foreach (var child in progressNode.Children)
                    printNode(child, indent + 1);
            }
        }

        // [讀] 已完成的進度路徑與完成時間
        for (int i = 0; i < Sheet.ProgressCompletions.Count; i++)
        {
            string completionPath = Sheet.ProgressCompletions[i];
            DateTime completionDate = Sheet.ProgressCompletionsLocalDateTime[i];
            Debug.LogError($"完成進度 {completionPath} 完成於 {completionDate}");
        }

        // [寫] 標記已知進度路徑為完成 這會使對應的進度節點更新 (如果節點存在的話)
        if (Sheet.FindProgressNodeByPath("單元1/關卡1", out LearningPortfolio.ProgressNode node1))
        {
            node1.SetComplete.Request
            (
                onSuccess: () => { Debug.Log("'單元1/關卡1' 成功標記進度完成"); },
                onFailure: (msg) => { Debug.LogError("標記進度完成失敗 因為:" + msg); },
                onException: (ex) => { Debug.LogException(ex); }
            );
        }
        // [寫] 取消已知進度路徑完成標記 這會使對應的進度節點更新 (如果節點存在的話)
        if (Sheet.FindProgressNodeByPath("單元1/關卡1", out LearningPortfolio.ProgressNode node2)) 
        {
            node2.SetUnmark.Request
            (
                onSuccess: () => { Debug.Log("'單元1/關卡1' 成功取消完成標記"); },
                onFailure: (msg) => { Debug.LogError("取消完成標記進度失敗 因為:" + msg); },
                onException: (ex) => { Debug.LogException(ex); }
            );
        }
        // [寫] 標記某個進度路徑為完成 (如果節點不存在則不會更新節點資訊)
        Sheet.SetCompleteIncludeNonNode.Request("Extra/額外關卡",
            onSuccess: () => { Debug.Log("'Extra/額外關卡' 成功標記進度完成"); },
            onFailure: (msg) => { Debug.LogError("標記進度完成失敗 因為:" + msg); },
            onException: (ex) => { Debug.LogException(ex); }
        );
        // [寫] 取消某個進度路徑完成標記 (如果節點不存在則不會更新節點資訊)
        Sheet.SetUnmarkIncludeNonNode.Request("Extra/額外關卡",
            onSuccess: () => { Debug.Log("'Extra/額外關卡' 成功取消完成標記"); },
            onFailure: (msg) => { Debug.LogError("取消完成標記進度失敗 因為:" + msg); },
            onException: (ex) => { Debug.LogException(ex); }
        );

        // [寫] 標記進度路徑為完成 (如果節點不存在則不會更新節點資訊) 非同步寫法
        if (Sheet.FindProgressNodeByPath("單元1/關卡1", out LearningPortfolio.ProgressNode node3))
        {
            UniTask.Void(async () =>
            {
                NetServiceAsyncRespond result = await node3.SetComplete.RequestAsync();
                if (result.IsSuccess)
                    Debug.Log("'單元1/關卡1' 成功標記進度完成");
                else if (result.IsFailed)
                    Debug.LogError("標記進度完成失敗 因為:" + result.ErrorMessage);
                else if (result.IsException)
                {
                    // 標記進度完成發生例外
                    Debug.LogException(result.Exception);
                }
            });
        }

        /* --------------  Sheet/Page = Sheet底下的分頁  ---------------------------------------------*/
        // {目標頁} = 第2頁
        LearningPortfolio.Page targetPage = Sheet.Pages[1];
        // 以下範例將會使用 {目標頁} = 第2頁 來做示範


        /* --------------  Sheet/Page/欄標籤 = 分頁內的欄位主題  -------------------------------------*/
        //           序列 |  總分  |   名稱   |  體重  | 
        //            [1] |   90   |   Lucy   |   40   |
        //            [2] |   80   |   Will   |   50   |
        // 所有欄標籤
        // 範例: string[] = {"總分","名稱","體重"}
        string[] columnLabels = targetPage.GetColumnsLabel();
        // 欄標籤的數量
        // 範例: int = 3
        int columnCount = columnLabels.Length;

        targetPage.ClearReadableData.Request
        (
            onSuccess: () => { Debug.Log("成功清除可讀取的資料"); },
            onFailure: (msg) => { Debug.LogError("清除可讀取的資料失敗 因為:" + msg); },
            onException: (ex) => { Debug.LogException(ex); }
        );

        // 非同步寫法
        UniTask.Void(async () =>
        {
            NetServiceAsyncRespond result = await targetPage.ClearReadableData.RequestAsync();
            if (result.IsSuccess)
                Debug.Log("成功清除可讀取的資料");
            else if (result.IsFailed)
                Debug.LogError("清除可讀取的資料失敗 因為:" + result.ErrorMessage);
            else if (result.IsException)
            {
                // 清除可讀取的資料發生例外
                Debug.LogException(result.Exception);
            }
        });


        /* --------------   Sheet/Page/Row = 分頁內的每一筆資料，行內資料應與欄標籤對應  -------------*/
        //           序列 |  總分  |   名稱   |  體重  | 
        //            [1] |   90   |   Lucy   |   40   |
        //            [2] |   80   |   Will   |   50   |
        // *請注意 : 為了 API 直覺性 列索引從1開始
        // 當前列資料數量有多少
        // 範例: int = 2
        int rowCount = targetPage.Rows.Count;
        // 取得 {目標頁} 第1列的資料
        // 範例: string[] = {"90","Lucy","40"}
        string[] rowLabels = targetPage.Rows[1].GetCellsText();

        //          序列 |  總分  |   名稱   |  體重  | 
        //           [1] |   90   |   Lucy   |   40   |
        //           [2] |   80   |   Will   |   50   |
        //   >>>>>   [3] |   70   |   NewV   |   66   |   <<<<<<
        // 如果你想新增一列資料到該頁 {目標頁}
        targetPage.AddRowAndSetCells.Request
        (
            value: new API.SetRowRequest()
            {
                Cells = new string[] { "70", "NewV", "66", "101", "123" }
            },
            onSuccess: (response) => { Debug.Log($"成功新增寫入一筆列資料，索引位置為 {response.RowIndex}"); },
            onFailure: (msg) => { Debug.LogError("新增新列失敗 因為:" + msg); },
            onException: (ex) => { Debug.LogException(ex); }
        );

        // 非同步寫法
        UniTask.Void(async () =>
        {
            NetServiceAsyncRespond<API.AddRowResponse> result =
                await targetPage.AddRowAndSetCells.RequestAsync(new API.SetRowRequest()
                {
                    Cells = new string[] { "70", "NewV", "66", "101", "123" }
                });
            if (result.IsSuccess)
                Debug.Log($"成功新增寫入一筆列資料，索引位置為 {result.Data.RowIndex}");
            else if (result.IsFailed)
                Debug.LogError("新增新列失敗 因為:" + result.ErrorMessage);
            else if (result.IsException)
            {
                // 新增新列發生例外
                Debug.LogException(result.Exception);
            }
        });

        /* --------------   Sheet/Page/Column = 分頁內的欄，欄位不會變動  ----------------------------*/
        //           序列 |  總分  |   名稱   |  體重  | 
        //            [1] |   90   |   Lucy   |   40   |
        //            [2] |   80   |   Will   |   50   |
        // 取得 {目標頁} 第1欄的欄位設定
        // 範例: LearningPortfolio.Column = { Label = "總分", IsReadOnly = false, FieldType = "Number" }
        LearningPortfolio.Column targetColumn = targetPage.Columns[0];

        // 範例: string = 總分
        string cellsTitle = targetColumn.Label;
        // 範例: bool = false (代表不是唯獨欄)
        bool cellsIsReadOnly = targetColumn.IsReadOnly;
        // 範例: string = Number (代表這欄資料類型，目前僅參考用沒有實質用途)
        LearningPortfolio.FieldType cellsFieldType = targetColumn.FieldType;

        // 取得 {目標頁} 第1欄的所有資料
        // 範例: string[] = {"總分","90","80"}
        string[] targetCells = targetColumn.GetCellsText();
    }

    public void BackToScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Template");
    }
}
