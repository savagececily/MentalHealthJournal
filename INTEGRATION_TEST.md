# Frontend-Backend Integration Verification

## ✅ Integration Configuration Complete

### Proxy Configuration
The Vite development server is now configured to proxy API requests:

**File**: `mentalhealthjournal.client/vite.config.ts`
```typescript
proxy: {
    '^/weatherforecast': {
        target,  // https://localhost:7102
        secure: false
    },
    '^/api': {
        target,  // https://localhost:7102
        secure: false
    }
}
```

### API Endpoints

#### Backend (JournalController)
- **POST** `/api/journal/analyze` - Analyze and save journal entry
- **GET** `/api/journal?userId={userId}` - Retrieve user's entries

#### Frontend (App.tsx)
- **POST** `/api/journal/analyze` - Called by `submitEntry()`
- **GET** `/api/journal?userId=demo-user` - Called by `loadEntries()`

### Request/Response Flow

```
User Types Entry → Click "Save & Analyze"
    ↓
Frontend: submitEntry()
    ↓
POST /api/journal/analyze
    {
        userId: "demo-user",
        text: "I'm feeling great today!"
    }
    ↓
Vite Proxy → https://localhost:7102/api/journal/analyze
    ↓
Backend: JournalController.AnalyzeEntry()
    ↓
JournalAnalysisService.AnalyzeAsync()
    ├── Azure Cognitive Services (Sentiment + Key Phrases)
    └── Azure OpenAI GPT-4 (Affirmation)
    ↓
CosmosDbService.SaveJournalEntryAsync()
    ↓
Response:
    {
        id: "...",
        userId: "demo-user",
        timestamp: "2025-12-09T...",
        text: "I'm feeling great today!",
        sentiment: "Positive",
        sentimentScore: 0.95,
        keyPhrases: ["feeling great"],
        summary: "This entry reflects a positive mindset...",
        affirmation: "Your positive energy is wonderful..."
    }
    ↓
Frontend: Entry added to state
    ↓
UI: Entry card appears with AI analysis
```

## 🧪 Integration Test Steps

### Test 1: Basic Connection
```bash
# Terminal 1 - Start Backend
cd MentalHealthJournal.Server
dotnet run

# Wait for: "Now listening on: https://localhost:7102"
```

```bash
# Terminal 2 - Start Frontend
cd mentalhealthjournal.client
npm run dev

# Wait for: "Local: http://localhost:54551/"
```

**Verify**: 
- ✅ Frontend loads at `http://localhost:54551/`
- ✅ No console errors
- ✅ UI displays correctly

### Test 2: GET Request (Load Entries)
1. Open browser DevTools (F12) → Network tab
2. Refresh the page at `http://localhost:54551/`
3. Look for request to `/api/journal?userId=demo-user`

**Expected**:
- ✅ Request: `GET http://localhost:54551/api/journal?userId=demo-user`
- ✅ Proxied to: `https://localhost:7102/api/journal?userId=demo-user`
- ✅ Status: 200 OK (or 404 if no entries yet)
- ✅ Response: Empty array `[]` or array of entries

### Test 3: POST Request (Create Entry)
1. Keep DevTools Network tab open
2. Type in the journal textarea: "I had an amazing day today!"
3. Click "Save & Analyze Entry"
4. Watch the Network tab

**Expected**:
- ✅ Request: `POST http://localhost:54551/api/journal/analyze`
- ✅ Request Body:
  ```json
  {
    "userId": "demo-user",
    "text": "I had an amazing day today!"
  }
  ```
- ✅ Status: 200 OK
- ✅ Response includes:
  - `sentiment`: "Positive"
  - `sentimentScore`: 0.XX
  - `keyPhrases`: ["amazing day", ...]
  - `summary`: "This entry reflects..."
  - `affirmation`: "You are..."
- ✅ Entry appears in UI immediately

### Test 4: End-to-End AI Analysis
Create this entry:
> "I'm feeling stressed about work deadlines, but I'm trying to stay positive and take breaks."

**Expected**:
- ✅ Sentiment: "Mixed" or "Neutral" (yellow/gray badge)
- ✅ Key Phrases: ["stressed", "work deadlines", "stay positive", "take breaks"]
- ✅ Summary: Mentions mixed emotions or balanced tone
- ✅ Affirmation: Supportive message about managing stress
- ✅ All fields populated in UI

### Test 5: Trends Calculation
1. Create 5 entries with different sentiments
2. Check that "📊 Your Trends" section appears in sidebar
3. Click "Show" to expand
4. Verify sentiment distribution chart displays

**Expected**:
- ✅ Total Entries shows correct count
- ✅ Recent Trend shows direction (improving/declining/stable)
- ✅ Chart bars reflect actual entry counts
- ✅ Bar colors match sentiment types

## 🔍 Troubleshooting

### Problem: "Failed to save journal entry"
**Possible Causes**:
1. Backend not running
2. Backend running on wrong port
3. Azure services not configured
4. CORS issues

**Solutions**:
```bash
# Check backend is running
curl https://localhost:7102/weatherforecast -k

# Should return weather data

# Check specific endpoint
curl -X GET "https://localhost:7102/api/journal?userId=demo-user" -k

# Should return [] or entries
```

### Problem: "Network Error" in console
**Check**:
1. Proxy configuration in `vite.config.ts`
2. Backend URL is `https://localhost:7102`
3. Both servers are running

**Fix**: Verify target in vite.config.ts matches launchSettings.json

### Problem: 500 Internal Server Error
**Possible Causes**:
1. Azure Cognitive Services credentials invalid
2. Azure OpenAI deployment not found
3. Cosmos DB connection issue

**Check Backend Logs**:
Look for error messages in the terminal running `dotnet run`

**Verify Azure Services**:
```bash
# Check appsettings.json or Azure App Configuration
# Ensure:
# - AzureCognitiveServices:Endpoint
# - AzureCognitiveServices:Key
# - AzureOpenAI:Endpoint
# - AzureOpenAI:Key
# - AzureOpenAI:DeploymentName
# - CosmosDb:Endpoint
# - CosmosDb:Key
```

### Problem: Empty affirmation or summary
**Causes**:
- Azure OpenAI deployment not accessible
- GPT-4 model not deployed

**Fallback**: Service should return default affirmation if AI fails

### Problem: No key phrases
**Causes**:
- Entry text too short
- Azure Cognitive Services not extracting phrases

**Solution**: Try longer, more descriptive entries

## 📊 Integration Points Checklist

### ✅ Frontend → Backend
- [x] Vite proxy configured for `/api` routes
- [x] Fetch calls use relative URLs (`/api/journal`)
- [x] Request bodies match backend DTOs
- [x] Response types match frontend interfaces

### ✅ Backend → Frontend
- [x] CORS allows frontend origin (implicit via proxy)
- [x] Static file serving enabled (`UseDefaultFiles`, `UseStaticFiles`)
- [x] Fallback route to `index.html` for SPA
- [x] API routes prefixed with `/api`

### ✅ Backend → Azure Services
- [x] Azure Cognitive Services configured
- [x] Azure OpenAI configured
- [x] Cosmos DB configured
- [x] Blob Storage configured
- [x] Azure App Configuration loaded

### ✅ Data Flow
- [x] JournalEntryRequest DTO accepted by backend
- [x] JournalEntry model returned to frontend
- [x] TypeScript interface matches C# model
- [x] All AI analysis fields transmitted

## 🎯 Success Criteria

Your integration is successful when:

1. ✅ Frontend loads without errors
2. ✅ Entries can be created and saved
3. ✅ AI analysis appears for each entry:
   - Sentiment badge
   - Key phrases
   - Summary
   - Affirmation
4. ✅ Entries persist (reload page, entries still there)
5. ✅ Trends dashboard shows correct data
6. ✅ No console errors in browser
7. ✅ No errors in backend logs

## 🚀 Production Deployment Notes

For production deployment, you'll need to:

1. **Build Frontend**
   ```bash
   cd mentalhealthjournal.client
   npm run build
   ```
   This creates `dist/` folder with optimized assets

2. **Copy to Backend**
   ```bash
   cp -r dist/* ../MentalHealthJournal.Server/wwwroot/
   ```

3. **Deploy Backend**
   - Publish .NET app to Azure App Service
   - Configure environment variables for Azure services
   - Ensure static files are served from `wwwroot`

4. **No Proxy Needed**
   - In production, frontend is served by backend
   - API calls are same-origin (no CORS/proxy needed)

---

**Your frontend and backend are now fully integrated!** 🎉

Test it by running both servers and creating journal entries with AI analysis.
