using MentalHealthJournal.Models;
using MentalHealthJournal.Server.Controllers;
using MentalHealthJournal.Services;
using MentalHealthJournal.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace MentalHealthJournal.Tests.Controllers
{
    public class JournalControllerTests
    {
        private readonly Mock<ILogger<JournalController>> _loggerMock;
        private readonly Mock<IJournalAnalysisService> _analysisServiceMock;
        private readonly Mock<ISpeechToTextService> _speechServiceMock;
        private readonly Mock<IBlobStorageService> _blobServiceMock;
        private readonly Mock<ICosmosDbService> _cosmosServiceMock;
        private readonly Mock<IDataExportService> _exportServiceMock;
        private readonly Mock<IStreakService> _streakServiceMock;
        private readonly JournalController _controller;
        private const string TestUserId = "test-user-123";

        public JournalControllerTests()
        {
            _loggerMock = new Mock<ILogger<JournalController>>();
            _analysisServiceMock = new Mock<IJournalAnalysisService>();
            _speechServiceMock = new Mock<ISpeechToTextService>();
            _blobServiceMock = new Mock<IBlobStorageService>();
            _cosmosServiceMock = new Mock<ICosmosDbService>();
            _exportServiceMock = new Mock<IDataExportService>();
            _streakServiceMock = new Mock<IStreakService>();

            _controller = new JournalController(
                _loggerMock.Object,
                _analysisServiceMock.Object,
                _speechServiceMock.Object,
                _blobServiceMock.Object,
                _cosmosServiceMock.Object,
                _exportServiceMock.Object,
                _streakServiceMock.Object);

            // Setup authenticated user
            SetupAuthenticatedUser(TestUserId);
        }

        private void SetupAuthenticatedUser(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, "test@example.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public void Health_ReturnsOk()
        {
            // Act
            var result = _controller.Health();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetEntries_WithAuthenticatedUser_ReturnsOkWithEntries()
        {
            // Arrange
            var entries = TestHelper.CreateSampleJournalEntryList(3, TestUserId);
            _cosmosServiceMock.Setup(s => s.GetEntriesForUserAsync(TestUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entries);

            // Act
            var result = await _controller.GetEntries();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedEntries = Assert.IsAssignableFrom<List<JournalEntry>>(okResult.Value);
            Assert.Equal(3, returnedEntries.Count);
            Assert.All(returnedEntries, entry => Assert.Equal(TestUserId, entry.userId));
        }

        [Fact]
        public async Task GetEntries_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange - Remove user
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.GetEntries();

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task AnalyzeEntry_WithValidTextEntry_ReturnsOkWithJournalEntry()
        {
            // Arrange
            var request = TestHelper.CreateSampleJournalRequest();
            var analysisResult = TestHelper.CreateSampleAnalysisResult();

            _analysisServiceMock.Setup(s => s.AnalyzeAsync(request.Text!, It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisResult);
            _cosmosServiceMock.Setup(s => s.SaveJournalEntryAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AnalyzeEntry(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var journalEntry = Assert.IsType<JournalEntry>(okResult.Value);
            
            Assert.Equal(TestUserId, journalEntry.userId);
            Assert.Equal(request.Text, journalEntry.Text);
            Assert.False(journalEntry.IsVoiceEntry);
            Assert.Equal(analysisResult.Sentiment, journalEntry.Sentiment);
            Assert.Equal(analysisResult.SentimentScore, journalEntry.SentimentScore);
            Assert.Equal(analysisResult.KeyPhrases, journalEntry.KeyPhrases);
            Assert.Equal(analysisResult.Summary, journalEntry.Summary);
            Assert.Equal(analysisResult.Affirmation, journalEntry.Affirmation);
        }

        [Fact]
        public async Task AnalyzeEntry_WithVoiceEntry_ReturnsOkWithJournalEntry()
        {
            // Arrange
            var request = TestHelper.CreateVoiceJournalRequest();
            var analysisResult = TestHelper.CreateSampleAnalysisResult();

            _analysisServiceMock.Setup(s => s.AnalyzeAsync(request.Text!, It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisResult);
            _cosmosServiceMock.Setup(s => s.SaveJournalEntryAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AnalyzeEntry(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var journalEntry = Assert.IsType<JournalEntry>(okResult.Value);
            
            Assert.Equal(TestUserId, journalEntry.userId);
            Assert.True(journalEntry.IsVoiceEntry);
            Assert.Equal(request.AudioBlobUrl, journalEntry.AudioBlobUrl);
        }

        [Fact]
        public async Task AnalyzeEntry_WithEmptyText_ReturnsBadRequest()
        {
            // Arrange
            var request = new JournalEntryRequest
            {
                Text = "",
                Timestamp = DateTime.UtcNow
            };

            // Act
            var result = await _controller.AnalyzeEntry(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("No text provided.", badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeEntry_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var request = TestHelper.CreateSampleJournalRequest();

            // Act
            var result = await _controller.AnalyzeEntry(request);

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task ProcessVoiceEntry_WithValidAudioFile_ReturnsOkWithTranscription()
        {
            // Arrange
            var audioFile = TestHelper.CreateMockAudioFile();
            var expectedBlobUrl = "https://test.blob.core.windows.net/audio/test.wav";
            var expectedTranscription = "This is the transcribed text.";

            _blobServiceMock.Setup(s => s.UploadAudioAsync(audioFile, TestUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedBlobUrl);
            _speechServiceMock.Setup(s => s.TranscribeAsync(audioFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTranscription);

            // Act
            var result = await _controller.ProcessVoiceEntry(audioFile);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = okResult.Value;
            
            Assert.NotNull(response);
            // Verify the response contains transcription and audioBlobUrl
            var transcription = response.GetType().GetProperty("transcription")?.GetValue(response, null);
            var audioBlobUrl = response.GetType().GetProperty("audioBlobUrl")?.GetValue(response, null);
            
            Assert.Equal(expectedTranscription, transcription);
            Assert.Equal(expectedBlobUrl, audioBlobUrl);
        }

        [Fact]
        public async Task ProcessVoiceEntry_WithNullFile_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.ProcessVoiceEntry(null!);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Audio file is required.", badRequestResult.Value);
        }

        [Fact]
        public async Task ProcessVoiceEntry_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var audioFile = TestHelper.CreateMockAudioFile();

            // Act
            var result = await _controller.ProcessVoiceEntry(audioFile);

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task ExportData_WithJsonFormat_ReturnsFileResult()
        {
            // Arrange
            var jsonContent = "{\"entries\": []}";
            _exportServiceMock.Setup(s => s.ExportToJsonAsync(TestUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(jsonContent);

            // Act
            var result = await _controller.ExportData("json");

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/json", fileResult.ContentType);
            Assert.Contains(".json", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task ExportData_WithCsvFormat_ReturnsFileResult()
        {
            // Arrange
            var csvContent = "Id,Date,Text\n1,2024-01-01,Test";
            _exportServiceMock.Setup(s => s.ExportToCsvAsync(TestUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(csvContent);

            // Act
            var result = await _controller.ExportData("csv");

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("text/csv", fileResult.ContentType);
            Assert.Contains(".csv", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task ExportData_WithInvalidFormat_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.ExportData("invalid");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid export format", badRequestResult.Value?.ToString());
        }
    }
}
