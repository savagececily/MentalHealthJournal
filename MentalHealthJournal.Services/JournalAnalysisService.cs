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
            _openAIDeployment = configuration["AzureOpenAI:DeploymentName"];
        }

        public async Task<JournalAnalysisResult> AnalyzeAsync(string text, CancellationToken cancellationToken = default)
        {
            Response<DocumentSentiment> sentimentResult = await _textClient.AnalyzeSentimentAsync(text, cancellationToken: cancellationToken);
            Response<KeyPhraseCollection> keyPhrasesResult = await _textClient.ExtractKeyPhrasesAsync(text, cancellationToken: cancellationToken);

            string summary = $"This entry is mostly {sentimentResult.Value.Sentiment.ToString().ToLower()} with a score of {sentimentResult.Value.ConfidenceScores.Positive:N2} positivity.";
            string affirmation = await GenerateAffirmationAsync(text);

            return new JournalAnalysisResult
            {
                Sentiment = sentimentResult.Value.Sentiment.ToString(),
                SentimentScore = sentimentResult.Value.ConfidenceScores.Positive,
                KeyPhrases = keyPhrasesResult.Value.ToList(),
                Summary = summary,
                Affirmation = affirmation
            };
        }

        private async Task<string> GenerateAffirmationAsync(string journalText, CancellationToken cancellationToken = default)
        {
            string prompt = $"Read this journal entry and generate a kind, supportive affirmation for the user:\n\n\"{journalText}\"";

            List<ChatMessage> chatMessages = new List<ChatMessage>()
            {
                new SystemChatMessage( "You are a kind mental health assistant."),
                new UserChatMessage(prompt),
            };

            ChatCompletionOptions requestOptions = new ChatCompletionOptions()
            {
                MaxOutputTokenCount = 4096,
                Temperature = 1.0f,
                TopP = 1.0f,

            };

            ChatClient chatClient = _openAIClient.GetChatClient(_openAIDeployment);

            _logger.LogInformation("Generating affirmation for journal entry: {JournalText}", journalText);

            ClientResult<ChatCompletion> completions = await chatClient.CompleteChatAsync(chatMessages, requestOptions, cancellationToken:cancellationToken);

            return completions.Value.Content[0].Text;
        }
    }
}

