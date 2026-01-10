using MentalHealthJournal.Models;
using MentalHealthJournal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace MentalHealthJournal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    public class JournalController : ControllerBase
    {
        private readonly ILogger<JournalController> _logger;
        private readonly IJournalAnalysisService _analysisService;
        private readonly ISpeechToTextService _speechService;
        private readonly IBlobStorageService _blobService;
        private readonly ICosmosDbService _cosmosService;
        private readonly IDataExportService _exportService;
        private readonly IStreakService _streakService;

        public JournalController(ILogger<JournalController> logger, 
            IJournalAnalysisService analysisService, 
            ISpeechToTextService speechService, 
            IBlobStorageService blobService,
            ICosmosDbService cosmosService,
            IDataExportService exportService,
            IStreakService streakService)
        {
            _logger = logger;
            _analysisService = analysisService;
            _speechService = speechService;
            _blobService = blobService;
            _cosmosService = cosmosService;
            _exportService = exportService;
            _streakService = streakService;
            
            _logger.LogInformation("JournalController initialized");
        }

        [HttpGet("health")]
        [AllowAnonymous] // Allow health check without authentication
        public IActionResult Health()
        {
            _logger.LogInformation("Health check endpoint called");
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

        [HttpGet]
        public async Task<ActionResult<List<JournalEntry>>> GetEntries(CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("GET /api/journal called with userId: {UserId}", userId);
            
            try
            {

                var entries = await _cosmosService.GetEntriesForUserAsync(userId, cancellationToken);
                
                _logger.LogInformation("Retrieved {Count} journal entries for user {UserId}", entries.Count, userId);
                
                return Ok(entries);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Service error retrieving journal entries for user {UserId}", userId);
                return StatusCode(503, "Service temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving journal entries for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving journal entries.");
            }
        }

        [HttpPost("analyze")]
        public async Task<ActionResult<JournalEntry>> AnalyzeEntry([FromBody] JournalEntryRequest request, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Received journal entry analysis request");
            
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Request body is null");
                    return BadRequest("Request body is required.");
                }
                
                _logger.LogInformation("Processing journal entry for user {UserId}", userId);

                string entryText = request.Text ?? "";
                string? blobUrl = request.AudioBlobUrl;
                bool isVoice = request.IsVoiceEntry;

                if (string.IsNullOrWhiteSpace(entryText))
                {
                    _logger.LogWarning("No text content provided for user {UserId}", userId);
                    return BadRequest("No text provided.");
                }

                // Validate text length (max 10,000 characters)
                if (entryText.Length > 10000)
                {
                    _logger.LogWarning("Text too long for user {UserId}: {Length} characters", userId, entryText.Length);
                    return BadRequest("Text exceeds maximum length of 10,000 characters.");
                }

                _logger.LogInformation("Starting AI analysis for user {UserId}, text length: {Length}", userId, entryText.Length);
                JournalAnalysisResult analysis = await _analysisService.AnalyzeAsync(entryText, cancellationToken);
                _logger.LogInformation("AI analysis completed for user {UserId}, sentiment: {Sentiment}", userId, analysis.Sentiment);

                JournalEntry journal = new();
                journal.userId = userId;
                journal.Timestamp = request.Timestamp;
                journal.Text = entryText;
                journal.IsVoiceEntry = isVoice;
                journal.AudioBlobUrl = blobUrl;
                journal.Sentiment = analysis.Sentiment;
                journal.SentimentScore = analysis.SentimentScore;
                journal.KeyPhrases = analysis.KeyPhrases;
                journal.Summary = analysis.Summary;
                journal.Affirmation = analysis.Affirmation;

                // Save to Cosmos DB
                await _cosmosService.SaveJournalEntryAsync(journal, cancellationToken);
                
                // Update streak asynchronously (don't await to avoid blocking the response)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _streakService.UpdateUserStreakAsync(userId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating streak for user {UserId} after entry creation", userId);
                    }
                });
                _logger.LogInformation("Successfully processed and saved journal entry for user {UserId}", request.UserId);

                return Ok(journal);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input for journal entry analysis for user {UserId}", userId);
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Service error processing journal entry for user {UserId}", userId);
                return StatusCode(503, "AI service temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing journal entry for user {UserId}", request?.UserId);
                return StatusCode(500, "An error occurred while processing the journal entry.");
            }
        }

        [HttpPost("voice")]
        public async Task<ActionResult<object>> ProcessVoiceEntry([FromForm] IFormFile audioFile, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Received voice entry request for user {UserId}", userId);
            
            try
            {
                if (audioFile == null || audioFile.Length == 0)
                {
                    _logger.LogWarning("No audio file provided");
                    return BadRequest("Audio file is required.");
                }

                // Validate file size (max 10MB)
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (audioFile.Length > maxFileSize)
                {
                    _logger.LogWarning("Audio file too large: {Size} bytes", audioFile.Length);
                    return BadRequest($"Audio file exceeds maximum size of {maxFileSize / (1024 * 1024)}MB.");
                }

                // Validate file type
                var allowedContentTypes = new[] { "audio/webm", "audio/wav", "audio/mp3", "audio/mpeg", "audio/ogg" };
                if (!allowedContentTypes.Contains(audioFile.ContentType.ToLower()))
                {
                    _logger.LogWarning("Invalid audio file type: {ContentType}", audioFile.ContentType);
                    return BadRequest("Invalid audio file type. Supported formats: WebM, WAV, MP3, OGG.");
                }

                _logger.LogInformation("Processing audio file for user {UserId}, size: {Size} bytes", userId, audioFile.Length);

                // Upload audio to blob storage
                string blobUrl = await _blobService.UploadAudioAsync(audioFile, userId, cancellationToken);
                _logger.LogInformation("Audio uploaded to blob storage: {BlobUrl}", blobUrl);

                // Transcribe audio to text
                string transcription = await _speechService.TranscribeAsync(audioFile, cancellationToken);
                _logger.LogInformation("Audio transcribed, text length: {Length}", transcription.Length);

                return Ok(new
                {
                    transcription = transcription,
                    audioBlobUrl = blobUrl
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Service error processing voice entry for user {UserId}", userId);
                return StatusCode(503, "Speech or storage service temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing voice entry for user {UserId}", userId);
                return StatusCode(500, "An error occurred while processing the voice entry.");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<JournalEntry>> UpdateEntry(string id, [FromBody] UpdateJournalEntryRequest request, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Received update request for entry {EntryId} from user {UserId}", id, userId);

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Text))
                {
                    _logger.LogWarning("Invalid update request for entry {EntryId}", id);
                    return BadRequest("Entry text is required.");
                }

                // Validate text length (max 10,000 characters)
                if (request.Text.Length > 10000)
                {
                    _logger.LogWarning("Text too long for entry {EntryId}: {Length} characters", id, request.Text.Length);
                    return BadRequest("Text exceeds maximum length of 10,000 characters.");
                }

                // Get existing entry to verify ownership
                var existingEntry = await _cosmosService.GetJournalEntryByIdAsync(id, userId, cancellationToken);
                if (existingEntry == null)
                {
                    _logger.LogWarning("Entry {EntryId} not found for user {UserId}", id, userId);
                    return NotFound("Journal entry not found.");
                }

                // Re-analyze with Azure AI for updated sentiment
                _logger.LogInformation("Re-analyzing updated entry {EntryId} for user {UserId}", id, userId);
                JournalAnalysisResult analysis = await _analysisService.AnalyzeAsync(request.Text, cancellationToken);
                _logger.LogInformation("Re-analysis completed for entry {EntryId}, sentiment: {Sentiment}", id, analysis.Sentiment);

                // Update the entry with new text and analysis results
                existingEntry.Text = request.Text;
                existingEntry.Sentiment = analysis.Sentiment;
                existingEntry.SentimentScore = analysis.SentimentScore;
                existingEntry.KeyPhrases = analysis.KeyPhrases;
                existingEntry.Summary = analysis.Summary;
                existingEntry.Affirmation = analysis.Affirmation;
                // Note: Keep original Timestamp, IsVoiceEntry, and AudioBlobUrl

                var updatedEntry = await _cosmosService.UpdateJournalEntryAsync(existingEntry, cancellationToken);
                _logger.LogInformation("Successfully updated entry {EntryId} for user {UserId}", id, userId);

                return Ok(updatedEntry);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input for entry update {EntryId}", id);
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Service error updating entry {EntryId} for user {UserId}", id, userId);
                return StatusCode(503, "Service temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating entry {EntryId} for user {UserId}", id, userId);
                return StatusCode(500, "An error occurred while updating the journal entry.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEntry(string id, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Received delete request for entry {EntryId} from user {UserId}", id, userId);

            try
            {
                // Verify entry exists and belongs to user before deleting
                var existingEntry = await _cosmosService.GetJournalEntryByIdAsync(id, userId, cancellationToken);
                if (existingEntry == null)
                {
                    _logger.LogWarning("Entry {EntryId} not found for deletion by user {UserId}", id, userId);
                    return NotFound("Journal entry not found.");
                }

                await _cosmosService.DeleteJournalEntryAsync(id, userId, cancellationToken);
                _logger.LogInformation("Successfully deleted entry {EntryId} for user {UserId}", id, userId);

                // Update streak as part of the request, but treat failures as non-fatal
                try
                {
                    await _streakService.UpdateUserStreakAsync(userId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating streak for user {UserId} after entry deletion", userId);
                }

                return NoContent(); // 204 No Content is standard for successful DELETE
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Service error deleting entry {EntryId} for user {UserId}", id, userId);
                return StatusCode(503, "Service temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting entry {EntryId} for user {UserId}", id, userId);
                return StatusCode(500, "An error occurred while deleting the journal entry.");
            }
        }

        [HttpGet("export/{format}")]
        public async Task<IActionResult> ExportData(string format, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Export request for user {UserId}, format: {Format}", userId, format);

            try
            {
                string content;
                string contentType;
                string fileName;

                switch (format.ToLower())
                {
                    case "json":
                        content = await _exportService.ExportToJsonAsync(userId, cancellationToken);
                        contentType = "application/json";
                        fileName = $"mental-health-journal-export-{DateTime.UtcNow:yyyy-MM-dd}.json";
                        break;

                    case "csv":
                        content = await _exportService.ExportToCsvAsync(userId, cancellationToken);
                        contentType = "text/csv";
                        fileName = $"mental-health-journal-export-{DateTime.UtcNow:yyyy-MM-dd}.csv";
                        break;

                    default:
                        return BadRequest("Invalid export format. Supported formats: json, csv");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(content);
                _logger.LogInformation("Export completed for user {UserId}, format: {Format}, size: {Size} bytes", userId, format, bytes.Length);

                return File(bytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data for user {UserId}, format: {Format}", userId, format);
                return StatusCode(500, "An error occurred while exporting your data.");
            }
        }

        [HttpGet("calendar")]
        public async Task<IActionResult> GetCalendarEntries(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Default to current month if no dates provided
            var start = startDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).ToUniversalTime();
            var end = endDate ?? new DateTime(
                start.Year,
                start.Month,
                DateTime.DaysInMonth(start.Year, start.Month),
                23, 59, 59,
                start.Kind);

            _logger.LogInformation("Calendar request for user {UserId}, startDate: {Start}, endDate: {End}", userId, start, end);

            try
            {
                var entries = await _cosmosService.GetEntriesForUserByDateRangeAsync(userId, start, end, cancellationToken);
                
                // Group entries by date for easier calendar rendering
                var groupedEntries = entries
                    .GroupBy(e => e.Timestamp.Date)
                    .Select(g => new
                    {
                        date = g.Key,
                        count = g.Count(),
                        entries = g.Select(e => new
                        {
                            id = e.id,
                            timestamp = e.Timestamp,
                            sentiment = e.Sentiment,
                            sentimentScore = e.SentimentScore,
                            summary = e.Summary
                        }).ToList()
                    })
                    .OrderBy(g => g.date)
                    .ToList();

                _logger.LogInformation("Retrieved {Count} days with entries for user {UserId}", groupedEntries.Count, userId);
                
                return Ok(groupedEntries);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Service error retrieving calendar entries for user {UserId}", userId);
                return StatusCode(503, "Service temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving calendar entries for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving calendar entries.");
            }
        }

        [HttpGet("streak")]
        public async Task<IActionResult> GetStreak(CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Streak request for user {UserId}", userId);

            try
            {
                var (currentStreak, longestStreak) = await _streakService.CalculateStreaksAsync(userId, cancellationToken);
                
                _logger.LogInformation("Retrieved streak for user {UserId}: Current={Current}, Longest={Longest}", 
                    userId, currentStreak, longestStreak);
                
                return Ok(new
                {
                    currentStreak,
                    longestStreak,
                    calculatedAt = DateTime.UtcNow
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Service error retrieving streak for user {UserId}", userId);
                return StatusCode(503, "Service temporarily unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving streak for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving streak information.");
            }
        }
    }
}
