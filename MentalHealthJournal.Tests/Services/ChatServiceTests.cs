using MentalHealthJournal.Models;
using MentalHealthJournal.Services;
using Azure.AI.OpenAI;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAI.Chat;
using Xunit;

namespace MentalHealthJournal.Tests.Services
{
    /// <summary>
    /// Unit tests for ChatService
    /// Tests focus on business logic, input validation, and error handling.
    /// Azure OpenAI and Cosmos DB operations are mocked.
    /// </summary>
    public class ChatServiceTests
    {
        private readonly Mock<ILogger<ChatService>> _loggerMock;
        private readonly Mock<AzureOpenAIClient> _openAIClientMock;
        private readonly Mock<CosmosClient> _cosmosClientMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<Container> _containerMock;
        private const string TestUserId = "test-user-123";
        private const string TestDeploymentName = "gpt-4";

        public ChatServiceTests()
        {
            _loggerMock = new Mock<ILogger<ChatService>>();
            _openAIClientMock = new Mock<AzureOpenAIClient>();
            _cosmosClientMock = new Mock<CosmosClient>();
            _configurationMock = new Mock<IConfiguration>();
            _containerMock = new Mock<Container>();

            // Setup configuration
            _configurationMock.Setup(c => c["AzureOpenAI:DeploymentName"])
                .Returns(TestDeploymentName);
            _configurationMock.Setup(c => c["CosmosDb:DatabaseName"])
                .Returns("TestDb");
            _configurationMock.Setup(c => c["CosmosDb:ChatSessionContainer"])
                .Returns("ChatSessions");

            // Setup Cosmos DB container
            _cosmosClientMock.Setup(c => c.GetContainer(
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Returns(_containerMock.Object);
        }

        private ChatService CreateService()
        {
            return new ChatService(
                _openAIClientMock.Object,
                _cosmosClientMock.Object,
                _configurationMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task SendMessageAsync_WithEmptyMessage_ThrowsArgumentException()
        {
            // Arrange
            var service = CreateService();
            var request = new ChatRequest
            {
                Message = ""
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.SendMessageAsync(TestUserId, request));
        }

        [Fact]
        public async Task SendMessageAsync_WithNullUserId_ThrowsArgumentException()
        {
            // Arrange
            var service = CreateService();
            var request = new ChatRequest
            {
                Message = "Hello"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.SendMessageAsync(null!, request));
        }

        [Fact]
        public async Task SendMessageAsync_WithEmptyUserId_ThrowsArgumentException()
        {
            // Arrange
            var service = CreateService();
            var request = new ChatRequest
            {
                Message = "Hello"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.SendMessageAsync("", request));
        }

        [Fact]
        public async Task SendMessageAsync_WithInvalidSessionId_ThrowsInvalidOperationException()
        {
            // Arrange
            var service = CreateService();
            var request = new ChatRequest
            {
                Message = "Hello",
                SessionId = "non-existent-session"
            };

            _containerMock.Setup(c => c.ReadItemAsync<ChatSession>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SendMessageAsync(TestUserId, request));
        }

        [Fact]
        public void Constructor_WithMissingDeploymentName_ThrowsInvalidOperationException()
        {
            // Arrange
            _configurationMock.Setup(c => c["AzureOpenAI:DeploymentName"])
                .Returns((string?)null);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => CreateService());
        }

        [Fact]
        public void Constructor_WithMissingDatabaseName_ThrowsInvalidOperationException()
        {
            // Arrange
            _configurationMock.Setup(c => c["CosmosDb:DatabaseName"])
                .Returns((string?)null);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => CreateService());
        }

        [Fact]
        public async Task GetSessionAsync_WithValidSessionId_ReturnsSession()
        {
            // Arrange
            var service = CreateService();
            var sessionId = "session-123";
            var expectedSession = new ChatSession
            {
                Id = sessionId,
                UserId = TestUserId,
                Title = "Test Conversation",
                Messages = new List<Models.ChatMessage>
                {
                    new Models.ChatMessage { Role = "user", Content = "Hello", Timestamp = DateTime.UtcNow }
                }
            };

            var responseMock = new Mock<ItemResponse<ChatSession>>();
            responseMock.Setup(r => r.Resource).Returns(expectedSession);

            _containerMock.Setup(c => c.ReadItemAsync<ChatSession>(
                sessionId,
                It.Is<PartitionKey>(pk => pk.ToString().Contains(TestUserId)),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock.Object);

            // Act
            var result = await service.GetSessionAsync(TestUserId, sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sessionId, result.Id);
            Assert.Equal(TestUserId, result.UserId);
            Assert.Single(result.Messages);
        }

        [Fact]
        public async Task GetSessionAsync_WithNonExistentSession_ReturnsNull()
        {
            // Arrange
            var service = CreateService();
            var sessionId = "non-existent-session";

            _containerMock.Setup(c => c.ReadItemAsync<ChatSession>(
                sessionId,
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

            // Act
            var result = await service.GetSessionAsync(TestUserId, sessionId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserSessionsAsync_WithValidUserId_ReturnsSessions()
        {
            // Arrange
            var service = CreateService();
            var sessions = new List<ChatSession>
            {
                new ChatSession
                {
                    Id = "session-1",
                    UserId = TestUserId,
                    Title = "Conversation 1",
                    IsActive = true,
                    LastMessageAt = DateTime.UtcNow
                },
                new ChatSession
                {
                    Id = "session-2",
                    UserId = TestUserId,
                    Title = "Conversation 2",
                    IsActive = true,
                    LastMessageAt = DateTime.UtcNow.AddHours(-1)
                }
            };

            var feedIteratorMock = new Mock<FeedIterator<ChatSession>>();
            var feedResponseMock = new Mock<FeedResponse<ChatSession>>();

            feedResponseMock.Setup(r => r.GetEnumerator()).Returns(sessions.GetEnumerator());
            feedIteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            feedIteratorMock.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(feedResponseMock.Object);

            _containerMock.Setup(c => c.GetItemQueryIterator<ChatSession>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(feedIteratorMock.Object);

            // Act
            var result = await service.GetUserSessionsAsync(TestUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, s => Assert.Equal(TestUserId, s.UserId));
        }

        [Fact]
        public async Task GetUserSessionsAsync_WithNoSessions_ReturnsEmptyList()
        {
            // Arrange
            var service = CreateService();
            var sessions = new List<ChatSession>();

            var feedIteratorMock = new Mock<FeedIterator<ChatSession>>();
            var feedResponseMock = new Mock<FeedResponse<ChatSession>>();

            feedResponseMock.Setup(r => r.GetEnumerator()).Returns(sessions.GetEnumerator());
            feedIteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            feedIteratorMock.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(feedResponseMock.Object);

            _containerMock.Setup(c => c.GetItemQueryIterator<ChatSession>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(feedIteratorMock.Object);

            // Act
            var result = await service.GetUserSessionsAsync(TestUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task DeleteSessionAsync_WithValidSession_SetsIsActiveFalse()
        {
            // Arrange
            var service = CreateService();
            var sessionId = "session-123";
            var session = new ChatSession
            {
                Id = sessionId,
                UserId = TestUserId,
                IsActive = true
            };

            var readResponseMock = new Mock<ItemResponse<ChatSession>>();
            readResponseMock.Setup(r => r.Resource).Returns(session);

            _containerMock.Setup(c => c.ReadItemAsync<ChatSession>(
                sessionId,
                It.Is<PartitionKey>(pk => pk.ToString().Contains(TestUserId)),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(readResponseMock.Object);

            var upsertResponseMock = new Mock<ItemResponse<ChatSession>>();
            _containerMock.Setup(c => c.UpsertItemAsync(
                It.Is<ChatSession>(s => s.Id == sessionId && !s.IsActive),
                It.Is<PartitionKey>(pk => pk.ToString().Contains(TestUserId)),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(upsertResponseMock.Object);

            // Act
            await service.DeleteSessionAsync(TestUserId, sessionId);

            // Assert
            _containerMock.Verify(c => c.UpsertItemAsync(
                It.Is<ChatSession>(s => !s.IsActive),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteSessionAsync_WithNonExistentSession_DoesNotThrow()
        {
            // Arrange
            var service = CreateService();
            var sessionId = "non-existent-session";

            _containerMock.Setup(c => c.ReadItemAsync<ChatSession>(
                sessionId,
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

            // Act & Assert - should not throw
            await service.DeleteSessionAsync(TestUserId, sessionId);

            // Verify upsert was never called
            _containerMock.Verify(c => c.UpsertItemAsync(
                It.IsAny<ChatSession>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
