using MentalHealthJournal.Services;
using MentalHealthJournal.Tests.Helpers;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MentalHealthJournal.Tests.Services
{
    public class BlobStorageServiceTests
    {
        private readonly Mock<ILogger<BlobStorageService>> _loggerMock;
        private readonly Mock<BlobServiceClient> _blobServiceClientMock;
        private readonly BlobStorageService _service;

        public BlobStorageServiceTests()
        {
            _loggerMock = new Mock<ILogger<BlobStorageService>>();
            _blobServiceClientMock = new Mock<BlobServiceClient>();

            var options = TestHelper.CreateTestOptions();
            _service = new BlobStorageService(_loggerMock.Object, options, _blobServiceClientMock.Object);
        }

        [Fact]
        public void Constructor_InitializesSuccessfully()
        {
            // Assert
            Assert.NotNull(_service);
        }

        [Fact]
        public async Task UploadAudioAsync_WithNullFile_ThrowsArgumentException()
        {
            // Arrange
            var userId = "test-user";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.UploadAudioAsync(null!, userId));
        }

        [Fact]
        public async Task UploadAudioAsync_WithEmptyUserId_ThrowsException()
        {
            // Arrange
            var audioFile = TestHelper.CreateMockAudioFile();

            // Act & Assert
            // Note: The actual exception type depends on implementation details
            await Assert.ThrowsAnyAsync<Exception>(() => 
                _service.UploadAudioAsync(audioFile, ""));
        }
    }
}
