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

        public JournalController(ILogger<JournalController> logger, IJournalAnalysisService analysisService, ISpeechToTextService speechService, IBlobStorageService blobService)
        {
            _logger = logger;
            _analysisService = analysisService;
            _speechService = speechService;
            _blobService = blobService;
        }

        [HttpPost("analyze")]
        public async Task<ActionResult<JournalAnalysisResult>> AnalyzeEntry([FromBody] JournalEntryRequest request)
        {
            string entryText = request.Text ?? "";

            string? blobUrl = null;
            bool isVoice = false;

            if (request.AudioFile != null)
            {
                isVoice = true;
                blobUrl = await _blobService.UploadAudioAsync(request.AudioFile, request.UserId);
                entryText = await _speechService.TranscribeAsync(request.AudioFile);
            }

            if (string.IsNullOrWhiteSpace(entryText))
            {
                return BadRequest("No text or audio provided.");
            }

            JournalAnalysisResult analysis = await _analysisService.AnalyzeAsync(entryText);

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

            // TODO: Save to Cosmos DB (next step)

            return Ok(journal);
        }
    }
}
