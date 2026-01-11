namespace MentalHealthJournal.Models
{
    public class JournalAnalysisResult
    {
        public string Sentiment { get; set; } = string.Empty;
        public double SentimentScore { get; set; }
        public List<string> KeyPhrases { get; set; } = new();
        public string Affirmation { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public bool IsCrisisDetected { get; set; }
        public string? CrisisReason { get; set; }
        public List<CrisisResource> CrisisResources { get; set; } = new();
    }
}
