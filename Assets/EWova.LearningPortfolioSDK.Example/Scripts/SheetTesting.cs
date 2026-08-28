using EWova.LearningPortfolio;

using System;

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
                TotalPlayCount = UnityEngine.Random.Range(1, 20),
                TotalPlayTime = TimeSpan.FromMinutes(UnityEngine.Random.Range(1, 60)),
                BestTestScore = UnityEngine.Random.Range(0, 100),
            };
            SheetManager.Instance.SetLevelRowDataFromOverviewPage(ProjectScheme.Level.第一關, data);
        }
        [Button("[總覽頁面] 清除 第一關資料")]
        public void ClearOverviewLevel1Data()
        {
            SheetManager.Instance.SetLevelRowDataFromOverviewPage(ProjectScheme.Level.第一關, null);
        }

        [Button("[總覽頁面] 寫入 第二關資料", Space = 5f)]
        public void SetOverviewLevel2Data()
        {
            var data = new ProjectScheme.OverviewPageLevelRow
            {
                TotalPlayCount = UnityEngine.Random.Range(1, 20),
                TotalPlayTime = TimeSpan.FromMinutes(UnityEngine.Random.Range(1, 60)),
                BestTestScore = UnityEngine.Random.Range(0, 100),
            };
            SheetManager.Instance.SetLevelRowDataFromOverviewPage(ProjectScheme.Level.第二關, data);
        }
        [Button("[總覽頁面] 清除 第二關資料")]
        public void ClearOverviewLevel2Data()
        {
            SheetManager.Instance.SetLevelRowDataFromOverviewPage(ProjectScheme.Level.第二關, null);
        }

        [Button("[總覽頁面] 寫入 特殊測驗資料", Space = 5f)]
        public void SetOverviewSpData()
        {
            var data = new ProjectScheme.OverviewPageLevelRow
            {
                TotalPlayCount = UnityEngine.Random.Range(1, 20),
                TotalPlayTime = TimeSpan.FromMinutes(UnityEngine.Random.Range(1, 60)),
                BestTestScore = UnityEngine.Random.Range(0, 100),
            };
            SheetManager.Instance.SetLevelRowDataFromOverviewPage(ProjectScheme.Level.特殊測驗, data);
        }
        [Button("[總覽頁面] 清除 特殊測驗資料")]
        public void ClearOverviewSpData()
        {
            SheetManager.Instance.SetLevelRowDataFromOverviewPage(ProjectScheme.Level.特殊測驗, null);
        }
        #endregion

        #region § 6.4.4 個別關卡頁面驗證
        private int _level1RowIndex = -1;
        private int _level2RowIndex = -1;
        private int _spRowIndex = -1;

        #region 第一關
        [Button("[第一關] 新增 一列並寫入資料", Space = 20f)]
        public void AddLevel1Data()
        {
            var data = new ProjectScheme.Level1PageRow
            {
                Score = UnityEngine.Random.Range(0, 100),
                StartPlay = DateTime.Now,
                PlayTime = TimeSpan.FromMinutes(UnityEngine.Random.Range(1, 10)),
                IsCompletePlay = true,
                TestScore = UnityEngine.Random.Range(0, 100),
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
            SheetManager.Instance.SetRowData<ProjectScheme.Level1PageRow>(_level1RowIndex, null);
            _level1RowIndex = -1;
        }
        [Button("[第一關] 清除所有 資料")]
        public void ClearAllLevel1Data()
        {
            SheetManager.Instance.ClearAllRowData<ProjectScheme.Level1PageRow>();
        }
        #endregion

        #region 第二關
        [Button("[第二關] 新增 一列並寫入資料", Space = 20f)]
        public void AddLevel2Data()
        {
            var data = new ProjectScheme.Level2PageRow
            {
                Score = UnityEngine.Random.Range(0, 100),
                StartPlay = DateTime.Now,
                PlayTime = TimeSpan.FromMinutes(UnityEngine.Random.Range(1, 10)),
                IsCompletePlay = true,
                TestScore = UnityEngine.Random.Range(0, 100),
            };

            SheetManager.Instance.AppendRowData(data, row => _level2RowIndex = row?.Index ?? -1);
        }

        [Button("[第二關] 清除 最後新增列的資料")]
        public void ClearLevel2Data()
        {
            if (_level2RowIndex < 0)
            {
                Debug.LogWarning("尚未新增過第二關資料");
                return;
            }
            SheetManager.Instance.SetRowData<ProjectScheme.Level2PageRow>(_level2RowIndex, null);
            _level2RowIndex = -1;
        }
        [Button("[第二關] 清除所有 資料")]
        public void ClearAllLevel2Data()
        {
            SheetManager.Instance.ClearAllRowData<ProjectScheme.Level2PageRow>();
        }
        #endregion

        #region 特殊測驗
        [Button("[特殊測驗] 新增 一列並寫入資料", Space = 20f)]
        public void AddSpData()
        {
            var data = new ProjectScheme.SpPageRow
            {
                Score = UnityEngine.Random.Range(0, 100),
                StartPlay = DateTime.Now,
                PlayTime = TimeSpan.FromMinutes(UnityEngine.Random.Range(1, 10)),
                IsCompletePlay = true,
                TestScore = UnityEngine.Random.Range(0, 100),
            };

            SheetManager.Instance.AppendRowData(data, row => _spRowIndex = row?.Index ?? -1);
        }

        [Button("[特殊測驗] 清除 最後新增列的資料")]
        public void ClearSpData()
        {
            if (_spRowIndex < 0)
            {
                Debug.LogWarning("尚未新增過特殊測驗資料");
                return;
            }
            SheetManager.Instance.SetRowData<ProjectScheme.SpPageRow>(_spRowIndex, null);
            _spRowIndex = -1;
        }

        [Button("[特殊測驗] 清除所有 資料")]
        public void ClearAllSpData()
        {
            SheetManager.Instance.ClearAllRowData<ProjectScheme.SpPageRow>();
        }
        #endregion
        #endregion
    }
}