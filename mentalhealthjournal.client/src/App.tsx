import { useState, useEffect, useCallback, lazy, Suspense } from 'react';
import { useAppInsightsContext } from '@microsoft/applicationinsights-react-js';
import { useAuth } from './hooks/useAuth';
import Login from './components/Login';
import UsernameSetup from './components/UsernameSetup';
import About from './About';
import { VoiceRecorder } from './components/VoiceRecorder';
import { EditEntryModal } from './components/EditEntryModal';
import CrisisAlert from './components/CrisisAlert';
import { journalService } from './services/journalService';
import './App.css';
import './Tabs.css';

// Lazy load heavy components
const DataExport = lazy(() => import('./components/DataExport').then(module => ({ default: module.DataExport })));
const CalendarView = lazy(() => import('./components/CalendarView').then(module => ({ default: module.CalendarView })));
const StreakCounter = lazy(() => import('./components/StreakCounter').then(module => ({ default: module.StreakCounter })));
const SentimentTimeline = lazy(() => import('./components/SentimentTimeline').then(module => ({ default: module.SentimentTimeline })));
const KeyPhrasesCloud = lazy(() => import('./components/KeyPhrasesCloud').then(module => ({ default: module.KeyPhrasesCloud })));
const TimePatterns = lazy(() => import('./components/TimePatterns').then(module => ({ default: module.TimePatterns })));
const VirtualTherapist = lazy(() => import('./components/VirtualTherapist').then(module => ({ default: module.VirtualTherapist })));

interface CrisisResource {
    name: string;
    phoneNumber: string;
    textNumber: string;
    description: string;
    url: string;
    isAvailable24_7: boolean;
}

interface JournalEntry {
    id: string;
    userId: string;
    timestamp: string;
    text?: string;
    isVoiceEntry: boolean;
    audioBlobUrl?: string;
    sentiment: string;
    sentimentScore: number;
    keyPhrases: string[];
    summary: string;
    affirmation: string;
    isCrisisDetected?: boolean;
    crisisReason?: string;
    crisisResources?: CrisisResource[];
}

interface TrendData {
    totalEntries: number;
    sentimentDistribution: {
        positive: number;
        negative: number;
        neutral: number;
        mixed: number;
    };
    recentTrend: string;
    averageSentimentScore: number;
}

function App() {
    const appInsights = useAppInsightsContext();
    const { user, token, isAuthenticated, isLoading: authLoading, logout } = useAuth();
    const [entries, setEntries] = useState<JournalEntry[]>([]);
    const [journalText, setJournalText] = useState('');
    const [loading, setLoading] = useState(false);
    const [loadingError, setLoadingError] = useState<string | null>(null);
    const [analyzing, setAnalyzing] = useState(false);
    const [showAbout, setShowAbout] = useState(false);
    const [activeTab, setActiveTab] = useState<'new' | 'past' | 'insights' | 'calendar' | 'export' | 'chat'>('new');
    const [trends, setTrends] = useState<TrendData | null>(null);
    const [latestEntry, setLatestEntry] = useState<JournalEntry | null>(null);
    const [audioBlob, setAudioBlob] = useState<Blob | null>(null);
    const [isTranscribing, setIsTranscribing] = useState(false);
    const [showUsernameSetup, setShowUsernameSetup] = useState(false);
    const [audioBlobUrl, setAudioBlobUrl] = useState<string | null>(null);
    const [editingEntry, setEditingEntry] = useState<JournalEntry | null>(null);
    const [deletingEntryId, setDeletingEntryId] = useState<string | null>(null);
    const [showCrisisAlert, setShowCrisisAlert] = useState(false);
    const [crisisData, setCrisisData] = useState<{ reason?: string; resources: CrisisResource[] }>({ resources: [] });

    // Clean up blob URL when audioBlob changes or component unmounts
    useEffect(() => {
        if (audioBlob) {
            const url = URL.createObjectURL(audioBlob);
            setAudioBlobUrl(url);
            return () => {
                URL.revokeObjectURL(url);
                setAudioBlobUrl(null);
            };
        }
        return undefined;
    }, [audioBlob]);

    const handleUpdateEntry = async (entryId: string, newText: string) => {
        if (!token) return;

        try {
            const updatedEntry = await journalService.updateEntry(token, entryId, newText);
            
            // Update the entry in the local state
            setEntries(prevEntries => 
                prevEntries.map(entry => 
                    entry.id === entryId ? updatedEntry : entry
                )
            );
            
            // Update latest entry if it's the one being edited
            if (latestEntry?.id === entryId) {
                setLatestEntry(updatedEntry);
            }
            
            appInsights.trackEvent({ 
                name: 'JournalEntryUpdated',
                properties: { entryId, sentiment: updatedEntry.sentiment }
            });
        } catch (error) {
            console.error('Error updating entry:', error);
            appInsights.trackException({ exception: error as Error, properties: { action: 'updateEntry' } });
            throw error; // Re-throw to be handled by the modal
        }
    };

    const handleDeleteEntry = async (entryId: string) => {
        if (!token) return;
        
        const confirmDelete = window.confirm('Are you sure you want to delete this journal entry? This action cannot be undone.');
        if (!confirmDelete) return;

        setDeletingEntryId(entryId);
        
        try {
            await journalService.deleteEntry(token, entryId);
            
            // Remove the entry from local state
            setEntries(prevEntries => prevEntries.filter(entry => entry.id !== entryId));
            
            // Clear latest entry if it's the one being deleted
            if (latestEntry?.id === entryId) {
                setLatestEntry(null);
            }
            
            appInsights.trackEvent({ 
                name: 'JournalEntryDeleted',
                properties: { entryId }
            });
        } catch (error) {
            console.error('Error deleting entry:', error);
            alert('Failed to delete entry. Please try again.');
            appInsights.trackException({ exception: error as Error, properties: { action: 'deleteEntry' } });
        } finally {
            setDeletingEntryId(null);
        }
    };

    const loadEntries = useCallback(async () => {
        if (!token) return;
        
        let isCancelled = false;
        
        try {
            setLoading(true);
            setLoadingError(null);
            const response = await fetch('/api/journal', {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });
            
            if (isCancelled) return; // Don't update state if cancelled
            
            if (response.ok) {
                const data = await response.json();
                if (!isCancelled) {
                    setEntries(data);
                    appInsights.trackEvent({ name: 'EntriesLoaded', properties: { count: data.length } });
                }
            } else {
                const errorText = await response.text();
                console.error('Error loading entries:', errorText);
                if (!isCancelled) {
                    setLoadingError(`Failed to load entries: ${response.status} ${response.statusText}`);
                    appInsights.trackEvent({ name: 'EntriesLoadFailed', properties: { status: response.status, error: errorText } });
                }
            }
        } catch (error) {
            console.error('Error loading entries:', error);
            if (!isCancelled) {
                setLoadingError('Unable to connect to the server. Please check your connection.');
                appInsights.trackException({ exception: error as Error, properties: { action: 'loadEntries' } });
            }
        } finally {
            if (!isCancelled) {
                setLoading(false);
            }
        }
        
        return () => {
            isCancelled = true;
        };
    }, [token, appInsights]);

    const calculateTrends = useCallback(() => {
        const distribution = {
            positive: 0,
            negative: 0,
            neutral: 0,
            mixed: 0
        };

        let totalScore = 0;

        entries.forEach(entry => {
            const sentiment = entry.sentiment.toLowerCase();
            if (sentiment === 'positive') distribution.positive++;
            else if (sentiment === 'negative') distribution.negative++;
            else if (sentiment === 'neutral') distribution.neutral++;
            else if (sentiment === 'mixed') distribution.mixed++;
            
            totalScore += entry.sentimentScore;
        });

        const recentEntries = entries.slice(0, 5);
        const positiveRecent = recentEntries.filter(e => e.sentiment.toLowerCase() === 'positive').length;
        const negativeRecent = recentEntries.filter(e => e.sentiment.toLowerCase() === 'negative').length;
        
        let recentTrend = 'stable';
        if (positiveRecent > negativeRecent * 2) {
            recentTrend = 'improving';
        } else if (negativeRecent > positiveRecent * 2) {
            recentTrend = 'declining';
        }

        setTrends({
            totalEntries: entries.length,
            sentimentDistribution: distribution,
            recentTrend,
            averageSentimentScore: entries.length > 0 ? totalScore / entries.length : 0
        });
    }, [entries]);

    useEffect(() => {
        if (isAuthenticated && token) {
            loadEntries();
            // Check if user needs to set username
            if (!user?.username) {
                setShowUsernameSetup(true);
            }
        }
    }, [isAuthenticated, token, loadEntries, user?.username]);

    useEffect(() => {
        if (entries.length > 0) {
            calculateTrends();
        }
    }, [entries, calculateTrends]);

    async function submitEntry() {
        if (!journalText.trim() && !audioBlob) {
            alert('Please enter text or record a voice entry');
            return;
        }

        if (journalText.trim().length > 10000) {
            alert('Journal entry is too long. Please limit your entry to 10,000 characters.');
            return;
        }

        if (!token) {
            alert('Authentication error. Please log in again.');
            return;
        }

        const startTime = Date.now();
        try {
            setAnalyzing(true);

            // If there's an audio blob, we need to upload it first and get transcription
            let textToSubmit = journalText;
            let audioBlobUrl = '';
            let isVoiceEntry = false;

            if (audioBlob) {
                setIsTranscribing(true);
                const formData = new FormData();
                formData.append('audioFile', audioBlob, 'recording.webm');

                const uploadResponse = await fetch('/api/journal/voice', {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${token}`
                    },
                    body: formData,
                });

                if (uploadResponse.ok) {
                    const voiceResult = await uploadResponse.json();
                    textToSubmit = voiceResult.transcription;
                    audioBlobUrl = voiceResult.audioBlobUrl;
                    isVoiceEntry = true;
                } else {
                    const errorText = await uploadResponse.text();
                    throw new Error(`Failed to process voice recording: ${errorText || uploadResponse.statusText}`);
                }
                setIsTranscribing(false);
            }

            const response = await fetch('/api/journal/analyze', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    text: textToSubmit,
                    isVoiceEntry,
                    audioBlobUrl,
                }),
            });

            if (response.ok) {
                const newEntry = await response.json();
                setEntries([newEntry, ...entries]);
                setLatestEntry(newEntry);
                setJournalText('');
                setAudioBlob(null);
                // Check for crisis detection
                if (newEntry.isCrisisDetected && newEntry.crisisResources) {
                    setCrisisData({
                        reason: newEntry.crisisReason,
                        resources: newEntry.crisisResources
                    });
                    setShowCrisisAlert(true);
                    appInsights.trackEvent({ 
                        name: 'CrisisDetected',
                        properties: { 
                            entryId: newEntry.id,
                            reason: newEntry.crisisReason
                        }
                    });
                }
                
                const duration = Date.now() - startTime;
                appInsights.trackEvent({ 
                    name: 'JournalEntrySubmitted', 
                    properties: { 
                        sentiment: newEntry.sentiment,
                        sentimentScore: newEntry.sentimentScore,
                        textLength: textToSubmit.length,
                        duration,
                        isVoiceEntry,
                        isCrisisDetected: newEntry.isCrisisDetected || false
                    } 
                });
            } else {
                const errorText = await response.text();
                const errorMessage = errorText || `Server error: ${response.status} ${response.statusText}`;
                alert(`Failed to save journal entry: ${errorMessage}`);
                appInsights.trackEvent({ name: 'JournalSubmitFailed', properties: { error: errorMessage, status: response.status } });
            }
        } catch (error) {
            console.error('Error submitting entry:', error);
            const errorMessage = error instanceof Error ? error.message : 'An unexpected error occurred';
            alert(`Error submitting entry: ${errorMessage}. Please check your connection and try again.`);
            appInsights.trackException({ exception: error as Error, properties: { action: 'submitEntry' } });
        } finally {
            setAnalyzing(false);
            setIsTranscribing(false);
        }
    }

    function getSentimentEmoji(sentiment: string): string {
        switch (sentiment.toLowerCase()) {
            case 'positive':
                return '😊';
            case 'negative':
                return '😔';
            case 'neutral':
                return '😐';
            case 'mixed':
                return '🤔';
            default:
                return '📝';
        }
    }

    function getSentimentColor(sentiment: string): string {
        switch (sentiment.toLowerCase()) {
            case 'positive':
                return '#4ade80';
            case 'negative':
                return '#f87171';
            case 'neutral':
                return '#94a3b8';
            case 'mixed':
                return '#fbbf24';
            default:
                return '#6b7280';
        }
    }

    function getTrendEmoji(trend: string): string {
        switch (trend) {
            case 'improving':
                return '📈';
            case 'declining':
                return '📉';
            default:
                return '➡️';
        }
    }

    if (authLoading) {
        return (
            <div className="app-container">
                <div className="loading">Loading...</div>
            </div>
        );
    }

    if (!isAuthenticated) {
        return <Login />;
    }

    return (
        <div className="app-container">
            <header className="app-header">
                <div className="header-content">
                    <div>
                        <h1>🌱 Inside Journal</h1>
                        <p>Track your thoughts, understand your emotions</p>
                    </div>
                    <div className="header-actions">
                        <button 
                            className="crisis-help-button" 
                            onClick={async () => {
                                try {
                                    const response = await fetch('/api/crisis-resources');
                                    if (!response.ok) {
                                        throw new Error(`Failed to load crisis resources: ${response.status}`);
                                    }
                                    const resources = await response.json();
                                    setCrisisData({ resources });
                                } catch (error) {
                                    console.error('Error fetching crisis resources', error);
                                    // Fallback: preserve existing crisis data or default to an empty list
                                    setCrisisData(prev => prev ?? { resources: [] });
                                } finally {
                                    setShowCrisisAlert(true);
                                }
                            }}
                            aria-label="Access crisis support resources"
                        >
                            🆘 Need Help Now?
                        </button>
                        <div className="user-info">
                            {user?.profilePictureUrl && (
                                <img src={user.profilePictureUrl} alt={`${user.name}'s profile picture`} className="user-avatar" />
                            )}
                            <span className="user-name">{user?.username || user?.name}</span>
                        </div>
                        <button className="about-button" onClick={() => setShowAbout(true)} aria-label="Open about information">
                            About
                        </button>
                        <button className="logout-button" onClick={logout} aria-label="Log out of your account">
                            Logout
                        </button>
                    </div>
                </div>
            </header>

            <div className="tabs-container">
                <div className="tabs" role="tablist" aria-label="Journal navigation">
                    <button 
                        className={`tab ${activeTab === 'new' ? 'active' : ''}`}
                        onClick={() => setActiveTab('new')}
                        role="tab"
                        aria-selected={activeTab === 'new'}
                        aria-controls="new-entry-panel"
                    >
                        ✏️ New Entry
                    </button>
                    <button 
                        className={`tab ${activeTab === 'past' ? 'active' : ''}`}
                        onClick={() => setActiveTab('past')}
                        role="tab"
                        aria-selected={activeTab === 'past'}
                        aria-controls="past-entries-panel"
                    >
                        📚 Past Entries
                    </button>
                    <button 
                        className={`tab ${activeTab === 'insights' ? 'active' : ''}`}
                        onClick={() => setActiveTab('insights')}
                        role="tab"
                        aria-selected={activeTab === 'insights'}
                        aria-controls="insights-panel"
                    >
                        📊 Insights
                    </button>
                    <button 
                        className={`tab ${activeTab === 'calendar' ? 'active' : ''}`}
                        onClick={() => setActiveTab('calendar')}
                        role="tab"
                        aria-selected={activeTab === 'calendar'}
                        aria-controls="calendar-panel"
                    >
                        📅 Calendar
                    </button>
                    <button 
                        className={`tab ${activeTab === 'export' ? 'active' : ''}`}
                        onClick={() => setActiveTab('export')}
                        role="tab"
                        aria-selected={activeTab === 'export'}
                        aria-controls="export-panel"
                    >
                        📦 Export Data
                    </button>
                    <button 
                        className={`tab ${activeTab === 'chat' ? 'active' : ''}`}
                        onClick={() => setActiveTab('chat')}
                        role="tab"
                        aria-selected={activeTab === 'chat'}
                        aria-controls="chat-panel"
                    >
                        💬 Virtual Support
                    </button>
                </div>
            </div>

            <div className="tab-content">
                {activeTab === 'new' && (
                    <div className="new-entry-tab" role="tabpanel" id="new-entry-panel" aria-labelledby="new-entry-tab">
                        <div className="entry-form-section">
                            <h2>Write Your Thoughts</h2>
                            <div className="textarea-wrapper">
                                <textarea
                                    className="journal-input"
                                    placeholder="How are you feeling today? Write your thoughts here..."
                                    value={journalText}
                                    onChange={(e) => setJournalText(e.target.value)}
                                    rows={10}
                                    disabled={analyzing || isTranscribing}
                                    aria-label="Journal entry text"
                                    aria-describedby="journal-help-text"
                                    maxLength={10000}
                                />
                                {journalText.length > 0 && (
                                    <div className="character-count" aria-live="polite">
                                        {journalText.length} / 10,000 characters
                                    </div>
                                )}
                            </div>
                            <p id="journal-help-text" className="visually-hidden">Enter your thoughts and feelings for AI-powered sentiment analysis</p>
                            
                            <div className="input-divider">
                                <span>OR</span>
                            </div>

                            <VoiceRecorder 
                                onRecordingComplete={(blob) => {
                                    setAudioBlob(blob);
                                    setJournalText(''); // Clear text when voice is recorded
                                }}
                                disabled={analyzing || isTranscribing || journalText.trim().length > 0}
                            />

                            {audioBlob && audioBlobUrl && (
                                <div className="audio-preview" role="region" aria-live="polite" aria-label="Voice recording preview">
                                    <p>🎤 Voice recording ready</p>
                                    <audio controls src={audioBlobUrl} aria-label="Preview of recorded audio" />
                                    <button 
                                        className="clear-audio-button"
                                        onClick={() => setAudioBlob(null)}
                                        aria-label="Clear voice recording"
                                    >
                                        Clear Recording
                                    </button>
                                </div>
                            )}

                            <button 
                                className="submit-button" 
                                onClick={submitEntry}
                                disabled={analyzing || isTranscribing || (!journalText.trim() && !audioBlob)}
                                aria-label={isTranscribing ? 'Transcribing audio, please wait' : analyzing ? 'Analyzing entry with AI, please wait' : 'Save and analyze journal entry'}
                            >
                                {isTranscribing ? '🎤 Transcribing audio...' : 
                                 analyzing ? '🤖 Analyzing with AI...' : 
                                 '✨ Save & Analyze Entry'}
                            </button>
                            {(analyzing || isTranscribing) && (
                                <div className="analyzing-info" role="status" aria-live="polite">
                                    <p>
                                        {isTranscribing ? '🎤 Converting your voice to text...' : 
                                         '🤖 AI is analyzing your entry for sentiment, key phrases, and generating personalized insights...'}
                                    </p>
                                </div>
                            )}
                        </div>

                        {latestEntry && (
                            <div className="quick-insights-section">
                                <h2>Quick Insights</h2>
                                <div className="insight-card">
                                    <div className="insight-header">
                                        <div className="insight-date">
                                            {new Date(latestEntry.timestamp).toLocaleDateString('en-US', {
                                                weekday: 'long',
                                                year: 'numeric',
                                                month: 'long',
                                                day: 'numeric',
                                                hour: '2-digit',
                                                minute: '2-digit'
                                            })}
                                        </div>
                                        <div 
                                            className="sentiment-badge large"
                                            style={{ backgroundColor: getSentimentColor(latestEntry.sentiment) }}
                                        >
                                            {getSentimentEmoji(latestEntry.sentiment)} {latestEntry.sentiment}
                                        </div>
                                    </div>

                                    {latestEntry.summary && (
                                        <div className="insight-item">
                                            <div className="insight-label">📝 AI Summary</div>
                                            <div className="insight-content">{latestEntry.summary}</div>
                                        </div>
                                    )}

                                    {latestEntry.affirmation && (
                                        <div className="insight-item affirmation">
                                            <div className="insight-label">💫 Affirmation</div>
                                            <div className="insight-content">{latestEntry.affirmation}</div>
                                        </div>
                                    )}

                                    {latestEntry.keyPhrases && latestEntry.keyPhrases.length > 0 && (
                                        <div className="insight-item">
                                            <div className="insight-label">🔑 Key Themes</div>
                                            <div className="key-phrases-grid">
                                                {latestEntry.keyPhrases.map((phrase, index) => (
                                                    <span key={index} className="phrase-tag">
                                                        {phrase}
                                                    </span>
                                                ))}
                                            </div>
                                        </div>
                                    )}
                                </div>
                            </div>
                        )}
                    </div>
                )}

                {activeTab === 'past' && (
                    <div className="past-entries-tab">
                        <h2>Your Journal History</h2>
                        {loadingError && (
                            <div className="error-message">
                                <p>⚠️ {loadingError}</p>
                                <button onClick={() => loadEntries()} className="retry-button">
                                    🔄 Retry Loading Entries
                                </button>
                            </div>
                        )}
                        {!loadingError && (
                            <>
                                {loading ? (
                                    <p className="loading-text">Loading your entries...</p>
                                ) : entries.length === 0 ? (
                                    <div className="empty-state">
                                        <p>No journal entries yet.</p>
                                        <p>Start writing to track your mental wellness and see AI-powered insights!</p>
                                    </div>
                                ) : (
                                    <div className="entries-list">
                                        {entries.map((entry) => (
                                    <div key={entry.id} className="entry-card">
                                        <div className="entry-header">
                                            <div className="entry-date">
                                                {new Date(entry.timestamp).toLocaleDateString('en-US', {
                                                    weekday: 'short',
                                                    year: 'numeric',
                                                    month: 'short',
                                                    day: 'numeric',
                                                    hour: '2-digit',
                                                    minute: '2-digit'
                                                })}
                                            </div>
                                            <div 
                                                className="sentiment-badge"
                                                style={{ backgroundColor: getSentimentColor(entry.sentiment) }}
                                                title={`Sentiment Score: ${entry.sentimentScore.toFixed(2)}`}
                                            >
                                                {getSentimentEmoji(entry.sentiment)} {entry.sentiment}
                                            </div>
                                        </div>

                                        <div className="entry-text">
                                            {entry.text}
                                        </div>

                                        {entry.summary && (
                                            <div className="entry-summary">
                                                <strong>📝 AI Summary:</strong> {entry.summary}
                                            </div>
                                        )}

                                        {entry.affirmation && (
                                            <div className="entry-affirmation">
                                                <strong>💫 Affirmation:</strong> {entry.affirmation}
                                            </div>
                                        )}

                                        {entry.keyPhrases && entry.keyPhrases.length > 0 && (
                                            <div className="key-phrases">
                                                <div className="key-phrases-label">🔑 Key Phrases:</div>
                                                {entry.keyPhrases.map((phrase, index) => (
                                                    <span key={index} className="phrase-tag">
                                                        {phrase}
                                                    </span>
                                                ))}
                                            </div>
                                        )}

                                        <div className="entry-actions">
                                            <button 
                                                className="edit-entry-button"
                                                onClick={() => setEditingEntry(entry)}
                                                disabled={deletingEntryId === entry.id}
                                                aria-label="Edit journal entry"
                                            >
                                                ✏️ Edit
                                            </button>
                                            <button 
                                                className="delete-entry-button"
                                                onClick={() => handleDeleteEntry(entry.id)}
                                                disabled={deletingEntryId === entry.id}
                                                aria-label="Delete journal entry"
                                            >
                                                {deletingEntryId === entry.id ? '⏳ Deleting...' : '🗑️ Delete'}
                                            </button>
                                        </div>
                                    </div>
                                ))}
                            </div>
                                )}
                            </>
                        )}
                    </div>
                )}

                {activeTab === 'insights' && (
                    <div className="insights-tab">
                        <h2>Detailed Insights & Trends</h2>
                        {!trends || entries.length === 0 ? (
                            <div className="empty-state">
                                <p>No insights available yet.</p>
                                <p>Create more journal entries to see your emotional trends and patterns!</p>
                            </div>
                        ) : (
                            <div className="insights-grid">
                                {/* Sentiment Timeline */}
                                <div className="insight-full-width">
                                    <Suspense fallback={<div className="loading-placeholder">Loading timeline...</div>}>
                                        <SentimentTimeline entries={entries} />
                                    </Suspense>
                                </div>

                                {/* Key Phrases Cloud */}
                                <div className="insight-full-width">
                                    <Suspense fallback={<div className="loading-placeholder">Loading themes...</div>}>
                                        <KeyPhrasesCloud entries={entries} />
                                    </Suspense>
                                </div>

                                {/* Time Patterns */}
                                <div className="insight-full-width">
                                    <Suspense fallback={<div className="loading-placeholder">Loading patterns...</div>}>
                                        <TimePatterns entries={entries} />
                                    </Suspense>
                                </div>

                                {/* Existing Statistics */}
                                <div className="insight-card-large">
                                    <h3>📊 Overall Statistics</h3>
                                    <div className="stats-grid">
                                        <div className="stat-box">
                                            <div className="stat-value">{trends.totalEntries}</div>
                                            <div className="stat-label">Total Entries</div>
                                        </div>
                                        <div className="stat-box">
                                            <div className="stat-value">{trends.averageSentimentScore.toFixed(2)}</div>
                                            <div className="stat-label">Avg Sentiment Score</div>
                                        </div>
                                        <div className="stat-box">
                                            <div className="stat-value">{getTrendEmoji(trends.recentTrend)}</div>
                                            <div className="stat-label">{trends.recentTrend}</div>
                                        </div>
                                    </div>
                                </div>

                                <div className="insight-card-large">
                                    <h3>🎭 Sentiment Distribution</h3>
                                    <div className="sentiment-chart">
                                        <div className="chart-bars">
                                            {Object.entries(trends.sentimentDistribution).map(([sentiment, count]) => {
                                                const percentage = trends.totalEntries > 0 
                                                    ? (count / trends.totalEntries) * 100 
                                                    : 0;
                                                return count > 0 ? (
                                                    <div key={sentiment} className="chart-bar-container">
                                                        <div className="chart-label">
                                                            {getSentimentEmoji(sentiment)} {sentiment}
                                                        </div>
                                                        <div className="chart-bar-wrapper">
                                                            <div 
                                                                className="chart-bar"
                                                                style={{ 
                                                                    width: `${percentage}%`,
                                                                    backgroundColor: getSentimentColor(sentiment)
                                                                }}
                                                            />
                                                            <span className="chart-count">{count} ({percentage.toFixed(1)}%)</span>
                                                        </div>
                                                    </div>
                                                ) : null;
                                            })}
                                        </div>
                                    </div>
                                </div>

                                <div className="insight-card-large">
                                    <h3>💡 Insights Summary</h3>
                                    <div className="insights-summary">
                                        <p>You've journaled <strong>{trends.totalEntries}</strong> times!</p>
                                        <p>Your recent emotional trend is <strong>{trends.recentTrend}</strong> {getTrendEmoji(trends.recentTrend)}</p>
                                        <p>Keep tracking your thoughts to gain deeper insights into your mental wellness journey.</p>
                                    </div>
                                </div>
                            </div>
                        )}
                    </div>
                )}

                {activeTab === 'calendar' && token && (
                    <div className="calendar-tab" role="tabpanel" id="calendar-panel" aria-labelledby="calendar-tab">
                        <h2>📅 Journal Calendar & Streak</h2>
                        <Suspense fallback={<div className="loading-placeholder">Loading calendar...</div>}>
                            <div className="calendar-streak-container">
                                <StreakCounter token={token} />
                                <CalendarView token={token} />
                            </div>
                        </Suspense>
                    </div>
                )}

                {activeTab === 'export' && token && (
                    <div className="export-tab">
                        <Suspense fallback={<div className="loading-placeholder">Loading export...</div>}>
                            <DataExport token={token} />
                        </Suspense>
                    </div>
                )}

                {activeTab === 'chat' && token && (
                    <div className="chat-tab">
                        <Suspense fallback={<div className="loading-placeholder">Loading chat...</div>}>
                            <VirtualTherapist />
                        </Suspense>
                    </div>
                )}
            </div>

            {showAbout && <About onClose={() => setShowAbout(false)} />}
            {showUsernameSetup && <UsernameSetup onComplete={() => setShowUsernameSetup(false)} />}
            {editingEntry && (
                <EditEntryModal
                    entryId={editingEntry.id}
                    currentText={editingEntry.text || ''}
                    onSave={handleUpdateEntry}
                    onClose={() => setEditingEntry(null)}
                />
            )}
            <CrisisAlert
                isVisible={showCrisisAlert}
                reason={crisisData.reason}
                resources={crisisData.resources}
                onClose={() => setShowCrisisAlert(false)}
            />
        </div>
    );
}

export default App;
