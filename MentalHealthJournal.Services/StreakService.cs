using MentalHealthJournal.Models;
using Microsoft.Extensions.Logging;

namespace MentalHealthJournal.Services
{
    public class StreakService : IStreakService
    {
        private readonly ICosmosDbService _cosmosService;
        private readonly IUserService _userService;
        private readonly ILogger<StreakService> _logger;

        public StreakService(
            ICosmosDbService cosmosService,
            IUserService userService,
            ILogger<StreakService> logger)
        {
            _cosmosService = cosmosService;
            _userService = userService;
            _logger = logger;
        }

        public async Task<(int currentStreak, int longestStreak)> CalculateStreaksAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var entries = await _cosmosService.GetEntriesForUserAsync(userId, cancellationToken);
                
                if (entries.Count == 0)
                {
                    return (0, 0);
                }

                // Get unique dates (in UTC, date part only)
                var entryDates = entries
                    .Select(e => e.Timestamp.Date)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToList();

                if (entryDates.Count == 0)
                {
                    return (0, 0);
                }

                // Calculate current streak
                int currentStreak = 0;
                DateTime today = DateTime.UtcNow.Date;
                DateTime checkDate = today;

                foreach (var date in entryDates)
                {
                    // Check if this date matches the expected date
                    if (date == checkDate || (currentStreak == 0 && date == today.AddDays(-1)))
                    {
                        currentStreak++;
                        checkDate = date.AddDays(-1);
                    }
                    else
                    {
                        break;
                    }
                }

                // Calculate longest streak
                int longestStreak = 0;
                int tempStreak = 1;

                for (int i = 0; i < entryDates.Count - 1; i++)
                {
                    var daysDiff = (entryDates[i] - entryDates[i + 1]).Days;
                    
                    if (daysDiff == 1)
                    {
                        tempStreak++;
                    }
                    else
                    {
                        longestStreak = Math.Max(longestStreak, tempStreak);
                        tempStreak = 1;
                    }
                }
                
                longestStreak = Math.Max(longestStreak, tempStreak);
                longestStreak = Math.Max(longestStreak, currentStreak);

                _logger.LogInformation("Calculated streaks for user {UserId}: Current={Current}, Longest={Longest}", 
                    userId, currentStreak, longestStreak);

                return (currentStreak, longestStreak);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating streaks for user {UserId}", userId);
                throw;
            }
        }

        public async Task UpdateUserStreakAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if we need to recalculate (avoid unnecessary calculations)
                var user = await _userService.GetUserByIdAsync(userId);
                if (user != null && user.LastStreakUpdateDate.HasValue)
                {
                    var daysSinceUpdate = (DateTime.UtcNow.Date - user.LastStreakUpdateDate.Value).Days;
                    
                    // Only recalculate if it's been at least a day since last update
                    if (daysSinceUpdate == 0)
                    {
                        _logger.LogInformation("Streak already calculated today for user {UserId}, skipping recalculation", userId);
                        return;
                    }
                }
                
                var (currentStreak, longestStreak) = await CalculateStreaksAsync(userId, cancellationToken);
                
                if (user != null)
                {
                    user.CurrentStreak = currentStreak;
                    user.LongestStreak = longestStreak;
                    user.LastStreakUpdateDate = DateTime.UtcNow.Date;
                    
                    await _userService.CreateOrUpdateUserAsync(user);
                    
                    _logger.LogInformation("Updated streak for user {UserId}: Current={Current}, Longest={Longest}", 
                        userId, currentStreak, longestStreak);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating streak for user {UserId}", userId);
                throw;
            }
        }
    }
}
