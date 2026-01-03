
namespace MentalHealthJournal.Models
{
    public class JournalEntry
    {
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string userId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Text { get; set; } 
        public bool IsVoiceEntry { get; set; }
        public string? AudioBlobUrl { get; set; }
        public string Sentiment { get; set; } = string.Empty;
        public double SentimentScore { get; set; }
        public List<string> KeyPhrases { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public string Affirmation { get; set; } = string.Empty;
    }
}

