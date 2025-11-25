using Microsoft.AspNetCore.Http;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MentalHealthJournal.Services
{
    public class SpeechToTextService: ISpeechToTextService
    {
        private readonly ILogger<SpeechToTextService> _logger;
        private readonly string _endpoint;
        private readonly string _speechKey;
        private readonly string _region;

        public SpeechToTextService(ILogger<SpeechToTextService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _endpoint = configuration["AzureCognitiveServices:Endpoint"];
            _speechKey = configuration["AzureCognitiveServices:SpeechKey"];
            _region = configuration["AzureCognitiveServices:Region"];
        }

        public async Task<string> TranscribeAsync(IFormFile audioFile, CancellationToken cancellationToken = default)
        {
            var config = SpeechConfig.FromEndpoint(new Uri(_endpoint), _speechKey);

            using var stream = audioFile.OpenReadStream();
            using var audioInput = AudioConfig.FromStreamInput(new BinaryAudioStreamReader(stream));
            using var recognizer = new SpeechRecognizer(config, audioInput);

            var result = await recognizer.RecognizeOnceAsync();
            return result.Reason == ResultReason.RecognizedSpeech ? result.Text : "";
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

