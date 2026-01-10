using MentalHealthJournal.Models;

namespace MentalHealthJournal.Services
{
    public interface IStreakService
    {
        /// <summary>
        /// Calculate the current and longest streak for a user based on their journal entries
        /// </summary>
        Task<(int currentStreak, int longestStreak)> CalculateStreaksAsync(string userId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Update the user's streak information after a new entry is created or deleted
        /// </summary>
        Task UpdateUserStreakAsync(string userId, CancellationToken cancellationToken = default);
    }
}
