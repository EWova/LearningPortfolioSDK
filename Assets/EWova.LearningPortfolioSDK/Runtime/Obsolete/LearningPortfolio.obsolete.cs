using System;
using System.Collections.Generic;
using System.Linq;

namespace EWova.LearningPortfolio
{
    public partial class LearningPortfolio
    {
        public partial class UserProjectRecordSheet
        {

            [Obsolete("已棄用，請使用 ProgressAllCompleteMarkedDic")]
            public IReadOnlyList<string> ProgressCompletions => AllMarkedProgressDic.Keys.ToList();
            [Obsolete("已棄用，請使用 ProgressAllCompleteMarkedDic")]
            public IReadOnlyList<DateTime> ProgressCompletionsLocalDateTime => AllMarkedProgressDic.Values.ToList();
        }
        public partial class ProgressNode 
        {
            [Obsolete("已棄用，請使用 SetMark")]
            public NetServiceVoid SetComplete => SetMark;
            [Obsolete("已棄用，請使用 IsMarked")]
            public bool IsCompletedSelf => RootSheet.AllMarkedProgressDic.ContainsKey(Path);
            [Obsolete("已棄用，請使用 MarkedTime")]
            public DateTime? CompleteTime => RootSheet.AllMarkedProgressDic.TryGetValue(Path, out var result) ? result : (DateTime?)null;
        }
    }
}