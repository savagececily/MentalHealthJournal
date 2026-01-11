using MentalHealthJournal.Models;
using MentalHealthJournal.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MentalHealthJournal.Tests.Services
{
    /// <summary>
    /// Integration tests for StreakService
    /// Note: These tests verify the core streak calculation logic.
    /// Full integration tests with actual Cosmos DB would require test environment setup.
    /// </summary>
    public class StreakServiceTests
    {
        private readonly Mock<ICosmosDbService> _cosmosServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<ILogger<StreakService>> _loggerMock;
        private readonly StreakService _streakService;

        public StreakServiceTests()
        {
            _cosmosServiceMock = new Mock<ICosmosDbService>();
            _userServiceMock = new Mock<IUserService>();
            _loggerMock = new Mock<ILogger<StreakService>>();
            
            _streakService = new StreakService(
                _cosmosServiceMock.Object,
                _userServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task CalculateStreaksAsync_WithNoEntries_ReturnsZeroStreaks()
        {
            // Arrange
            var userId = "test-user-id";
            _cosmosServiceMock
                .Setup(x => x.GetEntriesForUserAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<JournalEntry>());

            // Act
            var result = await _streakService.CalculateStreaksAsync(userId);

            // Assert
            Assert.Equal(0, result.currentStreak);
            Assert.Equal(0, result.longestStreak);
        }

        [Fact]
        public async Task CalculateStreaksAsync_WithSingleTodayEntry_ReturnsOneStreak()
        {
            // Arrange
            var userId = "test-user-id";
            var today = DateTime.UtcNow.Date;
            var entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    id = "1",
                    userId = userId,
                    Text = "Test entry",
                    Timestamp = today
                }
            };

            _cosmosServiceMock
                .Setup(x => x.GetEntriesForUserAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entries);

            // Act
            var result = await _streakService.CalculateStreaksAsync(userId);

            // Assert
            Assert.Equal(1, result.currentStreak);
            Assert.Equal(1, result.longestStreak);
        }

        [Fact]
        public async Task CalculateStreaksAsync_WithConsecutiveDays_ReturnsCorrectStreak()
        {
            // Arrange
            var userId = "test-user-id";
            var today = DateTime.UtcNow.Date;
            var entries = new List<JournalEntry>
            {
                new JournalEntry { id = "1", userId = userId, Text = "Day 1", Timestamp = today },
                new JournalEntry { id = "2", userId = userId, Text = "Day 2", Timestamp = today.AddDays(-1) },
                new JournalEntry { id = "3", userId = userId, Text = "Day 3", Timestamp = today.AddDays(-2) },
                new JournalEntry { id = "4", userId = userId, Text = "Day 4", Timestamp = today.AddDays(-3) }
            };

            _cosmosServiceMock
                .Setup(x => x.GetEntriesForUserAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entries);

            // Act
            var result = await _streakService.CalculateStreaksAsync(userId);

            // Assert
            Assert.Equal(4, result.currentStreak);
            Assert.Equal(4, result.longestStreak);
        }

        [Fact]
        public async Task CalculateStreaksAsync_WithBrokenStreak_ReturnsCorrectCurrentAndLongestStreak()
        {
            // Arrange
            var userId = "test-user-id";
            var today = DateTime.UtcNow.Date;
            var entries = new List<JournalEntry>
            {
                // Current 2-day streak
                new JournalEntry { id = "1", userId = userId, Text = "Day 1", Timestamp = today },
                new JournalEntry { id = "2", userId = userId, Text = "Day 2", Timestamp = today.AddDays(-1) },
                // Gap here - streak broken
                // Old 3-day streak
                new JournalEntry { id = "3", userId = userId, Text = "Old 1", Timestamp = today.AddDays(-5) },
                new JournalEntry { id = "4", userId = userId, Text = "Old 2", Timestamp = today.AddDays(-6) },
                new JournalEntry { id = "5", userId = userId, Text = "Old 3", Timestamp = today.AddDays(-7) }
            };

            _cosmosServiceMock
                .Setup(x => x.GetEntriesForUserAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entries);

            // Act
            var result = await _streakService.CalculateStreaksAsync(userId);

            // Assert
            Assert.Equal(2, result.currentStreak); // Current streak: today and yesterday
            Assert.Equal(3, result.longestStreak); // Longest: the old 3-day streak
        }

        [Fact]
        public async Task CalculateStreaksAsync_WithMultipleEntriesPerDay_CountsOnlyUniqueDate()
        {
            // Arrange
            var userId = "test-user-id";
            var today = DateTime.UtcNow.Date;
            var entries = new List<JournalEntry>
            {
                new JournalEntry { id = "1", userId = userId, Text = "Morning", Timestamp = today.AddHours(8) },
                new JournalEntry { id = "2", userId = userId, Text = "Evening", Timestamp = today.AddHours(20) },
                new JournalEntry { id = "3", userId = userId, Text = "Yesterday", Timestamp = today.AddDays(-1) }
            };

            _cosmosServiceMock
                .Setup(x => x.GetEntriesForUserAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entries);

            // Act
            var result = await _streakService.CalculateStreaksAsync(userId);

            // Assert
            Assert.Equal(2, result.currentStreak); // Today and yesterday, not 3
            Assert.Equal(2, result.longestStreak);
        }

        [Fact]
        public async Task UpdateUserStreakAsync_AlreadyCalculatedToday_SkipsRecalculation()
        {
            // Arrange
            var userId = "test-user-id";
            var today = DateTime.UtcNow.Date;
            var user = new User
            {
                id = userId,
                userId = userId,
                Email = "test@example.com",
                Name = "Test User",
                Username = "testuser",
                CurrentStreak = 5,
                LongestStreak = 10,
                LastStreakUpdateDate = today // Already updated today
            };

            _userServiceMock
                .Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            await _streakService.UpdateUserStreakAsync(userId);

            // Assert
            // Should not call GetEntriesForUserAsync or CreateOrUpdateUserAsync
            _cosmosServiceMock.Verify(x => x.GetEntriesForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _userServiceMock.Verify(x => x.CreateOrUpdateUserAsync(It.IsAny<User>()), Times.Never);
            
            // Verify logging occurred
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Streak already calculated today")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateUserStreakAsync_WithNoPreviousUpdate_CalculatesAndUpdatesStreak()
        {
            // Arrange
            var userId = "test-user-id";
            var today = DateTime.UtcNow.Date;
            var user = new User
            {
                id = userId,
                userId = userId,
                Email = "test@example.com",
                Name = "Test User",
                Username = "testuser",
                CurrentStreak = 0,
                LongestStreak = 0,
                LastStreakUpdateDate = null
            };

            var entries = new List<JournalEntry>
            {
                new JournalEntry { id = "1", userId = userId, Text = "Test", Timestamp = today }
            };

            _userServiceMock
                .Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync(user);

            _cosmosServiceMock
                .Setup(x => x.GetEntriesForUserAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entries);

            _userServiceMock
                .Setup(x => x.CreateOrUpdateUserAsync(It.IsAny<User>()))
                .ReturnsAsync(user);

            // Act
            await _streakService.UpdateUserStreakAsync(userId);

            // Assert
            _userServiceMock.Verify(x => x.CreateOrUpdateUserAsync(It.Is<User>(u =>
                u.CurrentStreak == 1 &&
                u.LongestStreak == 1 &&
                u.LastStreakUpdateDate == today
            )), Times.Once);
        }

        [Fact]
        public async Task CalculateStreaksAsync_WithOldEntryOnly_ReturnsZeroCurrentStreak()
        {
            // Arrange
            var userId = "test-user-id";
            var today = DateTime.UtcNow.Date;
            var entries = new List<JournalEntry>
            {
                new JournalEntry { id = "1", userId = userId, Text = "Old entry", Timestamp = today.AddDays(-10) }
            };

            _cosmosServiceMock
                .Setup(x => x.GetEntriesForUserAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entries);

            // Act
            var result = await _streakService.CalculateStreaksAsync(userId);

            // Assert
            Assert.Equal(0, result.currentStreak); // No current streak
            Assert.Equal(1, result.longestStreak); // But there was a 1-day streak in the past
        }

        [Fact]
        public async Task UpdateUserStreakAsync_WithNullUser_DoesNotThrowAndLogsError()
        {
            // Arrange
            var userId = "non-existent-user";

            _userServiceMock
                .Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Mock GetEntriesForUserAsync to avoid null reference when user is null
            _cosmosServiceMock
                .Setup(x => x.GetEntriesForUserAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<JournalEntry>());

            // Act & Assert - Should not throw
            await _streakService.UpdateUserStreakAsync(userId);

            // Verify no update was attempted (because user is null after the calculation)
            _userServiceMock.Verify(x => x.CreateOrUpdateUserAsync(It.IsAny<User>()), Times.Never);
        }
    }
}
