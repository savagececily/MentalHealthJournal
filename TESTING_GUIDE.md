# Mental Health Journal - Testing AI Analysis & Trends

## ✅ What's Been Enhanced

Your Mental Health Journal now includes:

1. **AI-Powered Analysis** - Each journal entry is analyzed for:
   - 😊 Sentiment (Positive, Negative, Neutral, Mixed)
   - 🔑 Key Phrase Extraction
   - 📝 AI-Generated Summaries
   - 💫 Personalized Affirmations (via Azure OpenAI GPT-4)

2. **Trend Visualization** - Track your mental wellness over time:
   - Total entry count
   - Sentiment distribution chart
   - Recent trend indicator (improving 📈, declining 📉, or stable ➡️)
   - Visual bar charts showing sentiment breakdown

3. **Enhanced UI** - Better user experience:
   - Real-time analysis feedback
   - Sentiment score tooltips
   - Color-coded sentiment badges
   - Responsive design

## 🚀 How to Test

### Step 1: Start the Backend

```bash
cd /Users/cecilysavage/GitHub/MentalHealthJournal/MentalHealthJournal.Server
dotnet run
```

The backend will start on `https://localhost:7270` (or the port specified in launchSettings.json)

### Step 2: Start the Frontend

Open a new terminal:

```bash
cd /Users/cecilysavage/GitHub/MentalHealthJournal/mentalhealthjournal.client
npm run dev
```

The frontend will start on `http://localhost:54551/`

### Step 3: Test AI Analysis

1. **Create Your First Entry**
   - In the left sidebar, type a journal entry (e.g., "I had a wonderful day today! I feel grateful for my friends and family.")
   - Click "Save & Analyze Entry"
   - Watch the button change to "Analyzing with AI..." 
   - You'll see a message: "🤖 AI is analyzing your entry for sentiment, key phrases, and generating insights..."

2. **View AI Results**
   - The entry will appear in the main section with:
     - **Sentiment Badge** (color-coded: green for positive, red for negative, etc.)
     - **AI Summary** - A contextual summary of your entry
     - **Affirmation** - A personalized encouraging message from GPT-4
     - **Key Phrases** - Important topics extracted from your text

3. **Test Different Sentiments**
   Try creating entries with different emotional tones:
   
   - **Positive**: "Today was amazing! I accomplished so much and feel really proud of myself."
   - **Negative**: "I'm feeling really stressed and overwhelmed with everything going on."
   - **Neutral**: "Today was a regular day. I went to work, came home, and had dinner."
   - **Mixed**: "Work was frustrating, but I had a great evening with friends afterward."

### Step 4: View Trends

1. **After creating 3-5 entries**, you'll see the "📊 Your Trends" section appear in the sidebar
2. Click the "Show" button to expand the trends
3. You'll see:
   - **Total Entries** - How many journal entries you've created
   - **Recent Trend** - Analysis of your last 5 entries
   - **Sentiment Distribution** - Visual bar chart showing breakdown by sentiment type

## 🔍 What to Look For

### AI Analysis Verification

✅ **Sentiment Detection**
- Hover over the sentiment badge to see the confidence score
- Scores should range from 0.0 to 1.0
- Positive entries should show green badges, negative should show red

✅ **Key Phrases**
- Should extract meaningful topics from your entry
- Examples: "wonderful day", "friends and family", "feeling stressed"
- Displayed as rounded tags below the entry

✅ **AI Summaries**
- Should provide a brief, contextual summary
- Includes confidence percentage
- Tailored to the sentiment detected

✅ **GPT-4 Affirmations**
- Should be personalized and encouraging
- Written in second person ("You...")
- Specific to your entry's content and tone

### Trend Analysis Verification

✅ **Sentiment Distribution Chart**
- Bars should accurately represent the count of each sentiment type
- Bar width proportional to percentage
- Only shows sentiment types that have entries

✅ **Recent Trend**
- "Improving" 📈: Last 5 entries have more positive than negative
- "Declining" 📉: Last 5 entries have more negative than positive
- "Stable" ➡️: Balanced or not enough data

## 🧪 Test Scenarios

### Scenario 1: Track Mood Over Week
Create 7 entries simulating a week:
1. Monday (Neutral): "Back to work after the weekend."
2. Tuesday (Negative): "Feeling stressed about deadlines."
3. Wednesday (Mixed): "Tough morning but productive afternoon."
4. Thursday (Positive): "Things are looking up!"
5. Friday (Positive): "Accomplished so much this week!"
6. Saturday (Positive): "Relaxing weekend ahead."
7. Sunday (Positive): "Feeling grateful and recharged."

**Expected Result**: Trend should show "improving" with majority positive sentiment

### Scenario 2: Verify AI Accuracy
Create an entry with specific topics:
"I'm excited about my new yoga practice. I've been meditating daily and it's helping with my anxiety. My therapist recommended journaling too."

**Expected Result**:
- Sentiment: Positive
- Key Phrases: "yoga practice", "meditating daily", "anxiety", "therapist"
- Summary: Mentions positive mindset
- Affirmation: Should reference growth, self-care, or mental wellness

### Scenario 3: Empty State
1. Start fresh or delete all entries
2. Should see:
   - "No journal entries yet" message
   - Feature preview boxes (Sentiment Analysis, Key Phrases, etc.)
   - No trends section

## 🛠 Backend Configuration

Your app is using Azure App Configuration to load settings. The key services:

### Azure Cognitive Services (Text Analytics)
- **Endpoint**: `https://mentalhealthjournal-cogservices.cognitiveservices.azure.com/`
- **Region**: East US
- **Used For**: Sentiment analysis, key phrase extraction

### Azure OpenAI
- **Endpoint**: `https://mentalhealthjournal-openai.openai.azure.com/`
- **Deployment**: mentalhealthjournal-gpt-4
- **Used For**: Generating personalized affirmations

### Azure Cosmos DB
- Configuration loaded from Azure App Configuration
- **Container**: JournalEntryContainer
- **Partition Key**: /userId

### Azure Blob Storage
- **Container**: journalAudio
- **Used For**: Storing voice entry audio files (future feature)

## 🐛 Troubleshooting

### Problem: "Failed to save journal entry"
**Solution**: 
- Check that backend is running (`dotnet run`)
- Verify Azure services are accessible
- Check browser console for errors

### Problem: No AI summary or affirmation
**Solution**:
- Check Azure OpenAI configuration
- Verify deployment name matches in appsettings.json
- Check backend logs for errors

### Problem: Trends not showing
**Solution**:
- Create at least 1 entry first
- Click "Show" button to expand trends
- Refresh the page if needed

### Problem: Key phrases missing
**Solution**:
- Ensure entry has meaningful content (not just short phrases)
- Azure Cognitive Services extracts phrases based on significance
- Try longer, more descriptive entries

## 📊 Sample Test Data

Use these entries to quickly populate your journal:

```
1. "I'm feeling incredibly grateful today. My presentation went well and my team was so supportive!"

2. "Struggling a bit with motivation today. I need to be kinder to myself during tough times."

3. "Had a peaceful morning walk. The weather was perfect and I felt centered and calm."

4. "Feeling anxious about the upcoming project deadline. I'm trying to break it into smaller tasks."

5. "Celebrated a small win today! I finally finished that task I've been putting off. Feels great!"

6. "Today was okay. Nothing particularly good or bad happened. Just a regular Tuesday."

7. "I'm proud of myself for setting boundaries at work today. It wasn't easy but it was necessary."
```

## ✨ Next Steps

Once you've verified the AI analysis and trends work correctly, you could:

1. **Add More Visualizations**
   - Line chart showing sentiment over time
   - Word cloud from key phrases
   - Weekly/monthly summaries

2. **Export Features**
   - Download journal entries as PDF
   - Export trend data as CSV

3. **Advanced Analytics**
   - Identify mood triggers
   - Track correlation between activities and sentiment
   - Compare current vs. previous weeks

4. **Voice Entries**
   - Integrate SpeechToTextService
   - Record audio journal entries
   - Transcribe and analyze voice notes

5. **User Authentication**
   - Add login/signup
   - Multi-user support
   - Privacy and data security

---

**Your Mental Health Journal is now equipped with powerful AI analysis and trend tracking!** 🌱✨

Write a few entries and watch the AI provide insights into your emotional patterns.
