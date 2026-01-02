using MentalHealthJournal.Models;
using MentalHealthJournal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MentalHealthJournal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JournalController : ControllerBase
    {
        private readonly ILogger<JournalController> _logger;
        private readonly IJournalAnalysisService _analysisService;
        private readonly ISpeechToTextService _speechService;
        private readonly IBlobStorageService _blobService;
        private readonly ICosmosDbService _cosmosService;

        public JournalController(ILogger<JournalController> logger, 
            IJournalAnalysisService analysisService, 
            ISpeechToTextService speechService, 
            IBlobStorageService blobService,
            ICosmosDbService cosmosService)
        {
            _logger = logger;
            _analysisService = analysisService;
            _speechService = speechService;
            _blobService = blobService;
            _cosmosService = cosmosService;
            
            _logger.LogInformation("JournalController initialized");
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            _logger.LogInformation("Health check endpoint called");
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

        [HttpGet]
        public async Task<ActionResult<List<JournalEntry>>> GetEntries([FromQuery] string userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("GET /api/journal called with userId: {UserId}", userId);
            
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("User ID is missing or empty in GetEntries");
                    return BadRequest("User ID is required.");
                }

                var entries = await _cosmosService.GetEntriesForUserAsync(userId, cancellationToken);
                
                _logger.LogInformation("Retrieved {Count} journal entries for user {UserId}", entries.Count, userId);
                
                return Ok(entries);
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
            _logger.LogInformation("Received journal entry analysis request");
            
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Request body is null");
                    return BadRequest("Request body is required.");
                }

                if (string.IsNullOrWhiteSpace(request.UserId))
                {
                    _logger.LogWarning("User ID is missing or empty");
                    return BadRequest("User ID is required.");
                }
                
                _logger.LogInformation("Processing journal entry for user {UserId}", request.UserId);

                string entryText = request.Text ?? "";
                string? blobUrl = request.AudioBlobUrl;
                bool isVoice = request.IsVoiceEntry;

                if (string.IsNullOrWhiteSpace(entryText))
                {
                    _logger.LogWarning("No text content provided for user {UserId}", request.UserId);
                    return BadRequest("No text provided.");
                }

                _logger.LogInformation("Starting AI analysis for user {UserId}, text length: {Length}", request.UserId, entryText.Length);
                JournalAnalysisResult analysis = await _analysisService.AnalyzeAsync(entryText, cancellationToken);
                _logger.LogInformation("AI analysis completed for user {UserId}, sentiment: {Sentiment}", request.UserId, analysis.Sentiment);

                JournalEntry journal = new();
                journal.userId = request.UserId;
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
                
                _logger.LogInformation("Successfully processed and saved journal entry for user {UserId}", request.UserId);

                return Ok(journal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing journal entry for user {UserId}", request?.UserId);
                return StatusCode(500, "An error occurred while processing the journal entry.");
            }
        }

        [HttpPost("voice")]
        public async Task<ActionResult<object>> ProcessVoiceEntry([FromForm] string userId, [FromForm] IFormFile audioFile, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Received voice entry request for user {UserId}", userId);
            
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("User ID is missing or empty");
                    return BadRequest("User ID is required.");
                }

                if (audioFile == null || audioFile.Length == 0)
                {
                    _logger.LogWarning("No audio file provided");
                    return BadRequest("Audio file is required.");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing voice entry for user {UserId}", userId);
                return StatusCode(500, "An error occurred while processing the voice entry.");
            }
        }

        [HttpGet("entries/{userId}")]
        public async Task<ActionResult<List<JournalEntry>>> GetUserEntries(string userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Received request to get entries for user {UserId}", userId);
            
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("User ID is missing or empty in GetUserEntries");
                    return BadRequest("User ID is required.");
                }

                var entries = await _cosmosService.GetEntriesForUserAsync(userId, cancellationToken);
                
                _logger.LogInformation("Retrieved {Count} journal entries for user {UserId}", entries.Count, userId);
                
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving journal entries for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving journal entries.");
            }
        }
    }
}
