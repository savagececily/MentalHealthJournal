using MentalHealthJournal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentalHealthJournal.Services
{
    public interface ICosmosDbService
    {
        public Task SaveJournalEntryAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default);
        public Task<List<JournalEntry>> GetEntriesForUserAsync(string userId, CancellationToken cancellationToken = default);
    }
}
