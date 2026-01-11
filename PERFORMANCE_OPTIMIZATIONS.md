# Performance Optimizations

This document outlines the performance optimizations implemented in the Mental Health Journal application to improve responsiveness, reduce load times, and ensure resilient Azure service integration.

## Table of Contents
- [Streak Caching](#streak-caching)
- [Retry Policies with Polly](#retry-policies-with-polly)
- [Code Splitting and Lazy Loading](#code-splitting-and-lazy-loading)
- [Monitoring and Troubleshooting](#monitoring-and-troubleshooting)
- [Future Enhancements](#future-enhancements)

---

## Streak Caching

### Overview
Streak calculations are now cached to prevent unnecessary recomputation on every journal entry operation.

### Implementation
- **Location**: `MentalHealthJournal.Services/StreakService.cs`
- **Model Update**: Added `LastStreakUpdateDate` property to `User` model
- **Logic**: `UpdateUserStreakAsync()` checks if the streak was already calculated today before performing expensive date calculations and database queries

### How It Works
```csharp
var daysSinceUpdate = (DateTime.UtcNow.Date - user.LastStreakUpdateDate.Date).Days;
if (daysSinceUpdate == 0)
{
    _logger.LogInformation("Streak already calculated today for user {UserId}, skipping recalculation", userId);
    return;
}
```

### Benefits
- **Reduces Cosmos DB queries**: Skips `GetEntriesForUserAsync()` calls when not needed
- **Improves API response time**: Journal entry creation/deletion is faster
- **Lower RU consumption**: Fewer Cosmos DB operations = cost savings
- **Better user experience**: Faster streak badge updates in the UI

### Monitoring
- Check logs for "Streak already calculated today" messages
- Monitor Cosmos DB RU consumption before/after optimization

---

## Retry Policies with Polly

### Overview
Implements exponential backoff retry policies for all Azure service calls to handle transient failures gracefully.

### Implementation
- **Location**: `MentalHealthJournal.Services/ResiliencePolicies.cs`
- **Package**: Polly 8.5.0
- **Services Covered**: Cosmos DB, Azure OpenAI, Cognitive Services (Text Analytics, Speech), Blob Storage

### Retry Pipeline Configurations

#### Cosmos DB
```csharp
CreateCosmosDbRetryPipeline(ILogger logger)
- 3 retry attempts
- Base delay: 1 second
- Exponential backoff with jitter
- Handles: RequestFailedException, HttpRequestException, TimeoutException
```

#### Azure OpenAI
```csharp
CreateOpenAIRetryPipeline(ILogger logger)
- 3 retry attempts
- Base delay: 2 seconds (longer due to model inference time)
- Exponential backoff with jitter
- Handles: RequestFailedException, ClientResultException, HttpRequestException, TimeoutException
```

#### Cognitive Services (Text Analytics, Speech)
```csharp
CreateCognitiveServicesRetryPipeline(ILogger logger)
- 3 retry attempts
- Base delay: 1 second
- Exponential backoff with jitter
- Handles: RequestFailedException, HttpRequestException, TimeoutException
```

#### Blob Storage
```csharp
CreateBlobStorageRetryPipeline(ILogger logger)
- 3 retry attempts
- Base delay: 500 milliseconds (faster due to lightweight operations)
- Exponential backoff with jitter
- Handles: RequestFailedException, HttpRequestException, TimeoutException
```

### Usage Example
```csharp
// In JournalAnalysisService.cs
private readonly ResiliencePipeline _cognitiveServicesRetryPipeline;

public JournalAnalysisService(/* ... */)
{
    _cognitiveServicesRetryPipeline = ResiliencePolicies.CreateCognitiveServicesRetryPipeline(_logger);
}

public async Task<JournalAnalysisResult> AnalyzeAsync(string content)
{
    // Wrap Azure service call in retry pipeline
    var sentimentResult = await _cognitiveServicesRetryPipeline.ExecuteAsync(
        async token => await _textClient.AnalyzeSentimentAsync(content, cancellationToken: token)
    );
}
```

### Benefits
- **Transient failure handling**: Automatically retries failed requests due to network issues, rate limiting, or temporary service unavailability
- **Exponential backoff**: Prevents overwhelming the service with immediate retries
- **Jitter**: Distributes retry attempts to avoid thundering herd problem
- **Comprehensive logging**: Tracks retry attempts for monitoring and debugging

### Monitoring
- Check Application Insights for retry warning logs
- Look for patterns in retry behavior (e.g., specific times of day)
- Monitor overall error rates to ensure retries are effective
- Adjust retry counts and delays if needed based on production data

---

## Code Splitting and Lazy Loading

### Overview
React lazy loading reduces the initial JavaScript bundle size by splitting heavy components into separate chunks that load on-demand.

### Implementation
- **Location**: `mentalhealthjournal.client/src/App.tsx`
- **Components Lazy-Loaded**:
  - DataExport
  - CalendarView
  - StreakCounter
  - SentimentTimeline
  - KeyPhrasesCloud
  - TimePatterns

### How It Works
```typescript
// Convert from eager import
import { DataExport } from './components/DataExport';

// To lazy import
const DataExport = lazy(() => 
    import('./components/DataExport')
        .then(module => ({ default: module.DataExport }))
);

// Wrap in Suspense with loading placeholder
<Suspense fallback={<div className="loading-placeholder">Loading export...</div>}>
    <DataExport token={token} />
</Suspense>
```

### Bundle Analysis
**Before Optimization:**
- Single main bundle: ~420 kB (gzipped)

**After Optimization:**
- Main bundle: 394.65 kB (137.94 kB gzipped)
- KeyPhrasesCloud chunk: 2.35 kB (0.99 kB gzipped)
- DataExport chunk: 2.96 kB (1.27 kB gzipped)
- StreakCounter chunk: 2.04 kB (0.81 kB gzipped)
- CalendarView chunk: 3.70 kB (1.56 kB gzipped)
- TimePatterns chunk: 3.90 kB (1.43 kB gzipped)
- SentimentTimeline chunk: 4.37 kB (1.48 kB gzipped)

### Benefits
- **Faster initial page load**: Users download less JavaScript upfront
- **Better First Contentful Paint (FCP)**: Critical content renders faster
- **On-demand loading**: Heavy visualization components only load when users navigate to those tabs
- **Improved perceived performance**: Loading placeholders provide immediate feedback
- **Bandwidth savings**: Users who don't view certain tabs never download those chunks

### Loading States
Each lazy-loaded component has a custom loading placeholder with a pulsing animation:
```css
.loading-placeholder {
    background: white;
    border-radius: 12px;
    padding: 60px 20px;
    text-align: center;
    color: #757575;
    font-size: 16px;
    animation: pulse 1.5s ease-in-out infinite;
}
```

### Browser Caching
Vite automatically generates hashed filenames (e.g., `SentimentTimeline-Dig0KKiU.js`) that enable long-term browser caching. When components change, the hash changes, invalidating the cache.

---

## Monitoring and Troubleshooting

### Application Insights Setup
1. **Enable Application Insights** on your Azure App Service
2. **Configure the connection string** in appsettings.json
3. **Enable logging** for all services (already configured in `Program.cs`)

### Key Metrics to Monitor

#### Performance Metrics
- **Average response time** for `/api/journal` endpoints
- **P95/P99 latency** for journal entry creation
- **RU consumption** in Cosmos DB
- **Streak calculation frequency** (should be once per day per user)

#### Resilience Metrics
- **Retry attempt count** by service (Cosmos DB, OpenAI, Cognitive Services, Blob Storage)
- **Final failure rate** after retries exhausted
- **Retry success rate** (how many retries eventually succeed)
- **Error distribution** by exception type

#### Bundle Performance
- **First Contentful Paint (FCP)** - Should improve with code splitting
- **Largest Contentful Paint (LCP)** - Monitor for regressions
- **Time to Interactive (TTI)** - Should improve with smaller initial bundle
- **Chunk load times** for lazy-loaded components

### Troubleshooting Guide

#### Streak Not Updating
1. Check logs for "Streak already calculated today" message
2. Verify `LastStreakUpdateDate` is being updated correctly
3. Ensure timezone handling is consistent (using `DateTime.UtcNow`)

#### Retry Policy Issues
1. Look for retry warning logs in Application Insights
2. Check if retries are exhausted (3 attempts)
3. Verify Azure services are responding (check Azure Service Health)
4. Increase retry count or delays if transient failures are common

#### Lazy Loading Problems
1. Check browser console for chunk load errors
2. Verify Vite build output includes all expected chunks
3. Ensure Suspense fallbacks display properly
4. Check CDN/hosting configuration for proper MIME types

#### High RU Consumption
1. Verify streak caching is working (check logs)
2. Monitor queries per request in Application Insights
3. Consider adding indexes to Cosmos DB for common queries
4. Review partition key strategy for even distribution

---

## Future Enhancements

### Pagination for Journal Entries
**Status**: Planned  
**Description**: Add pagination to `/api/journal` endpoint to load entries in batches  
**Benefits**:
- Reduces memory usage for users with many entries
- Faster initial page load for "Past Entries" section
- Lower bandwidth consumption

**Implementation Considerations**:
- Add `skip` and `take` query parameters
- Implement "Load More" button or infinite scroll
- Consider cursor-based pagination for large datasets

### Virtual Scrolling for Entries List
**Status**: Planned  
**Description**: Use react-window or react-virtualized to render only visible entries  
**Benefits**:
- Dramatically improves performance for users with 100+ entries
- Reduces DOM size and memory usage
- Smoother scrolling experience

**Implementation Considerations**:
- Install react-window: `npm install react-window`
- Wrap past entries list in `<FixedSizeList>` component
- Calculate item heights dynamically or use fixed height

### Response Caching
**Status**: Under Consideration  
**Description**: Implement React Query or SWR for API response caching  
**Benefits**:
- Reduces redundant API calls
- Instant data display from cache
- Automatic background revalidation

**Implementation Considerations**:
- Install React Query: `npm install @tanstack/react-query`
- Configure stale time and cache time based on data freshness requirements
- Add query invalidation on mutations (create, update, delete)

### Image Optimization
**Status**: Under Consideration  
**Description**: Optimize profile pictures and any other images  
**Benefits**:
- Faster page loads
- Lower bandwidth consumption
- Better mobile performance

**Implementation Considerations**:
- Use Azure Blob Storage CDN for image delivery
- Implement responsive images with srcset
- Consider WebP format with fallback

### Debounced Search/Filtering
**Status**: Not Started  
**Description**: Add debouncing for future search/filter features  
**Benefits**:
- Reduces unnecessary API calls while typing
- Improves perceived performance

**Implementation Considerations**:
- Use lodash.debounce or custom hook
- 300ms delay is typical for search inputs

---

## Configuration

### Polly Retry Configuration
To adjust retry behavior, modify `ResiliencePolicies.cs`:

```csharp
// Increase retry attempts
DelayOptions = new() { MaxRetryAttempts = 5 }

// Adjust base delay
DelayOptions = new() { Delay = TimeSpan.FromSeconds(3) }

// Change backoff type
BackoffType = DelayBackoffType.Linear // or Constant
```

### Lazy Loading Configuration
To add more lazy-loaded components:

```typescript
// 1. Convert import
const MyComponent = lazy(() => 
    import('./components/MyComponent')
        .then(module => ({ default: module.MyComponent }))
);

// 2. Wrap in Suspense
<Suspense fallback={<div className="loading-placeholder">Loading...</div>}>
    <MyComponent />
</Suspense>
```

---

## Testing

### Performance Testing
1. Use Chrome DevTools Lighthouse to measure FCP, LCP, TTI
2. Test on slow 3G connection to verify lazy loading benefits
3. Monitor Network tab to see chunk loading behavior
4. Test with large number of journal entries (50+)

### Resilience Testing
1. Simulate network failures using Fiddler or browser DevTools
2. Test with Azure services temporarily unavailable
3. Verify retry logs appear in Application Insights
4. Confirm user experience remains smooth during transient failures

### Caching Testing
1. Create multiple entries in same day, verify streak only calculated once
2. Check Cosmos DB queries in Application Insights
3. Verify `LastStreakUpdateDate` updates correctly

---

## Related Documentation
- [AZURE_DEPLOYMENT.md](./AZURE_DEPLOYMENT.md) - Azure deployment guide
- [TESTING_GUIDE.md](./TESTING_GUIDE.md) - Testing procedures
- [CRISIS_SUPPORT_FEATURE.md](./CRISIS_SUPPORT_FEATURE.md) - Crisis detection feature
- [DATA_VISUALIZATION_FEATURE.md](./DATA_VISUALIZATION_FEATURE.md) - Visualization features

## Support
For questions or issues related to performance optimizations, please create an issue in the repository.
