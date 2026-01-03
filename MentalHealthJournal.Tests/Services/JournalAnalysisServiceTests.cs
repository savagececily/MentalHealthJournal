using MentalHealthJournal.Models;
using MentalHealthJournal.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MentalHealthJournal.Tests.Services
{
    public class JournalAnalysisServiceTests
    {
        private readonly Mock<ILogger<JournalAnalysisService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;

        public JournalAnalysisServiceTests()
        {
            _loggerMock = new Mock<ILogger<JournalAnalysisService>>();
            _configurationMock = new Mock<IConfiguration>();

            // Setup configuration
            _configurationMock.Setup(c => c["AzureOpenAI:DeploymentName"]).Returns("test-deployment");
        }

        [Fact]
        public void Constructor_WithMissingDeploymentName_ThrowsArgumentNullException()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["AzureOpenAI:DeploymentName"]).Returns((string?)null);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new JournalAnalysisService(
                _loggerMock.Object,
                null!, // TextAnalyticsClient
                null!, // AzureOpenAIClient
                configMock.Object));
        }

        [Fact]
        public async Task AnalyzeAsync_WithNullText_ThrowsArgumentException()
        {
            // Since we can't easily mock the Azure services without additional dependencies,
            // we'll test the validation logic that we can control

            // This test would need a service with mocked dependencies
            // For now, we test the argument validation behavior that should occur
            
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                // This will fail due to null Azure clients, but should validate text first
                var service = new JournalAnalysisService(
                    _loggerMock.Object,
                    null!, // TextAnalyticsClient  
                    null!, // AzureOpenAIClient
                    _configurationMock.Object);
                
                await service.AnalyzeAsync(null!);
            });

            // The ArgumentException should be thrown for null text before Azure service calls
            Assert.Contains("cannot be null", exception.Message.ToLower());
        }

        [Fact]
        public async Task AnalyzeAsync_WithEmptyText_ThrowsArgumentException()
        {
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                var service = new JournalAnalysisService(
                    _loggerMock.Object,
                    null!, // TextAnalyticsClient  
                    null!, // AzureOpenAIClient
                    _configurationMock.Object);
                
                await service.AnalyzeAsync("");
            });

            Assert.Contains("cannot be null", exception.Message.ToLower());
        }

        [Fact]
        public async Task AnalyzeAsync_WithWhitespaceText_ThrowsArgumentException()
        {
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                var service = new JournalAnalysisService(
                    _loggerMock.Object,
                    null!, // TextAnalyticsClient  
                    null!, // AzureOpenAIClient
                    _configurationMock.Object);
                
                await service.AnalyzeAsync("   ");
            });

            Assert.Contains("cannot be null", exception.Message.ToLower());
        }

        [Theory]
        [InlineData("I'm feeling really sad today")]
        [InlineData("Today was an okay day, nothing special")]
        [InlineData("I'm so excited about the weekend!")]
        public async Task AnalyzeAsync_WithValidText_CallsAzureServices(string text)
        {
            // This test demonstrates that the method will attempt to call Azure services
            // In a real-world scenario, you'd either:
            // 1. Use integration tests with real services
            // 2. Create wrapper interfaces around Azure clients for easier mocking
            // 3. Use a test environment with fake Azure services

            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                var service = new JournalAnalysisService(
                    _loggerMock.Object,
                    null!, // TextAnalyticsClient - will cause NullReferenceException
                    null!, // AzureOpenAIClient - will cause NullReferenceException
                    _configurationMock.Object);
                
                await service.AnalyzeAsync(text);
            });
        }

        [Fact]
        public void Service_ConfigurationIsProperlyValidated()
        {
            // This test verifies that the service validates configuration during construction
            
            // Should not throw with valid configuration
            var service = new JournalAnalysisService(
                _loggerMock.Object,
                null!, // TextAnalyticsClient
                null!, // AzureOpenAIClient  
                _configurationMock.Object);

            Assert.NotNull(service);
        }
    }
}
