using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace MentalHealthJournal.Services
{
    /// <summary>
    /// Provides resilience policies for handling transient failures in Azure services
    /// </summary>
    public static class ResiliencePolicies
    {
        /// <summary>
        /// Creates a retry pipeline for Cosmos DB operations
        /// Handles transient failures like rate limiting (429), network issues, etc.
        /// </summary>
        public static ResiliencePipeline CreateCosmosDbRetryPipeline(ILogger logger)
        {
            return new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "Cosmos DB operation failed. Attempt {Attempt} of {MaxAttempts}. Delaying {Delay}ms. Exception: {Exception}",
                            args.AttemptNumber,
                            3,
                            args.RetryDelay.TotalMilliseconds,
                            args.Outcome.Exception?.Message ?? "Unknown error"
                        );
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        /// <summary>
        /// Creates a retry pipeline for Azure OpenAI operations
        /// Handles rate limiting and transient failures
        /// </summary>
        public static ResiliencePipeline CreateOpenAIRetryPipeline(ILogger logger)
        {
            return new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "Azure OpenAI operation failed. Attempt {Attempt} of {MaxAttempts}. Delaying {Delay}ms. Exception: {Exception}",
                            args.AttemptNumber,
                            3,
                            args.RetryDelay.TotalMilliseconds,
                            args.Outcome.Exception?.Message ?? "Unknown error"
                        );
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        /// <summary>
        /// Creates a retry pipeline for Azure Cognitive Services (Speech, Text Analytics)
        /// Handles transient failures and rate limiting
        /// </summary>
        public static ResiliencePipeline CreateCognitiveServicesRetryPipeline(ILogger logger)
        {
            return new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "Cognitive Services operation failed. Attempt {Attempt} of {MaxAttempts}. Delaying {Delay}ms. Exception: {Exception}",
                            args.AttemptNumber,
                            3,
                            args.RetryDelay.TotalMilliseconds,
                            args.Outcome.Exception?.Message ?? "Unknown error"
                        );
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        /// <summary>
        /// Creates a retry pipeline for Azure Blob Storage operations
        /// Handles transient network failures
        /// </summary>
        public static ResiliencePipeline CreateBlobStorageRetryPipeline(ILogger logger)
        {
            return new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(500),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "Blob Storage operation failed. Attempt {Attempt} of {MaxAttempts}. Delaying {Delay}ms. Exception: {Exception}",
                            args.AttemptNumber,
                            3,
                            args.RetryDelay.TotalMilliseconds,
                            args.Outcome.Exception?.Message ?? "Unknown error"
                        );
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }
    }
}
