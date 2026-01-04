using MentalHealthJournal.Models;
using MentalHealthJournal.Services;
using MentalHealthJournal.Tests.Helpers;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MentalHealthJournal.Tests.Services
{
    public class CosmosDbServiceTests
    {
        private readonly Mock<ILogger<CosmosDbService>> _loggerMock;
        private readonly Mock<CosmosClient> _cosmosClientMock;
        private readonly CosmosDbService _service;

        public CosmosDbServiceTests()
        {
            _loggerMock = new Mock<ILogger<CosmosDbService>>();
            _cosmosClientMock = new Mock<CosmosClient>();

            var options = TestHelper.CreateTestOptions();
            _service = new CosmosDbService(_loggerMock.Object, _cosmosClientMock.Object, options);
        }

        [Fact]
        public void Constructor_InitializesSuccessfully()
        {
            // Assert
            Assert.NotNull(_service);
        }

        // Note: Full integration tests would require actual Cosmos DB connection
        // These tests are simplified to validate the service structure
    }
}
