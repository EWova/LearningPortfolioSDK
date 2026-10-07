using System;

namespace EWova.LearningPortfolio
{
    public partial class LearningPortfolio
    {
#if UNITY_EDITOR
        public static ChartCellDisplay TestRandomChartCellViewRenderer((bool, FieldType, string) args)
        {
            var random = new System.Random();

            var fieldTypes = Enum.GetValues(typeof(FieldType));
            var fieldType = (FieldType)fieldTypes.GetValue(random.Next(fieldTypes.Length));

            var text = fieldType switch
            {
                FieldType.String => $"Test {random.Next()}",
                FieldType.Number => random.NextDouble().ToString(),
                FieldType.Boolean => random.Next(2) == 0 ? "true" : "false",
                FieldType.Percentage => random.NextDouble().ToString(),
                FieldType.DurationSeconds => random.Next(3_600_000).ToString(),
                FieldType.DurationMinutes => random.Next(3_600_000).ToString(),
                FieldType.DurationMilliseconds => random.Next(3_600_000).ToString(),
                FieldType.DateTimeOffset => DateTimeOffset.Now.ToString("o"),
                _ => random.Next().ToString()
            };

            args = (random.Next(2) == 0, fieldType, text);

            return DefaultChartCellViewRenderer(args);
        }
#endif
    }
}
