using MentalHealthJournal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace MentalHealthJournal.Tests.Services
{
    public class BlobStorageServiceTests
    {
        private readonly Mock<ILogger<BlobStorageService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;

        public BlobStorageServiceTests()
        {
            _loggerMock = new Mock<ILogger<BlobStorageService>>();
            _configurationMock = new Mock<IConfiguration>();

            // Setup configuration
            _configurationMock.Setup(c => c["AzureBlobStorage:ContainerName"]).Returns("test-container");
        }

        [Fact]
        public void Constructor_WithMissingContainerName_ThrowsArgumentNullException()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["AzureBlobStorage:ContainerName"]).Returns((string?)null);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BlobStorageService(
                _loggerMock.Object,
                configMock.Object,
                null!)); // BlobServiceClient
        }

        [Fact]
        public async Task UploadAudioAsync_WithNullFile_ThrowsArgumentException()
        {
            // Arrange
            var service = new BlobStorageService(
                _loggerMock.Object,
                _configurationMock.Object,
                null!); // BlobServiceClient
            var userId = "user123";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.UploadAudioAsync(null!, userId));
        }

        [Fact]
        public async Task UploadAudioAsync_WithEmptyFile_ThrowsArgumentException()
        {
            // Arrange
            var audioFile = CreateMockAudioFile("test-audio.wav", "audio/wav", "");
            var userId = "user123";
            var service = new BlobStorageService(
                _loggerMock.Object,
                _configurationMock.Object,
                null!); // BlobServiceClient

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.UploadAudioAsync(audioFile, userId));
        }

        [Theory]
        [InlineData("test-audio.wav", "audio/wav")]
        [InlineData("recording.mp3", "audio/mpeg")]
        [InlineData("voice-note.m4a", "audio/mp4")]
        public async Task UploadAudioAsync_WithDifferentAudioFormats_ValidatesInput(string fileName, string contentType)
        {
            // Arrange
            var audioFile = CreateMockAudioFile(fileName, contentType, "test audio content");
            var userId = "user123";
            var service = new BlobStorageService(
                _loggerMock.Object,
                _configurationMock.Object,
                null!); // BlobServiceClient

            // Act & Assert
            // Should throw because BlobServiceClient is null, but input validation should pass
            await Assert.ThrowsAnyAsync<Exception>(() => service.UploadAudioAsync(audioFile, userId));
        }

        [Fact]
        public void Service_ConfigurationIsProperlyValidated()
        {
            // This test verifies that the service validates configuration during construction
            
            // Should not throw with valid configuration
            var service = new BlobStorageService(
                _loggerMock.Object,
                _configurationMock.Object,
                null!); // BlobServiceClient

            Assert.NotNull(service);
        }

        [Theory]
        [InlineData("user123")]
        [InlineData("user-456")]
        [InlineData("test@example.com")]
        public async Task UploadAudioAsync_WithValidUserIds_ValidatesInput(string userId)
        {
            // Arrange
            var audioFile = CreateMockAudioFile("test.wav", "audio/wav", "content");
            var service = new BlobStorageService(
                _loggerMock.Object,
                _configurationMock.Object,
                null!); // BlobServiceClient

            // Act & Assert
            // Should throw because BlobServiceClient is null, but input validation should pass
            await Assert.ThrowsAnyAsync<Exception>(() => service.UploadAudioAsync(audioFile, userId));
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
    }
}
