using EWova.LearningPortfolio;

using System;
using System.Collections.Generic;

public static class ProjectScheme
{
    /// <summary>
    /// 進度節點
    /// </summary>
    public enum ProgressNode
    {
        完成教材,
        登入開始探索教材,
        第一關,
        第一關_體驗遊玩,
        第一關_考試測驗,
        第二關,
        第二關_測驗題,
        特殊測驗,
    }

    /// <summary>
    /// 進度節點對應的路徑
    /// </summary>
    public readonly static IReadOnlyDictionary<ProgressNode, string> ProgressNodeMap =
        new Dictionary<ProgressNode, string>()
        {
            [ProgressNode.完成教材] = "clear",
            [ProgressNode.登入開始探索教材] = "clear/start",
            [ProgressNode.第一關] = "clear/level1",
            [ProgressNode.第一關_體驗遊玩] = "clear/level1/play",
            [ProgressNode.第一關_考試測驗] = "clear/level1/test",
            [ProgressNode.第二關] = "clear/level2",
            [ProgressNode.第二關_測驗題] = "clear/level2/test",
            [ProgressNode.特殊測驗] = "clear/sp",
        };

    /// <summary>
    /// 頁面 (與後台對齊)
    /// </summary>
    public enum Page
    {
        總覽 = 0,
        第一關 = 1,
        第二關 = 2,
        特殊測驗 = 3,
    }
    /// <summary>
    /// 關卡 (與後台對齊)
    /// </summary>
    public enum Level
    {
        第一關 = 1,
        第二關 = 2,
        特殊測驗 = 3,
    }

    /// <summary>
    /// 總覽頁
    /// </summary>
    public class OverviewPageLevelRow
    {
        [Column("總遊玩次數")] public int TotalPlayCount;
        [Column("總遊玩時間")] public TimeSpan TotalPlayTime;
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
        [Column("遊玩日期")] public DateTimeOffset StartPlay;
        [Column("遊玩時長")] public TimeSpan PlayTime;
        [Column("是否完成關卡")] public bool IsCompletePlay;
        [Column("答題成績")] public int TestScore;
    }

    /// <summary>
    /// 第二關頁
    /// </summary>
    public class Level2PageRow : LevelRowBase
    {
        public override Level Level => Level.第二關;
        [Column("分數")] public int Score;
        [Column("遊玩日期")] public DateTimeOffset StartPlay;
        [Column("遊玩時長")] public TimeSpan PlayTime;
        [Column("是否完成關卡")] public bool IsCompletePlay;
        [Column("答題成績")] public int TestScore;
    }

    /// <summary>
    /// 特殊測驗頁
    /// </summary>
    public class SpPageRow : LevelRowBase
    {
        public override Level Level => Level.特殊測驗;
        [Column("分數")] public int Score;
        [Column("遊玩日期")] public DateTimeOffset StartPlay;
        [Column("遊玩時長")] public TimeSpan PlayTime;
        [Column("是否完成關卡")] public bool IsCompletePlay;
        [Column("答題成績")] public int TestScore;
    }
}
