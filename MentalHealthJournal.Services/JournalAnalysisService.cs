using Azure.AI.TextAnalytics;
using MentalHealthJournal.Models;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using Azure;
using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace MentalHealthJournal.Services
{
    public class JournalAnalysisService : IJournalAnalysisService
    {
        private readonly ILogger<JournalAnalysisService> _logger;
        private readonly TextAnalyticsClient _textClient;
        private readonly AzureOpenAIClient _openAIClient;
        private readonly string _openAIDeployment;

        public JournalAnalysisService(ILogger<JournalAnalysisService> logger,
            TextAnalyticsClient textClient,
            AzureOpenAIClient openAIClient,
            IOptions<AppSettings> configuration)
        {
            _logger = logger;
            _textClient = textClient;
            _openAIClient = openAIClient;
            _openAIDeployment = configuration.Value.AzureOpenAI.DeploymentName ?? throw new ArgumentNullException("AzureOpenAI:DeploymentName");
        }

        public async Task<JournalAnalysisResult> AnalyzeAsync(string text, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new ArgumentException("Text cannot be null or empty", nameof(text));
                }

                _logger.LogInformation("Starting analysis for text with length: {TextLength}", text.Length);

                // Perform sentiment analysis
                Response<DocumentSentiment> sentimentResult = await _textClient.AnalyzeSentimentAsync(text, cancellationToken: cancellationToken);
                
                // Extract key phrases
                Response<KeyPhraseCollection> keyPhrasesResult = await _textClient.ExtractKeyPhrasesAsync(text, cancellationToken: cancellationToken);

                // Generate summary based on sentiment
                string summary = GenerateSummary(sentimentResult.Value);
                
                // Generate personalized affirmation using Azure OpenAI
                string affirmation = await GenerateAffirmationAsync(text, cancellationToken);

                // Check for crisis indicators
                var (isCrisis, crisisReason) = await DetectCrisisAsync(text, cancellationToken);

                var result = new JournalAnalysisResult
                {
                    Sentiment = sentimentResult.Value.Sentiment.ToString(),
                    SentimentScore = sentimentResult.Value.ConfidenceScores.Positive,
                    KeyPhrases = keyPhrasesResult.Value.ToList(),
                    Summary = summary,
                    Affirmation = affirmation,
                    IsCrisisDetected = isCrisis,
                    CrisisReason = crisisReason,
                    CrisisResources = isCrisis ? CrisisResources.GetDefaultResources() : new List<CrisisResource>()
                };

                _logger.LogInformation("Analysis completed successfully. Sentiment: {Sentiment}, KeyPhrases count: {KeyPhrasesCount}, Crisis detected: {IsCrisis}", 
                    result.Sentiment, result.KeyPhrases.Count, result.IsCrisisDetected);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing journal entry text");
                throw;
            }
        }

        private async Task<string> GenerateAffirmationAsync(string journalText, CancellationToken cancellationToken = default)
        {
            try
            {
                string prompt = $@"Read this journal entry and generate a kind, supportive, and personalized affirmation for the user. 
The affirmation should be encouraging, empathetic, and help them feel validated and supported.
Keep it concise (1-2 sentences) and speak directly to them using 'you'.

Journal entry: ""{journalText}""";

                List<ChatMessage> chatMessages = new List<ChatMessage>()
                {
                    new SystemChatMessage("You are a compassionate mental health assistant who provides supportive and encouraging affirmations. Your responses should be warm, validating, and help the user feel understood and supported."),
                    new UserChatMessage(prompt),
                };

                ChatCompletionOptions requestOptions = new ChatCompletionOptions()
                {
                    MaxOutputTokenCount = 200,
                    Temperature = 0.7f,
                    TopP = 1.0f,
                };

                ChatClient chatClient = _openAIClient.GetChatClient(_openAIDeployment);

                _logger.LogInformation("Generating affirmation for journal entry");

                ClientResult<ChatCompletion> completions = await chatClient.CompleteChatAsync(chatMessages, requestOptions, cancellationToken: cancellationToken);

                string affirmation = completions.Value.Content[0].Text.Trim();
                _logger.LogInformation("Generated affirmation successfully");
                
                return affirmation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating affirmation");
                // Return a fallback affirmation if AI generation fails
                return "You are valued, your feelings are valid, and you have the strength to navigate through this moment.";
            }
        }

        private async Task<(bool isCrisis, string? reason)> DetectCrisisAsync(string journalText, CancellationToken cancellationToken = default)
        {
            try
            {
                string prompt = $@"Analyze this journal entry for signs of immediate crisis or serious mental health concerns.
Specifically look for indicators of:
- Suicidal ideation or self-harm intentions
- Plans or methods to harm oneself or others
- Severe hopelessness or despair with no perceived way out
- Recent suicide attempts or severe self-harm
- Acute trauma or abuse

Do NOT flag general sadness, stress, anxiety, or normal difficult emotions.

Respond in JSON format:
{{
  ""isCrisis"": true or false,
  ""reason"": ""brief explanation if crisis detected, or null if not""
}}

Journal entry: ""{journalText}""";

                List<ChatMessage> chatMessages = new List<ChatMessage>()
                {
                    new SystemChatMessage("You are a mental health crisis detection system. Your role is to identify immediate safety concerns that require professional intervention. Be sensitive but accurate. Only flag genuine crises, not everyday struggles."),
                    new UserChatMessage(prompt),
                };

                ChatCompletionOptions requestOptions = new ChatCompletionOptions()
                {
                    MaxOutputTokenCount = 150,
                    Temperature = 0.3f, // Lower temperature for more consistent detection
                    TopP = 1.0f,
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                };

                ChatClient chatClient = _openAIClient.GetChatClient(_openAIDeployment);

                _logger.LogInformation("Performing crisis detection on journal entry");

                ClientResult<ChatCompletion> completions = await chatClient.CompleteChatAsync(chatMessages, requestOptions, cancellationToken: cancellationToken);

                string response = completions.Value.Content[0].Text.Trim();
                
                // Parse the JSON response
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(response);
                var root = jsonDoc.RootElement;
                
                bool isCrisis = root.GetProperty("isCrisis").GetBoolean();
                string? reason = root.TryGetProperty("reason", out var reasonElement) && reasonElement.ValueKind != System.Text.Json.JsonValueKind.Null
                    ? reasonElement.GetString()
                    : null;

                if (isCrisis)
                {
                    _logger.LogWarning("Crisis detected in journal entry. Reason: {Reason}", reason);
                }
                else
                {
                    _logger.LogInformation("No crisis indicators detected");
                }
                
                return (isCrisis, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing crisis detection");
                // In case of error, err on the side of caution but don't false alarm
                return (false, null);
            }
        }

        private string GenerateSummary(DocumentSentiment sentiment)
        {
            var dominantSentiment = sentiment.Sentiment.ToString().ToLower();
            var positiveScore = sentiment.ConfidenceScores.Positive;
            var negativeScore = sentiment.ConfidenceScores.Negative;
            var neutralScore = sentiment.ConfidenceScores.Neutral;

            string summary = dominantSentiment switch
            {
                "positive" => $"This entry reflects a positive mindset with {positiveScore:P0} confidence. You seem to be in good spirits.",
                "negative" => $"This entry shows some challenging emotions with {negativeScore:P0} confidence. Remember that difficult feelings are temporary.",
                "neutral" => $"This entry maintains a balanced tone with {neutralScore:P0} confidence. You appear to be processing your thoughts thoughtfully.",
                "mixed" => "This entry contains a mix of emotions, showing the complexity of your current experience.",
                _ => $"This entry is mostly {dominantSentiment} in tone."
            };

            return summary;
        }
    }
}

