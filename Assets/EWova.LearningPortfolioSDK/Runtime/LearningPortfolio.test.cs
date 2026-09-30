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
                FieldType.DurationSeconds => random.NextDouble().ToString(),
                FieldType.DurationMinutes => random.NextDouble().ToString(),
                FieldType.DurationMilliseconds => random.NextDouble().ToString(),
                FieldType.DateTimeOffset => DateTimeOffset.Now.ToString("o"),
                _ => random.Next().ToString()
            };

            args = (random.Next(2) == 0, fieldType, text);

            return DefaultChartCellViewRenderer(args);
        }
#endif
    }
}
