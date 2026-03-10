using Microsoft.ML.Data;

namespace TPulse.ML.Models
{
    public class TextSentiment
    {
        [LoadColumn(0)]
        public string Text { get; set; } = string.Empty;

        [LoadColumn(1)]
        public bool Label { get; set; }
    }
}