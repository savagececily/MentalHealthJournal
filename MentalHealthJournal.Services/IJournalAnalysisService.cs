using MentalHealthJournal.Models;

namespace MentalHealthJournal.Services
{
    public interface IJournalAnalysisService
    {
        //TODO: Define methods for journal analysis
        public Task<JournalAnalysisResult> AnalyzeAsync(string text, CancellationToken cancellationToken = default);

    }
}
