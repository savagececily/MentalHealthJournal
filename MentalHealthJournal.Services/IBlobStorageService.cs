using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentalHealthJournal.Services
{
    public interface IBlobStorageService
    {
        public Task<string> UploadAudioAsync(IFormFile audioFile, string userId, CancellationToken cancellationToken = default);
    }
}
