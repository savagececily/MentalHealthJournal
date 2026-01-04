using MentalHealthJournal.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace MentalHealthJournal.Services
{
    public class DataExportService : IDataExportService
    {
        private readonly ILogger<DataExportService> _logger;
        private readonly ICosmosDbService _cosmosService;
        private readonly IUserService _userService;

        public DataExportService(
            ILogger<DataExportService> logger,
            ICosmosDbService cosmosService,
            IUserService userService)
        {
            _logger = logger;
            _cosmosService = cosmosService;
            _userService = userService;
        }

        public async Task<string> ExportToJsonAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Exporting data to JSON for user {UserId}", userId);

                // Get user data
                var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
                var entries = await _cosmosService.GetEntriesForUserAsync(userId, cancellationToken);

                // Create export object
                var exportData = new
                {
                    ExportDate = DateTime.UtcNow,
                    User = new
                    {
                        UserId = user?.GoogleId,
                        Username = user?.Username,
                        Email = user?.Email
                    },
                    TotalEntries = entries.Count,
                    Entries = entries.Select(e => new
                    {
                        e.id,
                        Date = e.Timestamp,
                        e.Text,
                        e.IsVoiceEntry,
                        e.AudioBlobUrl,
                        e.Sentiment,
                        SentimentConfidence = e.SentimentScore,
                        e.KeyPhrases,
                        e.Summary,
                        e.Affirmation
                    }).ToList()
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(exportData, options);
                _logger.LogInformation("Successfully exported {Count} entries to JSON for user {UserId}", entries.Count, userId);

                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to JSON for user {UserId}", userId);
                throw;
            }
        }

        public async Task<string> ExportToCsvAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Exporting data to CSV for user {UserId}", userId);

                var entries = await _cosmosService.GetEntriesForUserAsync(userId, cancellationToken);

                var csv = new StringBuilder();
                
                // Header
                csv.AppendLine("Entry ID,Date,Text,Is Voice Entry,Audio URL,Sentiment,Sentiment Score,Key Phrases,Summary,Affirmation");

                // Data rows
                foreach (var entry in entries)
                {
                    csv.AppendLine(
                        $"\"{entry.id}\"," +
                        $"\"{entry.Timestamp:yyyy-MM-dd HH:mm:ss}\"," +
                        $"\"{EscapeCsv(entry.Text)}\"," +
                        $"\"{entry.IsVoiceEntry}\"," +
                        $"\"{entry.AudioBlobUrl}\"," +
                        $"\"{entry.Sentiment}\"," +
                        $"\"{entry.SentimentScore:F4}\"," +
                        $"\"{string.Join("; ", entry.KeyPhrases)}\"," +
                        $"\"{EscapeCsv(entry.Summary)}\"," +
                        $"\"{EscapeCsv(entry.Affirmation)}\""
                    );
                }

                _logger.LogInformation("Successfully exported {Count} entries to CSV for user {UserId}", entries.Count, userId);

                return csv.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to CSV for user {UserId}", userId);
                throw;
            }
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // Escape quotes and remove newlines for CSV compatibility
            return value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", "");
        }
    }
}
