using MentalHealthJournal.Services;
using MentalHealthJournal.Tests.Helpers;
using Azure.AI.TextAnalytics;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MentalHealthJournal.Tests.Services
{
    /// <summary>
    /// Unit tests for JournalAnalysisService
    /// Note: These tests focus on input validation.
    /// Testing actual Azure AI service calls requires integration tests with test credentials.
    /// </summary>
    public class JournalAnalysisServiceTests
    {
        private readonly Mock<ILogger<JournalAnalysisService>> _loggerMock;
        private readonly Mock<TextAnalyticsClient> _textAnalyticsClientMock;
        private readonly Mock<AzureOpenAIClient> _openAIClientMock;

        public JournalAnalysisServiceTests()
        {
            _loggerMock = new Mock<ILogger<JournalAnalysisService>>();
            _textAnalyticsClientMock = new Mock<TextAnalyticsClient>();
            _openAIClientMock = new Mock<AzureOpenAIClient>();
        }

        [Fact]
        public async Task AnalyzeAsync_WithEmptyText_ThrowsArgumentException()
        {
            // Arrange
            var options = TestHelper.CreateTestOptions();
            var service = new JournalAnalysisService(
                _loggerMock.Object,
                _textAnalyticsClientMock.Object,
                _openAIClientMock.Object,
                options);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                service.AnalyzeAsync(""));
        }

        [Fact]
        public async Task AnalyzeAsync_WithNullText_ThrowsArgumentException()
        {
            // Arrange
            var options = TestHelper.CreateTestOptions();
            var service = new JournalAnalysisService(
                _loggerMock.Object,
                _textAnalyticsClientMock.Object,
                _openAIClientMock.Object,
                options);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                service.AnalyzeAsync(null!));
        }
    }
}
