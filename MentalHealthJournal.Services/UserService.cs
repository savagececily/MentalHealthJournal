using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using MentalHealthJournal.Models;
using User = MentalHealthJournal.Models.User;

namespace MentalHealthJournal.Services;

public class UserService : IUserService
{
    private readonly Container _usersContainer;
    private readonly ILogger<UserService> _logger;

    public UserService(CosmosClient cosmosClient, ILogger<UserService> logger)
    {
        var database = cosmosClient.GetDatabase("MentalHealthJournalDb");
        _usersContainer = database.GetContainer("Users");
        _logger = logger;
    }

    public async Task<User?> GetUserByProviderIdAsync(string providerId, string provider)
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProviderId = @providerId AND c.Provider = @provider")
                .WithParameter("@providerId", providerId)
                .WithParameter("@provider", provider);

            var iterator = _usersContainer.GetItemQueryIterator<User>(query);
            var results = await iterator.ReadNextAsync();
            
            return results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by provider ID");
            return null;
        }
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        try
        {
            var response = await _usersContainer.ReadItemAsync<User>(userId, new PartitionKey(userId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID");
            return null;
        }
    }

    public async Task<User> CreateOrUpdateUserAsync(User user)
    {
        try
        {
            user.LastLoginAt = DateTime.UtcNow;
            
            var response = await _usersContainer.UpsertItemAsync(user, new PartitionKey(user.userId));
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating user");
            throw;
        }
    }

    public async Task<bool> IsUsernameAvailableAsync(string username, string? currentUserId = null)
    {
        try
        {
            // NOTE: This performs a cross-partition query which can be expensive
            // For production, consider:
            // 1. Creating a secondary container with username as partition key
            // 2. Using Azure Cognitive Search for username lookups
            // 3. Caching username availability results
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE LOWER(c.Username) = LOWER(@username)")
                .WithParameter("@username", username);

            var iterator = _usersContainer.GetItemQueryIterator<User>(query);
            var results = await iterator.ReadNextAsync();
            
            var existingUser = results.FirstOrDefault();
            
            // Username is available if no one has it, or if the current user has it
            return existingUser == null || existingUser.id == currentUserId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking username availability");
            return false;
        }
    }
}
