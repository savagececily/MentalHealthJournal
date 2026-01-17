# Virtual Therapist Chat Feature

## Overview

The Virtual Therapist is a compassionate AI-powered chat feature that provides emotional support and mental health guidance to users. It uses Azure OpenAI to deliver empathetic, supportive conversations while maintaining appropriate boundaries.

## Features

✅ **Real-time AI Chat** - Powered by Azure OpenAI with therapeutic system prompts
✅ **Conversation History** - All chat sessions stored in Cosmos DB with user isolation
✅ **Multi-Session Support** - Users can have multiple independent conversations
✅ **Context-Aware Responses** - Maintains conversation context (last 10 messages)
✅ **Crisis Detection Awareness** - Includes guidelines for crisis situations
✅ **Beautiful UI** - Modern, responsive chat interface with smooth animations

## Architecture

### Backend Components

1. **Models** (`MentalHealthJournal.Models/ChatMessage.cs`)
   - `ChatMessage` - Individual message with role, content, and timestamp
   - `ChatSession` - Complete conversation with metadata
   - `ChatRequest` - API request format
   - `ChatResponse` - API response format

2. **Service** (`MentalHealthJournal.Services/`)
   - `IChatService` - Service interface
   - `ChatService` - Implementation with Azure OpenAI integration
   - Therapeutic system prompt optimized for mental health support
   - Cosmos DB integration for persistent storage

3. **Controller** (`MentalHealthJournal.Server/Controllers/ChatController.cs`)
   - `POST /api/chat/message` - Send message and get AI response
   - `GET /api/chat/sessions` - Get user's chat sessions
   - `GET /api/chat/session/{id}` - Get specific session
   - `DELETE /api/chat/session/{id}` - Delete session

### Frontend Components

1. **React Component** (`VirtualTherapist.tsx`)
   - Chat interface with message history
   - Session management sidebar
   - Real-time message sending
   - Loading states and error handling
   - Typing indicators

2. **Service** (`chatService.ts`)
   - API client for chat endpoints
   - TypeScript interfaces
   - Authentication handling

3. **Styling** (`VirtualTherapist.css`)
   - Modern gradient design
   - Responsive layout
   - Smooth animations
   - Mobile-friendly

## Setup Instructions

### 1. Azure Cosmos DB Setup

Create a new container for chat sessions:

```bash
# Using Azure CLI
az cosmosdb sql container create \
  --account-name <your-cosmos-account> \
  --database-name MentalHealthJournalDb \
  --name ChatSessions \
  --partition-key-path /userId \
  --throughput 400
```

Or create manually in Azure Portal:
- Container name: `ChatSessions`
- Partition key: `/userId`
- Throughput: 400 RU/s (can scale up as needed)

### 2. Azure App Configuration

Add the new configuration value:

```bash
az appconfig kv set \
  --name <your-app-config-name> \
  --key CosmosDb:ChatSessionContainer \
  --value ChatSessions
```

Or add manually in Azure Portal:
- Key: `CosmosDb:ChatSessionContainer`
- Value: `ChatSessions`

### 3. Configuration Files

Update your `appsettings.json`:

```json
{
  "CosmosDb": {
    "Endpoint": "your-cosmos-db-endpoint",
    "Key": "your-cosmos-db-key",
    "DatabaseName": "MentalHealthJournalDb",
    "JournalEntryContainer": "JournalEntries",
    "UserContainer": "Users",
    "ChatSessionContainer": "ChatSessions"
  }
}
```

### 4. Azure OpenAI Deployment

Ensure you have an Azure OpenAI deployment (already configured in your app):
- Model: GPT-4 or GPT-4o recommended for therapeutic conversations
- Deployment name should match `AzureOpenAI:DeploymentName` config

### 5. Build and Deploy

```bash
# Backend
cd MentalHealthJournal.Server
dotnet build
dotnet run

# Frontend
cd mentalhealthjournal.client
npm install
npm run dev
```

## Usage

1. **Navigate to Chat** - Click the "💬 Virtual Support" tab
2. **Start Conversation** - Type a message or use a starter prompt
3. **Get Support** - Receive empathetic, supportive responses
4. **Manage Sessions** - View past conversations in the sidebar
5. **Delete Conversations** - Remove sessions you no longer need

## System Prompt

The AI uses a carefully crafted system prompt that:
- Provides compassionate, empathetic support
- Validates feelings without judgment
- Suggests healthy coping strategies
- Recognizes crisis situations
- Maintains appropriate professional boundaries
- Encourages professional help when needed

## Security & Privacy

- ✅ **User Isolation** - Each user's chats are partitioned by userId
- ✅ **Authentication Required** - JWT authentication for all endpoints
- ✅ **Encrypted Storage** - Data encrypted at rest in Cosmos DB
- ✅ **HTTPS Only** - All communication encrypted in transit
- ✅ **No Data Sharing** - Conversations are private to each user

## Cost Considerations

### Cosmos DB
- 400 RU/s baseline: ~$24/month
- Storage: ~$0.25/GB/month
- Scale up as needed

### Azure OpenAI
- GPT-4: ~$0.03-0.06 per 1K tokens
- Average conversation: 500-1000 tokens
- Estimate: ~$0.03-0.05 per conversation

### Optimization Tips
1. Limit conversation context to last 10 messages (already implemented)
2. Use GPT-3.5-turbo for lower costs (acceptable quality)
3. Implement rate limiting per user
4. Consider autoscaling Cosmos DB throughput

## API Examples

### Send Message

```bash
curl -X POST https://your-app.azurewebsites.net/api/chat/message \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "I'\''ve been feeling anxious lately",
    "sessionId": null
  }'
```

### Get Sessions

```bash
curl https://your-app.azurewebsites.net/api/chat/sessions \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Monitoring

Monitor usage in Azure Portal:
- **Cosmos DB Metrics** - Request units, latency, storage
- **OpenAI Metrics** - Token usage, request count, latency
- **Application Insights** - API performance, errors, user activity

## Future Enhancements

Potential features to add:
- 🔄 Export chat history
- 🎯 Mood tracking integration with journal entries
- 🔔 Proactive check-ins
- 🎨 Customizable AI personality
- 📊 Chat analytics and insights
- 🌐 Multi-language support
- 🎤 Voice chat integration
- 🤖 RAG with therapeutic resources

## Troubleshooting

### "Session not found" error
- Check that ChatSessions container exists in Cosmos DB
- Verify partition key is set to `/userId`

### AI responses are slow
- Check Azure OpenAI quotas and limits
- Consider using GPT-3.5-turbo instead of GPT-4
- Monitor Application Insights for latency

### Chat history not loading
- Verify JWT token is valid
- Check Cosmos DB connection string
- Review Application Insights logs

## Support

For issues or questions:
1. Check Application Insights logs
2. Review Cosmos DB metrics
3. Verify Azure OpenAI deployment status
4. Check authentication configuration

---

**Important:** This feature provides supportive conversation but is NOT a replacement for professional mental health care. Users should be encouraged to seek professional help for serious concerns.
