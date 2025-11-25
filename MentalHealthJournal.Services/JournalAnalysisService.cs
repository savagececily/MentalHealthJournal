using Azure.AI.TextAnalytics;
using MentalHealthJournal.Models;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using Azure;
using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;

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
            IConfiguration configuration)
        {
            _logger = logger;
            _textClient = textClient;
            _openAIClient = openAIClient;
            _openAIDeployment = configuration["AzureOpenAI:DeploymentName"] ?? throw new ArgumentNullException("AzureOpenAI:DeploymentName");
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

                var result = new JournalAnalysisResult
                {
                    Sentiment = sentimentResult.Value.Sentiment.ToString(),
                    SentimentScore = sentimentResult.Value.ConfidenceScores.Positive,
                    KeyPhrases = keyPhrasesResult.Value.ToList(),
                    Summary = summary,
                    Affirmation = affirmation
                };

                _logger.LogInformation("Analysis completed successfully. Sentiment: {Sentiment}, KeyPhrases count: {KeyPhrasesCount}", 
                    result.Sentiment, result.KeyPhrases.Count);

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

