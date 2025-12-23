# 🔗 Frontend-Backend Integration Quick Reference

## ✅ Integration Status: COMPLETE

### Configuration Summary

| Component | Setting | Value |
|-----------|---------|-------|
| Backend URL | HTTPS | `https://localhost:7102` |
| Backend URL | HTTP | `http://localhost:5197` |
| Frontend URL | Dev Server | `http://localhost:54551` |
| Proxy | API Routes | `^/api` → `https://localhost:7102` |

### API Endpoints

#### Journal Management
```
POST /api/journal/analyze
GET  /api/journal?userId={userId}
```

### Request/Response Examples

#### Create Entry with AI Analysis
**Request:**
```http
POST /api/journal/analyze
Content-Type: application/json

{
  "userId": "demo-user",
  "text": "I'm feeling great today!"
}
```

**Response:**
```json
{
  "id": "abc123",
  "userId": "demo-user",
  "timestamp": "2025-12-09T10:30:00Z",
  "text": "I'm feeling great today!",
  "isVoiceEntry": false,
  "sentiment": "Positive",
  "sentimentScore": 0.95,
  "keyPhrases": ["feeling great"],
  "summary": "This entry reflects a positive mindset with 95% confidence. You seem to be in good spirits.",
  "affirmation": "Your positive energy is wonderful and uplifting!"
}
```

#### Retrieve Entries
**Request:**
```http
GET /api/journal?userId=demo-user
```

**Response:**
```json
[
  {
    "id": "abc123",
    "userId": "demo-user",
    "timestamp": "2025-12-09T10:30:00Z",
    "text": "I'm feeling great today!",
    "sentiment": "Positive",
    "sentimentScore": 0.95,
    "keyPhrases": ["feeling great"],
    "summary": "This entry reflects a positive mindset...",
    "affirmation": "Your positive energy is wonderful..."
  }
]
```

## 🚀 Start Commands

```bash
# Backend
cd MentalHealthJournal.Server
dotnet run

# Frontend (new terminal)
cd mentalhealthjournal.client
npm run dev
```

## 🧪 Quick Test

1. Open `http://localhost:54551/`
2. Type: "I had an amazing day!"
3. Click "Save & Analyze Entry"
4. Verify:
   - ✅ Green "Positive" badge appears
   - ✅ AI summary shows
   - ✅ Affirmation displays
   - ✅ Key phrases listed

## 🔧 Updated Files

- ✅ `vite.config.ts` - Added `/api` proxy route
- ✅ `App.tsx` - Uses `/api/journal` endpoints
- ✅ Build successful (no errors)

## 📡 Network Flow

```
Browser (localhost:54551)
    ↓ fetch('/api/journal/analyze')
Vite Dev Server
    ↓ Proxy to target
Backend (localhost:7102)
    ↓ JournalController
    ↓ JournalAnalysisService
    ↓ Azure Cognitive Services + OpenAI
    ↓ Cosmos DB
    ↓ Response
Browser
    ↓ Update UI
```

## ✨ Integration Complete!

The UI is now fully integrated with the backend API. All `/api` requests from the frontend are automatically proxied to the .NET backend running on port 7102.
