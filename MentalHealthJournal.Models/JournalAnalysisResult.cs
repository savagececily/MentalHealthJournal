namespace MentalHealthJournal.Models
{
    public class JournalAnalysisResult
    {
        public string Sentiment { get; set; } = string.Empty;
        public double SentimentScore { get; set; }
        public List<string> KeyPhrases { get; set; } = new();
        public string Affirmation { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }
}
