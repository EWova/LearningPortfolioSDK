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
            public IReadOnlyList<string> ProgressCompletions => ProgressAllCompleteMarkedDic.Keys.ToList();
            [Obsolete("已棄用，請使用 ProgressAllCompleteMarkedDic")]
            public IReadOnlyList<DateTime> ProgressCompletionsLocalDateTime => ProgressAllCompleteMarkedDic.Values.ToList();
        }
    }
}