using MentalHealthJournal.Models;

namespace MentalHealthJournal.Services
{
    public interface IDataExportService
    {
        Task<string> ExportToJsonAsync(string userId, CancellationToken cancellationToken = default);
        Task<string> ExportToCsvAsync(string userId, CancellationToken cancellationToken = default);
    }
}
