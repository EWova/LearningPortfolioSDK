using System;
using System.Collections.Generic;

namespace EWova.LearningPortfolio.BasicAssets
{
    /// <summary>
    /// Scheme + Manager 範例樣板：定義本專案與後台對齊的「進度節點」與「頁面/欄位」結構，
    /// 搭配 <see cref="SheetManager"/> 使用。
    /// 請依照自己專案的後台樣板調整下方的進度節點、頁面與欄位，
    /// 或使用 LearningPortfolioProfile Inspector 的「產生 C# Scheme」直接覆寫本檔案。
    /// </summary>
    public static class ProjectScheme
    {
        /// <summary>
        /// 進度節點
        /// </summary>
        public enum ProgressNode
        {
            完成教材,
            第一關,
        }

        /// <summary>
        /// 進度節點對應的路徑
        /// </summary>
        public readonly static IReadOnlyDictionary<ProgressNode, string> ProgressNodeMap =
            new Dictionary<ProgressNode, string>()
            {
                [ProgressNode.完成教材] = "clear",
                [ProgressNode.第一關] = "clear/level1",
            };

        /// <summary>
        /// 頁面 (與後台對齊)
        /// </summary>
        public enum Page
        {
            總覽 = 0,
            第一關 = 1,
        }
        /// <summary>
        /// 關卡 (與後台對齊，總覽以外的頁面)
        /// </summary>
        public enum Level
        {
            第一關 = 1,
        }

        /// <summary>
        /// 總覽頁 (每個關卡一列)
        /// </summary>
        public class OverviewPageLevelRow
        {
            [Column("總遊玩次數")] public int TotalPlayCount;
            [Column("最佳答題成績")] public int BestTestScore;
        }

        /// <summary>
        /// 關卡頁面基底
        /// </summary>
        public abstract class LevelRowBase
        {
            public abstract Level Level { get; }
        }

        /// <summary>
        /// 第一關頁
        /// </summary>
        public class Level1PageRow : LevelRowBase
        {
            public override Level Level => Level.第一關;
            [Column("分數")] public int Score;
            [Column("是否完成關卡")] public bool IsCompletePlay;
        }
    }
}
