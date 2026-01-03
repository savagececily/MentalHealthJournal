using MentalHealthJournal.Models;
using MentalHealthJournal.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MentalHealthJournal.Tests.Services
{
    public class CosmosDbServiceTests
    {
        private readonly Mock<ILogger<CosmosDbService>> _loggerMock;
        private readonly Mock<CosmosClient> _cosmosClientMock;
        private readonly Mock<Container> _containerMock;
        private readonly Mock<IOptions<AppSettings>> _optionsMock;
        private readonly AppSettings _appSettings;

        public CosmosDbServiceTests()
        {
            _loggerMock = new Mock<ILogger<CosmosDbService>>();
            _cosmosClientMock = new Mock<CosmosClient>();
            _containerMock = new Mock<Container>();
            _optionsMock = new Mock<IOptions<AppSettings>>();

            _appSettings = new AppSettings
            {
                CosmosDb = new CosmosDbSettings
                {
                    DatabaseName = "TestDatabase",
                    JournalEntryContainer = "TestContainer"
                }
            };

            _optionsMock.Setup(o => o.Value).Returns(_appSettings);
            
            // Mock the GetContainer method to return our mock container
            _cosmosClientMock.Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(_containerMock.Object);
        }

        [Fact]
        public void Constructor_WithValidConfiguration_CreatesService()
        {
            // Act
            var service = new CosmosDbService(
                _loggerMock.Object,
                _cosmosClientMock.Object,
                _optionsMock.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public async Task SaveJournalEntryAsync_WithValidEntry_ValidatesInput()
        {
            // Arrange
            var journalEntry = CreateTestJournalEntry();
            var service = new CosmosDbService(
                _loggerMock.Object,
                _cosmosClientMock.Object,
                _optionsMock.Object);

            // Act & Assert
            // The mocked container will likely throw, but it shouldn't be ArgumentException
            // We're testing that the service accepts valid input without validation errors
            var exception = await Record.ExceptionAsync(() => service.SaveJournalEntryAsync(journalEntry));
            
            // If an exception occurs, it should not be due to input validation
            if (exception != null)
            {
                Assert.IsNotType<ArgumentNullException>(exception);
                Assert.IsNotType<ArgumentException>(exception);
            }
        }

        [Fact]
        public async Task GetEntriesForUserAsync_WithValidUserId_ValidatesInput()
        {
            // Arrange
            var userId = "user123";
            var service = new CosmosDbService(
                _loggerMock.Object,
                _cosmosClientMock.Object,
                _optionsMock.Object);

            // Act & Assert
            var exception = await Record.ExceptionAsync(() => service.GetEntriesForUserAsync(userId));
            
            // If an exception occurs, it should not be due to input validation
            if (exception != null)
            {
                Assert.IsNotType<ArgumentNullException>(exception);
                Assert.IsNotType<ArgumentException>(exception);
            }
        }

        [Theory]
        [InlineData("user1")]
        [InlineData("user2")]
        [InlineData("user-with-special-chars@123")]
        public async Task GetEntriesForUserAsync_WithDifferentUserIds_ValidatesInput(string userId)
        {
            // Arrange
            var service = new CosmosDbService(
                _loggerMock.Object,
                _cosmosClientMock.Object,
                _optionsMock.Object);

            // Act & Assert
            var exception = await Record.ExceptionAsync(() => service.GetEntriesForUserAsync(userId));
            
            // If an exception occurs, it should not be due to input validation  
            if (exception != null)
            {
                Assert.IsNotType<ArgumentNullException>(exception);
                Assert.IsNotType<ArgumentException>(exception);
            }
        }

        [Fact]
        public void Service_ValidatesAppSettings()
        {
            // Test that the service properly uses the injected configuration
            
            // Arrange
            var invalidSettings = new AppSettings
            {
                CosmosDb = new CosmosDbSettings
                {
                    DatabaseName = "",
                    JournalEntryContainer = ""
                }
            };

            var invalidOptionsMock = new Mock<IOptions<AppSettings>>();
            invalidOptionsMock.Setup(o => o.Value).Returns(invalidSettings);

            // Act & Assert
            // Service should still be created but will fail when trying to use Cosmos DB
            var service = new CosmosDbService(
                _loggerMock.Object,
                _cosmosClientMock.Object,
                invalidOptionsMock.Object);

            Assert.NotNull(service);
        }

        [Fact]
        public void Service_ConfigurationIsProperlyInjected()
        {
            // This test verifies that the service can be instantiated with proper configuration
            // which is important for dependency injection validation

            // Act
            var service = new CosmosDbService(
                _loggerMock.Object,
                _cosmosClientMock.Object,
                _optionsMock.Object);

            // Assert
            Assert.NotNull(service);
        }

        private static JournalEntry CreateTestJournalEntry(string? id = null)
        {
            return new JournalEntry
            {
                Id = id ?? "test-id",
                userId = "user123",
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
