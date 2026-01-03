# Voice Recording Feature - Implementation Summary

## Overview
Voice recording functionality has been added to the Inside Journal application, allowing users to create journal entries by recording their voice instead of typing text.

## Features Implemented

### Frontend Components

1. **AudioRecordingService** (`src/services/audioRecordingService.ts`)
   - Handles browser media recording using Web Audio API
   - Captures audio from user's microphone
   - Manages recording state (start/stop)
   - Returns audio as Blob for upload
   - Automatically cleans up media streams

2. **VoiceRecorder Component** (`src/components/VoiceRecorder.tsx`)
   - User-friendly recording interface
   - Visual recording indicator with animated pulse dot
   - Real-time recording timer (MM:SS format)
   - Start/Stop controls
   - Microphone permission handling
   - Can be disabled when text entry is active

3. **Updated App.tsx**
   - Integrated VoiceRecorder in New Entry tab
   - Added "OR" divider between text and voice input
   - Audio preview with playback controls
   - Clear recording button
   - Transcription status indicator
   - Disabled submit button logic for voice processing
   - Mutually exclusive text/voice entry (can use one or the other)

4. **Enhanced Styling** (`src/components/VoiceRecorder.css` & `src/Tabs.css`)
   - Gradient button styling for record/stop
   - Animated pulse dot during recording
   - Audio preview card with dashed border
   - Clear visual feedback for all states
   - Responsive design

### Backend Updates

1. **New Voice Endpoint** (`POST /api/journal/voice`)
   - Accepts multipart form data with audio file and userId
   - Uploads audio to Azure Blob Storage
   - Transcribes audio using Azure Speech-to-Text
   - Returns transcription and blob URL

2. **Updated Analyze Endpoint** (`POST /api/journal/analyze`)
   - Now accepts `isVoiceEntry` and `audioBlobUrl` fields
   - Works with both text and voice entries
   - Saves voice entry metadata to Cosmos DB

3. **Enhanced JournalEntryRequest Model**
   - Added `IsVoiceEntry` boolean flag
   - Added `AudioBlobUrl` property for blob reference

## User Flow

1. **Recording**
   - User clicks "🎤 Record Voice Entry" button
   - Browser requests microphone permission (first time)
   - Recording starts with visual pulse indicator and timer
   - User clicks "⏹️ Stop" when finished

2. **Preview**
   - Audio preview appears with playback controls
   - User can listen to recording before submitting
   - "Clear Recording" button allows re-recording

3. **Submission**
   - User clicks "✨ Save & Analyze Entry"
   - Status shows "🎤 Transcribing audio..."
   - Audio uploaded to Azure Blob Storage
   - Speech-to-Text converts audio to text
   - Status changes to "🤖 Analyzing with AI..."
   - AI analyzes transcribed text for sentiment and insights
   - Entry saved with voice metadata

4. **Display**
   - Voice entries show in Past Entries tab
   - Audio player available for playback
   - Transcribed text displayed
   - Full AI analysis (sentiment, summary, affirmation)

## Technical Details

### Audio Format
- **Recording Format**: WebM (browser default)
- **Codec**: Opus (most browsers)
- **Sample Rate**: Browser default (typically 48kHz)

### Azure Services Used
- **Azure Blob Storage**: Stores audio recordings
- **Azure Speech-to-Text**: Transcribes voice to text
- **Azure OpenAI**: Analyzes transcribed text
- **Azure Text Analytics**: Sentiment analysis
- **Azure Cosmos DB**: Stores entry metadata

### API Endpoints

**POST /api/journal/voice**
```
Request: multipart/form-data
- userId: string
- audioFile: File (WebM audio)

Response: 200 OK
{
  "transcription": "text from audio",
  "audioBlobUrl": "https://..."
}
```

**POST /api/journal/analyze**
```
Request: application/json
{
  "userId": "string",
  "text": "transcribed or typed text",
  "isVoiceEntry": true/false,
  "audioBlobUrl": "string (optional)"
}

Response: 200 OK
{
  "id": "guid",
  "userId": "string",
  "text": "entry text",
  "isVoiceEntry": true,
  "audioBlobUrl": "https://...",
  "sentiment": "positive/negative/neutral/mixed",
  "sentimentScore": 0.85,
  "keyPhrases": ["phrase1", "phrase2"],
  "summary": "AI-generated summary",
  "affirmation": "Personalized affirmation"
}
```

## Testing Checklist

- [ ] Microphone permission prompt appears
- [ ] Recording timer increments correctly
- [ ] Stop button ends recording
- [ ] Audio preview plays recorded audio
- [ ] Clear Recording button removes audio
- [ ] Submit button disabled during transcription
- [ ] Transcription status message appears
- [ ] Voice entry saved to Cosmos DB
- [ ] Audio accessible from blob storage
- [ ] AI analysis works on transcribed text
- [ ] Voice entries display in Past Entries tab
- [ ] Audio playback works in entry details

## Next Steps

1. **Deploy Updated Code**
   ```bash
   # Build frontend
   cd mentalhealthjournal.client
   npm run build
   
   # Deploy to Azure Web App
   cd ..
   dotnet publish MentalHealthJournal.Server -c Release -o ./publish
   ```

2. **Test in Production**
   - Verify microphone permissions in browser
   - Test voice recording end-to-end
   - Verify blob storage upload
   - Confirm transcription accuracy
   - Check AI analysis on voice entries

3. **Future Enhancements**
   - Add audio waveform visualization during recording
   - Support multiple audio formats (MP3, WAV)
   - Add recording duration limit (e.g., 5 minutes)
   - Implement audio compression before upload
   - Add edit transcription feature before saving
   - Support multiple languages in speech recognition

## Files Changed

### New Files
- `mentalhealthjournal.client/src/services/audioRecordingService.ts`
- `mentalhealthjournal.client/src/components/VoiceRecorder.tsx`
- `mentalhealthjournal.client/src/components/VoiceRecorder.css`

### Modified Files
- `mentalhealthjournal.client/src/App.tsx`
- `mentalhealthjournal.client/src/Tabs.css`
- `MentalHealthJournal.Server/Controllers/JournalController.cs`
- `MentalHealthJournal.Models/JournalEntryRequest.cs`

## Deployment Command

```bash
# From repository root
az webapp deploy \
  --resource-group MentalHealthJournal \
  --name MentalHealthJournal-WebApp \
  --src-path ./publish \
  --type zip
```
