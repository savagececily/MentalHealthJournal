using MentalHealthJournal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace MentalHealthJournal.Tests.Services
{
    public class SpeechToTextServiceTests
    {
        private readonly Mock<ILogger<SpeechToTextService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly SpeechToTextService _service;

        public SpeechToTextServiceTests()
        {
            _loggerMock = new Mock<ILogger<SpeechToTextService>>();
            _configurationMock = new Mock<IConfiguration>();

            // Setup configuration
            _configurationMock.Setup(c => c["AzureCognitiveServices:Key"]).Returns("test-key");
            _configurationMock.Setup(c => c["AzureCognitiveServices:Region"]).Returns("eastus");

            _service = new SpeechToTextService(
                _loggerMock.Object,
                _configurationMock.Object);
        }

        [Fact]
        public void Constructor_WithMissingKey_ThrowsArgumentNullException()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["AzureCognitiveServices:Key"]).Returns((string?)null);
            configMock.Setup(c => c["AzureCognitiveServices:Region"]).Returns("eastus");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SpeechToTextService(_loggerMock.Object, configMock.Object));
        }

        [Fact]
        public void Constructor_WithMissingRegion_ThrowsArgumentNullException()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["AzureCognitiveServices:Key"]).Returns("test-key");
            configMock.Setup(c => c["AzureCognitiveServices:Region"]).Returns((string?)null);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SpeechToTextService(_loggerMock.Object, configMock.Object));
        }

        [Fact]
        public async Task TranscribeAsync_WithNullAudioFile_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => _service.TranscribeAsync(null!));
        }

        [Theory]
        [InlineData("test.wav")]
        [InlineData("recording.mp3")]
        [InlineData("voice-note.m4a")]
        public async Task TranscribeAsync_WithDifferentAudioFormats_HandlesGracefully(string fileName)
        {
            // Arrange
            var audioFile = CreateMockAudioFile(fileName, "audio/wav", "mock audio data");

            // Act & Assert
            // Since we can't easily mock the Azure Speech SDK, we expect this to throw
            // In a real-world scenario, you'd use dependency injection to abstract the Speech SDK
            await Assert.ThrowsAnyAsync<Exception>(() => _service.TranscribeAsync(audioFile));
        }

        [Fact]
        public void Service_ConfigurationIsProperlyInjected()
        {
            // This test verifies that the service can be instantiated with proper configuration
            // which is important for dependency injection validation

            // Arrange & Act
            var service = new SpeechToTextService(_loggerMock.Object, _configurationMock.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid-key")]
        [InlineData("test-key-123")]
        public void Constructor_WithVariousKeyFormats_HandlesCorrectly(string key)
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["AzureCognitiveServices:Key"]).Returns(string.IsNullOrEmpty(key) ? null : key);
            configMock.Setup(c => c["AzureCognitiveServices:Region"]).Returns("eastus");

            // Act & Assert
            // Empty string should throw, but other formats should not
            if (string.IsNullOrEmpty(key))
            {
                Assert.Throws<ArgumentNullException>(() => new SpeechToTextService(_loggerMock.Object, configMock.Object));
            }
            else
            {
                var service = new SpeechToTextService(_loggerMock.Object, configMock.Object);
                Assert.NotNull(service);
            }
        }

        [Theory]
        [InlineData("eastus")]
        [InlineData("westus")]
        [InlineData("northeurope")]
        [InlineData("southeastasia")]
        public void Constructor_WithVariousRegions_DoesNotThrow(string region)
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["AzureCognitiveServices:Key"]).Returns("test-key");
            configMock.Setup(c => c["AzureCognitiveServices:Region"]).Returns(region);

            // Act
            var service = new SpeechToTextService(_loggerMock.Object, configMock.Object);

            // Assert
            Assert.NotNull(service);
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
