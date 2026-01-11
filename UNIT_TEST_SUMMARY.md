# Mental Health Journal - Unit Test Summary

**Last Updated**: January 11, 2026  
**Test Framework**: xUnit with Moq  
**Total Tests**: 29 tests passing ✅

## Test Execution Summary

```
Passed: 29 | Failed: 0 | Skipped: 0 | Duration: ~220ms
```

## Test Coverage by Service

### StreakServiceTests (10 tests) ✅
Tests for the streak calculation and caching optimization feature.

**Coverage:**
- ✅ `CalculateStreaksAsync_WithNoEntries_ReturnsZeroStreaks`
- ✅ `CalculateStreaksAsync_WithSingleTodayEntry_ReturnsOneStreak`
- ✅ `CalculateStreaksAsync_WithConsecutiveDays_ReturnsCorrectStreak`
- ✅ `CalculateStreaksAsync_WithBrokenStreak_ReturnsCorrectCurrentAndLongestStreak`
- ✅ `CalculateStreaksAsync_WithMultipleEntriesPerDay_CountsOnlyUniqueDate`
- ✅ `UpdateUserStreakAsync_AlreadyCalculatedToday_SkipsRecalculation` ⭐ *Tests caching optimization*
- ✅ `UpdateUserStreakAsync_WithNoPreviousUpdate_CalculatesAndUpdatesStreak`
- ✅ `CalculateStreaksAsync_WithOldEntryOnly_ReturnsZeroCurrentStreak`
- ✅ `UpdateUserStreakAsync_WithNullUser_DoesNotThrowAndLogsError`

**Key Features Tested:**
- Streak calculation algorithm correctness
- **NEW**: Caching optimization (skips recalculation if done today)
- Handling of consecutive vs. broken streaks
- Multiple entries per day (counts as single day)
- Edge cases: no entries, old entries only, null users

### JournalAnalysisServiceTests (2 tests) ✅
Tests for AI-powered journal analysis with Azure Cognitive Services and OpenAI.

**Coverage:**
- ✅ `AnalyzeAsync_WithEmptyText_ThrowsArgumentException`
- ✅ `AnalyzeAsync_WithNullText_ThrowsArgumentException`

**Key Features Tested:**
- Input validation for sentiment analysis
- Null/empty text handling

**Note**: Full integration tests with Azure AI services require test environment setup.

### Other Service Tests (17 tests) ✅
Existing tests for:
- **BlobStorageService**: File upload and audio storage operations
- **CosmosDbService**: Database connectivity and CRUD operations
- **SpeechToTextService**: Audio transcription and speech recognition

### Controller Tests ✅
- **JournalControllerTests**: API endpoint validation, authentication, CRUD operations

## Recent Test Additions

### Performance Optimizations (January 2026)

#### Streak Caching Tests
Added comprehensive tests for the new `LastStreakUpdateDate` caching feature:
- Validates that streak calculation is skipped when already done today
- Tests that `daysSinceUpdate == 0` logic works correctly
- Verifies logging behavior for cache hits

#### Test Implementation Details
```csharp
// Example: Testing the caching optimization
[Fact]
public async Task UpdateUserStreakAsync_AlreadyCalculatedToday_SkipsRecalculation()
{
    var today = DateTime.UtcNow.Date;
    var user = new User { LastStreakUpdateDate = today };
    
    await _streakService.UpdateUserStreakAsync(userId);
    
    // Verifies GetEntriesForUserAsync was NEVER called (cached)
    _cosmosServiceMock.Verify(x => x.GetEntriesForUserAsync(...), Times.Never);
}
```

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~StreakServiceTests"
dotnet test --filter "FullyQualifiedName~JournalAnalysisServiceTests"
```

### Run with Detailed Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run with Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Test Principles

### Arrange-Act-Assert (AAA) Pattern
All tests follow the AAA structure for clarity:

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange: Setup test data and mocks
    var testData = CreateTestData();
    _mockService.Setup(...).Returns(...);
    
    // Act: Execute the method under test
    var result = await _service.Method(testData);
    
    // Assert: Verify expected outcomes
    Assert.Equal(expectedValue, result);
    _mockService.Verify(..., Times.Once);
}
```

### Mocking Strategy
- **Moq Framework**: Used for mocking service dependencies
- **ILogger**: Always mocked to avoid console spam during tests
- **Azure SDK Clients**: Mocked to avoid external dependencies and network calls
- **Service Interfaces**: Mocked to isolate unit under test

### Test Naming Convention
`MethodName_Scenario_ExpectedOutcome`

Examples:
- `CalculateStreaksAsync_WithNoEntries_ReturnsZeroStreaks`
- `UpdateUserStreakAsync_AlreadyCalculatedToday_SkipsRecalculation`

## What's NOT Tested (Requires Integration Tests)

The following require actual Azure services and are excluded from unit tests:
- ❌ Actual Azure OpenAI API calls
- ❌ Actual Azure Cognitive Services sentiment analysis
- ❌ Actual Cosmos DB queries and operations
- ❌ Actual Blob Storage file uploads
- ❌ Actual Speech-to-Text transcription
- ❌ Polly retry policy behavior with real transient failures

**Integration Test Setup Needed:**
- Test Azure Cosmos DB instance (or use Emulator)
- Test Azure OpenAI deployment
- Test Cognitive Services instance
- Test Blob Storage account
- Test Speech Services instance

## Code Coverage Goals

**Current Status**: ~60% estimated coverage of critical business logic

**Coverage by Layer:**
- Services (Business Logic): ~70%
- Controllers (API): ~50%
- Models: 100% (no logic to test)

**Target**: 80% coverage for services with business logic

## Future Test Enhancements

### High Priority
- [ ] Add integration tests with Azure Cosmos DB Emulator
- [ ] Test crisis detection feature (DetectCrisisAsync method)
- [ ] Test data visualization calculation logic (timeline, word cloud, patterns)
- [ ] Test ResiliencePolicies retry behavior with simulated failures

### Medium Priority
- [ ] Add performance benchmarks for streak calculations
- [ ] Test UserService methods (GetUserById, CreateOrUpdate, UpdateUsername)
- [ ] Test DataExportService JSON/CSV export functionality
- [ ] Add load tests for API endpoints

### Low Priority
- [ ] Add mutation testing to verify test quality
- [ ] Add snapshot testing for complex responses
- [ ] Add property-based testing for streak algorithms

## Continuous Integration

Tests run automatically on:
- Every commit (via GitHub Actions - if configured)
- Pull request creation
- Pre-deployment validation

**Test Command in CI:**
```bash
dotnet test --configuration Release --no-build --verbosity normal
```

## Troubleshooting Tests

### Common Issues

**Issue**: Tests fail with NullReferenceException
- **Cause**: Missing mock setup for service dependency
- **Fix**: Add `.Setup()` for all called methods on mocked services

**Issue**: Tests fail with "Object reference not set"
- **Cause**: Service expects non-null user but test provides null
- **Fix**: Mock both user retrieval AND entry retrieval for null scenarios

**Issue**: Logger verification fails
- **Cause**: Logger.Log method has complex signature
- **Fix**: Use `It.IsAnyType` matcher for log verification

## Related Documentation
- [TESTING_GUIDE.md](../TESTING_GUIDE.md) - Comprehensive testing procedures
- [PERFORMANCE_OPTIMIZATIONS.md](../PERFORMANCE_OPTIMIZATIONS.md) - Performance features tested here
- [CRISIS_SUPPORT_FEATURE.md](../CRISIS_SUPPORT_FEATURE.md) - Crisis detection (needs tests)
- [DATA_VISUALIZATION_FEATURE.md](../DATA_VISUALIZATION_FEATURE.md) - Visualization logic (needs tests)

## Contact
For questions about tests, see the main [README.md](../README.md) or create an issue.
