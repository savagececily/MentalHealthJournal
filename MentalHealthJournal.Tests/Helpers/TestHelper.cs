using MentalHealthJournal.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;

namespace MentalHealthJournal.Tests.Helpers
{
    public static class TestHelper
    {
        public static JournalEntry CreateSampleJournalEntry(string? id = null, string userId = "testuser")
        {
            return new JournalEntry
            {
                id = id ?? Guid.NewGuid().ToString(),
                userId = userId,
                Timestamp = DateTime.UtcNow,
                Text = "This is a sample journal entry for testing purposes.",
                IsVoiceEntry = false,
                Sentiment = "Positive",
                SentimentScore = 0.8,
                KeyPhrases = new List<string> { "sample", "journal", "testing" },
                Summary = "A positive sample entry for testing",
                Affirmation = "You are doing great with your testing!"
            };
        }

        public static JournalEntry CreateVoiceJournalEntry(string? id = null, string userId = "testuser")
        {
            var entry = CreateSampleJournalEntry(id, userId);
            entry.IsVoiceEntry = true;
            entry.AudioBlobUrl = "https://test.blob.core.windows.net/audio/test-recording.wav";
            return entry;
        }

        public static JournalEntryRequest CreateSampleJournalRequest()
        {
            return new JournalEntryRequest
            {
                Text = "This is a test journal entry.",
                Timestamp = DateTime.UtcNow,
                IsVoiceEntry = false
            };
        }

        public static JournalEntryRequest CreateVoiceJournalRequest()
        {
            return new JournalEntryRequest
            {
                Text = "Transcribed text from voice entry",
                AudioBlobUrl = "https://test.blob.core.windows.net/audio/test.wav",
                Timestamp = DateTime.UtcNow,
                IsVoiceEntry = true
            };
        }

        public static JournalAnalysisResult CreateSampleAnalysisResult()
        {
            return new JournalAnalysisResult
            {
                Sentiment = "Positive",
                SentimentScore = 0.75,
                KeyPhrases = new List<string> { "happy", "productive", "grateful" },
                Summary = "This entry reflects a positive mindset with feelings of happiness and gratitude.",
                Affirmation = "You're doing wonderful! Your positive energy is inspiring and shows great emotional awareness."
            };
        }

        public static IFormFile CreateMockAudioFile(string fileName = "test-audio.wav", string contentType = "audio/wav", string content = "mock audio content")
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

        public static AppSettings CreateTestAppSettings()
        {
            return new AppSettings
            {
                AzureOpenAI = new AzureOpenAISettings
                {
                    Endpoint = "https://test-openai.openai.azure.com",
                    DeploymentName = "test-deployment"
                },
                AzureCognitiveServices = new AzureCognitiveServicesSettings
                {
                    Endpoint = "https://test-cognitive.cognitiveservices.azure.com",
                    Region = "eastus"
                },
                AzureBlobStorage = new AzureBlobStorageSettings
                {
                    ServiceUri = "https://test.blob.core.windows.net",
                    ContainerName = "test-audio"
                },
                CosmosDb = new CosmosDbSettings
                {
                    Endpoint = "https://test-cosmos.documents.azure.com:443/",
                    DatabaseName = "TestMentalHealthJournal",
                    JournalEntryContainer = "TestJournalEntries"
                }
            };
        }

        public static IOptions<AppSettings> CreateTestOptions()
        {
            return Options.Create(CreateTestAppSettings());
        }

        public static List<JournalEntry> CreateSampleJournalEntryList(int count = 3, string userId = "testuser")
        {
            var entries = new List<JournalEntry>();
            for (int i = 0; i < count; i++)
            {
                var entry = CreateSampleJournalEntry($"entry-{i}", userId);
                entry.Timestamp = DateTime.UtcNow.AddDays(-i);
                entry.Text = $"This is journal entry number {i + 1}.";
                entries.Add(entry);
            }
            return entries;
        }

        public static List<JournalAnalysisResult> CreateSampleAnalysisResultList(int count = 3)
        {
            var results = new List<JournalAnalysisResult>();
            var sentiments = new[] { "Positive", "Neutral", "Negative" };
            var scores = new[] { 0.8, 0.5, 0.2 };

            for (int i = 0; i < count; i++)
            {
                results.Add(new JournalAnalysisResult
                {
                    Sentiment = sentiments[i % sentiments.Length],
                    SentimentScore = scores[i % scores.Length],
                    KeyPhrases = new List<string> { $"keyword{i}", $"phrase{i}" },
                    Summary = $"Summary for entry {i + 1}",
                    Affirmation = $"Affirmation for entry {i + 1}"
                });
            }

            return results;
        }
    }
}
