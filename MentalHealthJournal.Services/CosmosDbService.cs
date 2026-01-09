using MentalHealthJournal.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MentalHealthJournal.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly ILogger<CosmosDbService> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly AppSettings _appSettings;
        private readonly Container _container;

        public CosmosDbService(ILogger<CosmosDbService> logger, CosmosClient cosmosClient, IOptions<AppSettings> options)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _appSettings = options.Value;
            _container = _cosmosClient.GetContainer(_appSettings.CosmosDb.DatabaseName, _appSettings.CosmosDb.JournalEntryContainer);
        }

        public async Task SaveJournalEntryAsync(JournalEntry journalEntry, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Saving journal entry for user {UserId}", journalEntry.userId);
               await _container.CreateItemAsync(journalEntry, new PartitionKey(journalEntry.userId), cancellationToken: cancellationToken);
                _logger.LogInformation("Journal entry saved successfully for user {UserId}", journalEntry.userId);
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Cosmos DB error saving journal entry for user {UserId}. Status: {Status}", journalEntry.userId, ex.StatusCode);
                throw new InvalidOperationException($"Failed to save journal entry: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving journal entry for user {UserId}", journalEntry.userId);
                throw;
            }
        }

        public async Task<List<JournalEntry>> GetEntriesForUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Retrieving journal entries for user {UserId}", userId);
                QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId ORDER BY c.timestamp DESC")
                    .WithParameter("@userId", userId);

                var results = new List<JournalEntry>();

                var iterator = _container.GetItemQueryIterator<JournalEntry>(query, requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(userId)
                });

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync(cancellationToken);
                    results.AddRange(response);
                }

                _logger.LogInformation("Retrieved {Count} journal entries for user {UserId}", results.Count, userId);
                return results;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Cosmos DB error retrieving journal entries for user {UserId}. Status: {Status}", userId, ex.StatusCode);
                throw new InvalidOperationException($"Failed to retrieve journal entries: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving journal entries for user {UserId}", userId);
                throw;
            }
        }
    }
}
