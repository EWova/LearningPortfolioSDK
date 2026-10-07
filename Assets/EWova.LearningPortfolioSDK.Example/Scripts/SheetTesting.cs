using EWova.LearningPortfolio;
using EWova.LearningPortfolio.BasicAssets;

using UnityEngine;

namespace Test
{
    // § 6.4 資料驗證
    // 驗證 SheetManager 測試各關卡的新增/移除資料
    public class SheetTesting : MonoBehaviour
    {
        #region § 6.4.2 進度樹節點標記驗證
        public ProjectScheme.ProgressNode TargetNode;

        [Button("抓取 TargetNode 是否完成")]
        public void GetIsProgressNodeCompleted()
        {
            bool result = SheetManager.Instance.IsProgressNodeCompleted(TargetNode);
            Debug.Log($"TargetNode: {TargetNode}, 完成狀態: {result}");
        }
        [Button("抓取 TargetNode 是否被完成標記")]
        public void GetIsProgressNodeMarked()
        {
            bool result = SheetManager.Instance.IsProgressNodeMarked(TargetNode);
            Debug.Log($"TargetNode: {TargetNode}, 標記狀態: {result}");
        }
        [Button("設定 TargetNode 完成標記")]
        public void MarkCompletedProgressNode()
        {
            SheetManager.Instance.SetProgressNodeMarked(TargetNode);
        }
        [Button("移除 TargetNode 完成標記")]
        public void UnmarkCompletedProgressNode()
        {
            SheetManager.Instance.SetProgressNodeUnmarked(TargetNode);
        }
        #endregion

        #region § 6.4.3 總覽頁面驗證
        [Button("[總覽頁面] 寫入 第一關資料", Space = 20f)]
        public void SetOverviewLevel1Data()
        {
            var data = new ProjectScheme.OverviewPageLevelRow
            {
                總遊玩次數 = UnityEngine.Random.Range(1, 20),
                最佳答題成績 = UnityEngine.Random.Range(0, 100),
            };
            SheetManager.Instance.SetLevelRowDataFromOverviewPage(ProjectScheme.Level.第一關, data);
        }
        [Button("[總覽頁面] 清除 第一關資料")]
        public void ClearOverviewLevel1Data()
        {
            SheetManager.Instance.SetLevelRowDataFromOverviewPage(ProjectScheme.Level.第一關, null);
        }
        #endregion

        #region § 6.4.4 個別關卡頁面驗證
        private int _level1RowIndex = -1;

        [Button("[第一關] 新增 一列並寫入資料", Space = 20f)]
        public void AddLevel1Data()
        {
            var data = new ProjectScheme.第一關PageRow
            {
                分數 = UnityEngine.Random.Range(0, 100),
                是否完成關卡 = true,
            };

            SheetManager.Instance.AppendRowData(data, row => _level1RowIndex = row?.Index ?? -1);
        }

        [Button("[第一關] 清除 最後新增列的資料")]
        public void ClearLevel1Data()
        {
            if (_level1RowIndex < 0)
            {
                Debug.LogWarning("尚未新增過第一關資料");
                return;
            }
            SheetManager.Instance.SetRowData<ProjectScheme.第一關PageRow>(_level1RowIndex, null);
            _level1RowIndex = -1;
        }
        [Button("[第一關] 清除所有 資料")]
        public void ClearAllLevel1Data()
        {
            SheetManager.Instance.ClearAllRowData<ProjectScheme.第一關PageRow>();
        }
        #endregion
    }
}
