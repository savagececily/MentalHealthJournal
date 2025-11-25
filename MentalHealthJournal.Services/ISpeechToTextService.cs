using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentalHealthJournal.Services
{
    public interface ISpeechToTextService
    {
        public Task<string> TranscribeAsync(IFormFile audioFile, CancellationToken cancellationToken = default);
    }
}
