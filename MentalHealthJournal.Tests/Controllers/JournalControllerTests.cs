using MentalHealthJournal.Models;
using MentalHealthJournal.Server.Controllers;
using MentalHealthJournal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
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
        private readonly JournalController _controller;

        public JournalControllerTests()
        {
            _loggerMock = new Mock<ILogger<JournalController>>();
            _analysisServiceMock = new Mock<IJournalAnalysisService>();
            _speechServiceMock = new Mock<ISpeechToTextService>();
            _blobServiceMock = new Mock<IBlobStorageService>();
            _cosmosServiceMock = new Mock<ICosmosDbService>();

            _controller = new JournalController(
                _loggerMock.Object,
                _analysisServiceMock.Object,
                _speechServiceMock.Object,
                _blobServiceMock.Object,
                _cosmosServiceMock.Object);
        }

        [Fact]
        public async Task AnalyzeEntry_WithValidTextRequest_ReturnsOkWithJournalEntry()
        {
            // Arrange
            var request = new JournalEntryRequest
            {
                UserId = "user123",
                Text = "I had a great day today!",
                Timestamp = DateTime.UtcNow
            };

            var analysisResult = new JournalAnalysisResult
            {
                Sentiment = "Positive",
                SentimentScore = 0.8,
                KeyPhrases = new List<string> { "great", "day" },
                Summary = "Positive entry",
                Affirmation = "Keep up the positive attitude!"
            };

            _analysisServiceMock.Setup(s => s.AnalyzeAsync(request.Text, It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisResult);

            _cosmosServiceMock.Setup(s => s.SaveJournalEntryAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AnalyzeEntry(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var journalEntry = Assert.IsType<JournalEntry>(okResult.Value);
            
            Assert.Equal(request.UserId, journalEntry.UserId);
            Assert.Equal(request.Text, journalEntry.Text);
            Assert.False(journalEntry.IsVoiceEntry);
            Assert.Equal(analysisResult.Sentiment, journalEntry.Sentiment);
            Assert.Equal(analysisResult.SentimentScore, journalEntry.SentimentScore);
            Assert.Equal(analysisResult.KeyPhrases, journalEntry.KeyPhrases);
            Assert.Equal(analysisResult.Summary, journalEntry.Summary);
            Assert.Equal(analysisResult.Affirmation, journalEntry.Affirmation);
        }

        [Fact]
        public async Task AnalyzeEntry_WithValidAudioRequest_ReturnsOkWithJournalEntry()
        {
            // Arrange
            var audioFile = CreateMockAudioFile("test.wav", "audio/wav", "test content");
            var request = new JournalEntryRequest
            {
                UserId = "user123",
                AudioFile = audioFile,
                Timestamp = DateTime.UtcNow
            };

            var transcribedText = "I had a wonderful day!";
            var blobUrl = "https://test.blob.core.windows.net/audio/test.wav";
            var analysisResult = new JournalAnalysisResult
            {
                Sentiment = "Positive",
                SentimentScore = 0.9,
                KeyPhrases = new List<string> { "wonderful", "day" },
                Summary = "Very positive entry",
                Affirmation = "Your positivity is inspiring!"
            };

            _blobServiceMock.Setup(s => s.UploadAudioAsync(audioFile, request.UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(blobUrl);

            _speechServiceMock.Setup(s => s.TranscribeAsync(audioFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transcribedText);

            _analysisServiceMock.Setup(s => s.AnalyzeAsync(transcribedText, It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisResult);

            _cosmosServiceMock.Setup(s => s.SaveJournalEntryAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AnalyzeEntry(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var journalEntry = Assert.IsType<JournalEntry>(okResult.Value);
            
            Assert.Equal(request.UserId, journalEntry.UserId);
            Assert.Equal(transcribedText, journalEntry.Text);
            Assert.True(journalEntry.IsVoiceEntry);
            Assert.Equal(blobUrl, journalEntry.AudioBlobUrl);
            Assert.Equal(analysisResult.Sentiment, journalEntry.Sentiment);
        }

        [Fact]
        public async Task AnalyzeEntry_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.AnalyzeEntry(null!);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Request body is required.", badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeEntry_WithEmptyUserId_ReturnsBadRequest()
        {
            // Arrange
            var request = new JournalEntryRequest
            {
                UserId = "",
                Text = "Some text",
                Timestamp = DateTime.UtcNow
            };

            // Act
            var result = await _controller.AnalyzeEntry(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("User ID is required.", badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeEntry_WithNoTextOrAudio_ReturnsBadRequest()
        {
            // Arrange
            var request = new JournalEntryRequest
            {
                UserId = "user123",
                Timestamp = DateTime.UtcNow
            };

            // Act
            var result = await _controller.AnalyzeEntry(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("No text or audio provided.", badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeEntry_WhenAnalysisServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            var request = new JournalEntryRequest
            {
                UserId = "user123",
                Text = "Test text",
                Timestamp = DateTime.UtcNow
            };

            _analysisServiceMock.Setup(s => s.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Analysis service error"));

            // Act
            var result = await _controller.AnalyzeEntry(request);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.Equal("An error occurred while processing the journal entry.", statusResult.Value);
        }

        [Fact]
        public async Task GetUserEntries_WithValidUserId_ReturnsOkWithEntries()
        {
            // Arrange
            var userId = "user123";
            var entries = new List<JournalEntry>
            {
                CreateTestJournalEntry("entry1", userId),
                CreateTestJournalEntry("entry2", userId)
            };

            _cosmosServiceMock.Setup(s => s.GetEntriesForUserAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entries);

            // Act
            var result = await _controller.GetUserEntries(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedEntries = Assert.IsType<List<JournalEntry>>(okResult.Value);
            
            Assert.Equal(2, returnedEntries.Count);
            Assert.All(returnedEntries, entry => Assert.Equal(userId, entry.UserId));
        }

        [Fact]
        public async Task GetUserEntries_WithEmptyUserId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetUserEntries("");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("User ID is required.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetUserEntries_WhenCosmosServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            var userId = "user123";
            _cosmosServiceMock.Setup(s => s.GetEntriesForUserAsync(userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetUserEntries(userId);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.Equal("An error occurred while retrieving journal entries.", statusResult.Value);
        }

        [Fact]
        public async Task GetUserEntries_WithNoEntries_ReturnsOkWithEmptyList()
        {
            // Arrange
            var userId = "user123";
            var entries = new List<JournalEntry>();

            _cosmosServiceMock.Setup(s => s.GetEntriesForUserAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entries);

            // Act
            var result = await _controller.GetUserEntries(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedEntries = Assert.IsType<List<JournalEntry>>(okResult.Value);
            
            Assert.Empty(returnedEntries);
        }

        private static IFormFile CreateMockAudioFile(string fileName, string contentType, string content)
        {
            var mock = new Mock<IFormFile>();
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);

            mock.Setup(f => f.FileName).Returns(fileName);
            mock.Setup(f => f.ContentType).Returns(contentType);
            mock.Setup(f => f.Length).Returns(bytes.Length);
            mock.Setup(f => f.OpenReadStream()).Returns(stream);

            return mock.Object;
        }

        private static JournalEntry CreateTestJournalEntry(string id, string userId)
        {
            return new JournalEntry
            {
                Id = id,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                Text = "Test journal entry",
                IsVoiceEntry = false,
                Sentiment = "Positive",
                SentimentScore = 0.8,
                KeyPhrases = new List<string> { "test", "journal" },
                Summary = "Test summary",
                Affirmation = "Test affirmation"
            };
        }
    }
}
