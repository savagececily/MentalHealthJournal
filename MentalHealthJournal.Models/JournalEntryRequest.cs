using Microsoft.AspNetCore.Http;

namespace MentalHealthJournal.Models
{
    public class JournalEntryRequest
    {
        public string? Text { get; set; }            // Optional if using voice
        public IFormFile? AudioFile { get; set; }    // Optional if using text
        public string UserId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsVoiceEntry { get; set; } = false;
        public string? AudioBlobUrl { get; set; }
    }
}
