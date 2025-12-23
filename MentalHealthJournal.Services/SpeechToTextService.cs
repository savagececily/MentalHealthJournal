using Microsoft.AspNetCore.Http;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MentalHealthJournal.Models;

namespace MentalHealthJournal.Services
{
    public class SpeechToTextService: ISpeechToTextService
    {
        private readonly ILogger<SpeechToTextService> _logger;
        private readonly string _speechKey;
        private readonly string _region;

        public SpeechToTextService(ILogger<SpeechToTextService> logger, IOptions<AppSettings> configuration)
        {
            _logger = logger;
            _speechKey = configuration.Value.AzureCognitiveServices.Key ?? throw new ArgumentNullException("AzureCognitiveServices:Key");
            _region = configuration.Value.AzureCognitiveServices.Region ?? throw new ArgumentNullException("AzureCognitiveServices:Region");
        }

        public async Task<string> TranscribeAsync(IFormFile audioFile, CancellationToken cancellationToken = default)
        {
            try
            {
                var config = SpeechConfig.FromSubscription(_speechKey, _region);
                config.SpeechRecognitionLanguage = "en-US";

                using var stream = audioFile.OpenReadStream();
                using var audioInput = AudioConfig.FromStreamInput(new BinaryAudioStreamReader(stream));
                using var recognizer = new SpeechRecognizer(config, audioInput);

                _logger.LogInformation("Starting speech recognition for audio file: {FileName}", audioFile.FileName);

                var result = await recognizer.RecognizeOnceAsync();
                
                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    _logger.LogInformation("Speech recognition successful. Transcribed text length: {Length}", result.Text.Length);
                    return result.Text;
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    _logger.LogWarning("No speech could be recognized from audio file: {FileName}", audioFile.FileName);
                    return string.Empty;
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    _logger.LogError("Speech recognition was canceled. Reason: {Reason}, Details: {ErrorDetails}", 
                        cancellation.Reason, cancellation.ErrorDetails);
                    
                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        throw new InvalidOperationException($"Speech recognition failed: {cancellation.ErrorDetails}");
                    }
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during speech recognition for file: {FileName}", audioFile.FileName);
                throw;
            }
        }

        private class BinaryAudioStreamReader : PullAudioInputStreamCallback
        {
            private readonly Stream _stream;

            public BinaryAudioStreamReader(Stream stream)
            {
                _stream = stream;
            }

            public override int Read(byte[] dataBuffer, uint size)
            {
                return _stream.Read(dataBuffer, 0, (int)size);
            }

            public override void Close()
            {
                _stream.Close();
                base.Close();
            }
        }
    }
}

