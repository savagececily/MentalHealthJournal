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
        }

        [HttpPost("analyze")]
        public async Task<ActionResult<JournalEntry>> AnalyzeEntry([FromBody] JournalEntryRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request body is required.");
                }

                if (string.IsNullOrWhiteSpace(request.UserId))
                {
                    return BadRequest("User ID is required.");
                }

                string entryText = request.Text ?? "";
                string? blobUrl = null;
                bool isVoice = false;

                if (request.AudioFile != null)
                {
                    isVoice = true;
                    blobUrl = await _blobService.UploadAudioAsync(request.AudioFile, request.UserId, cancellationToken);
                    entryText = await _speechService.TranscribeAsync(request.AudioFile, cancellationToken);
                }

                if (string.IsNullOrWhiteSpace(entryText))
                {
                    return BadRequest("No text or audio provided.");
                }

                JournalAnalysisResult analysis = await _analysisService.AnalyzeAsync(entryText, cancellationToken);

                JournalEntry journal = new()
                {
                    UserId = request.UserId,
                    Timestamp = request.Timestamp,
                    Text = entryText,
                    IsVoiceEntry = isVoice,
                    AudioBlobUrl = blobUrl,
                    Sentiment = analysis.Sentiment,
                    SentimentScore = analysis.SentimentScore,
                    KeyPhrases = analysis.KeyPhrases,
                    Summary = analysis.Summary,
                    Affirmation = analysis.Affirmation
                };

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

        [HttpGet("entries/{userId}")]
        public async Task<ActionResult<List<JournalEntry>>> GetUserEntries(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
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
