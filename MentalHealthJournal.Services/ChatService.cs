using Azure.AI.OpenAI;
using MentalHealthJournal.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace MentalHealthJournal.Services
{
    public class ChatService : IChatService
    {
        private readonly AzureOpenAIClient _openAIClient;
        private readonly Container _chatContainer;
        private readonly ILogger<ChatService> _logger;
        private readonly string _deploymentName;

        private const string SystemPrompt = @"You are a compassionate and empathetic virtual mental health support companion. Your role is to:

1. Listen actively and validate the user's feelings without judgment
2. Provide emotional support and encouragement
3. Help users reflect on their thoughts and emotions
4. Suggest healthy coping strategies and self-care practices
5. Recognize signs of crisis and provide appropriate resources

Important guidelines:
- You are NOT a replacement for professional therapy or medical advice
- Always encourage users to seek professional help for serious concerns
- If a user expresses thoughts of self-harm or suicide, provide crisis resources immediately
- Maintain a warm, supportive, and non-judgmental tone
- Ask open-ended questions to help users explore their feelings
- Validate emotions while gently challenging negative thought patterns
- Respect boundaries and user autonomy

Remember: Your goal is to provide support and encouragement, not to diagnose or treat mental health conditions.";

        public ChatService(
            AzureOpenAIClient openAIClient,
            CosmosClient cosmosClient,
            IConfiguration configuration,
            ILogger<ChatService> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
            _deploymentName = configuration["AzureOpenAI:DeploymentName"] 
                ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is not configured");

            var databaseName = configuration["CosmosDb:DatabaseName"] 
                ?? throw new InvalidOperationException("CosmosDb:DatabaseName is not configured");
            var containerName = configuration["CosmosDb:ChatSessionContainer"] ?? "ChatSessions";
            
            _chatContainer = cosmosClient.GetContainer(databaseName, containerName);
        }

        public async Task<ChatResponse> SendMessageAsync(string userId, ChatRequest request)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                throw new ArgumentException("Message cannot be null or empty", nameof(request));
            }

            try
            {
                // Get or create session
                ChatSession session;
                if (!string.IsNullOrEmpty(request.SessionId))
                {
                    session = await GetSessionAsync(userId, request.SessionId) 
                        ?? throw new InvalidOperationException("Session not found");
                }
                else
                {
                    session = new ChatSession
                    {
                        UserId = userId,
                        Title = GenerateSessionTitle(request.Message)
                    };
                }

                // Add user message
                var userMessage = new Models.ChatMessage
                {
                    Role = "user",
                    Content = request.Message,
                    Timestamp = DateTime.UtcNow
                };
                session.Messages.Add(userMessage);

                // Prepare messages for OpenAI
                var chatMessagesForContext = new List<Models.ChatMessage>
                {
                    new Models.ChatMessage { Role = "system", Content = SystemPrompt }
                };
                chatMessagesForContext.AddRange(session.Messages.TakeLast(10)); // Keep last 10 messages for context

                // Get response from Azure OpenAI
                var chatClient = _openAIClient.GetChatClient(_deploymentName);
                var openAIMessages = new List<OpenAI.Chat.ChatMessage>();
                foreach (var m in chatMessagesForContext)
                {
                    if (m.Role == "user")
                    {
                        openAIMessages.Add(OpenAI.Chat.ChatMessage.CreateUserMessage(m.Content));
                    }
                    else if (m.Role == "assistant")
                    {
                        openAIMessages.Add(OpenAI.Chat.ChatMessage.CreateAssistantMessage(m.Content));
                    }
                    else
                    {
                        openAIMessages.Add(OpenAI.Chat.ChatMessage.CreateSystemMessage(m.Content));
                    }
                }

                var chatCompletion = await chatClient.CompleteChatAsync(openAIMessages);
                var assistantResponse = chatCompletion.Value.Content[0].Text;

                // Add assistant message
                var assistantMessage = new Models.ChatMessage
                {
                    Role = "assistant",
                    Content = assistantResponse,
                    Timestamp = DateTime.UtcNow
                };
                session.Messages.Add(assistantMessage);
                session.LastMessageAt = DateTime.UtcNow;

                // Save session to Cosmos DB
                await _chatContainer.UpsertItemAsync(
                    session,
                    new PartitionKey(userId)
                );

                _logger.LogInformation("Chat message processed for user {UserId} in session {SessionId}", 
                    userId, session.Id);

                return new ChatResponse
                {
                    SessionId = session.Id,
                    Message = assistantResponse,
                    Timestamp = assistantMessage.Timestamp
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message for user {UserId}", userId);
                throw;
            }
        }

        public async Task<ChatSession?> GetSessionAsync(string userId, string sessionId)
        {
            try
            {
                var response = await _chatContainer.ReadItemAsync<ChatSession>(
                    sessionId,
                    new PartitionKey(userId)
                );
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<List<ChatSession>> GetUserSessionsAsync(string userId)
        {
            try
            {
                var query = new QueryDefinition(
                    "SELECT * FROM c WHERE c.userId = @userId AND c.isActive = true ORDER BY c.lastMessageAt DESC"
                ).WithParameter("@userId", userId);

                var sessions = new List<ChatSession>();
                using var feedIterator = _chatContainer.GetItemQueryIterator<ChatSession>(
                    query,
                    requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId) }
                );

                while (feedIterator.HasMoreResults)
                {
                    var response = await feedIterator.ReadNextAsync();
                    sessions.AddRange(response);
                }

                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chat sessions for user {UserId}", userId);
                throw;
            }
        }

        public async Task DeleteSessionAsync(string userId, string sessionId)
        {
            try
            {
                var session = await GetSessionAsync(userId, sessionId);
                if (session != null)
                {
                    session.IsActive = false;
                    await _chatContainer.UpsertItemAsync(
                        session,
                        new PartitionKey(userId)
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting chat session {SessionId} for user {UserId}", 
                    sessionId, userId);
                throw;
            }
        }

        private static string GenerateSessionTitle(string firstMessage)
        {
            // Generate a title from the first message (max 50 chars)
            var title = firstMessage.Length > 50 
                ? firstMessage[..47] + "..." 
                : firstMessage;
            return title;
        }
    }
}
