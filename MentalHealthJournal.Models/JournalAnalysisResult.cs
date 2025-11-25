namespace MentalHealthJournal.Models
{
    public class JournalAnalysisResult
    {
        public string Sentiment { get; set; }
        public double SentimentScore { get; set; }
        public List<string> KeyPhrases { get; set; }
        public string Affirmation { get; set; }
        public string Summary { get; set; }
    }
}
