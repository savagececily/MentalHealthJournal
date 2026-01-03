import { useState, useEffect } from 'react';
import { useAppInsightsContext } from '@microsoft/applicationinsights-react-js';
import { useAuth } from './contexts/AuthContext';
import Login from './components/Login';
import UsernameSetup from './components/UsernameSetup';
import About from './About';
import { VoiceRecorder } from './components/VoiceRecorder';
import './App.css';
import './Tabs.css';

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
    const [analyzing, setAnalyzing] = useState(false);
    const [showAbout, setShowAbout] = useState(false);
    const [activeTab, setActiveTab] = useState<'new' | 'past' | 'insights'>('new');
    const [trends, setTrends] = useState<TrendData | null>(null);
    const [latestEntry, setLatestEntry] = useState<JournalEntry | null>(null);
    const [audioBlob, setAudioBlob] = useState<Blob | null>(null);
    const [isTranscribing, setIsTranscribing] = useState(false);
    const [showUsernameSetup, setShowUsernameSetup] = useState(false);

    useEffect(() => {
        if (isAuthenticated) {
            loadEntries();
            // Check if user needs to set username
            if (!user?.username) {
                setShowUsernameSetup(true);
            }
        }
    }, [isAuthenticated]);

    useEffect(() => {
        if (entries.length > 0) {
            calculateTrends();
        }
    }, [entries]);

    async function loadEntries() {
        if (!token) return;
        
        try {
            setLoading(true);
            const response = await fetch('/api/journal', {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });
            if (response.ok) {
                const data = await response.json();
                setEntries(data);
                appInsights.trackEvent({ name: 'EntriesLoaded', properties: { count: data.length } });
            }
        } catch (error) {
            console.error('Error loading entries:', error);
            appInsights.trackException({ exception: error as Error, properties: { action: 'loadEntries' } });
        } finally {
            setLoading(false);
        }
    }

    function calculateTrends() {
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
    }

    async function submitEntry() {
        if (!journalText.trim() && !audioBlob) {
            alert('Please enter text or record a voice entry');
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
                    throw new Error('Failed to process voice recording');
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
                
                const duration = Date.now() - startTime;
                appInsights.trackEvent({ 
                    name: 'JournalEntrySubmitted', 
                    properties: { 
                        sentiment: newEntry.sentiment,
                        sentimentScore: newEntry.sentimentScore,
                        textLength: textToSubmit.length,
                        duration,
                        isVoiceEntry
                    } 
                });
            } else {
                const errorText = await response.text();
                alert(`Failed to save journal entry: ${errorText}`);
                appInsights.trackEvent({ name: 'JournalSubmitFailed', properties: { error: errorText } });
            }
        } catch (error) {
            console.error('Error submitting entry:', error);
            alert('Error submitting entry. Please check that the backend is running.');
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
                        <div className="user-info">
                            {user?.profilePictureUrl && (
                                <img src={user.profilePictureUrl} alt={user.name} className="user-avatar" />
                            )}
                            <span className="user-name">{user?.username || user?.name}</span>
                        </div>
                        <button className="about-button" onClick={() => setShowAbout(true)}>
                            About
                        </button>
                        <button className="logout-button" onClick={logout}>
                            Logout
                        </button>
                    </div>
                </div>
            </header>

            <div className="tabs-container">
                <div className="tabs">
                    <button 
                        className={`tab ${activeTab === 'new' ? 'active' : ''}`}
                        onClick={() => setActiveTab('new')}
                    >
                        ✏️ New Entry
                    </button>
                    <button 
                        className={`tab ${activeTab === 'past' ? 'active' : ''}`}
                        onClick={() => setActiveTab('past')}
                    >
                        📚 Past Entries
                    </button>
                    <button 
                        className={`tab ${activeTab === 'insights' ? 'active' : ''}`}
                        onClick={() => setActiveTab('insights')}
                    >
                        📊 Insights
                    </button>
                </div>
            </div>

            <div className="tab-content">
                {activeTab === 'new' && (
                    <div className="new-entry-tab">
                        <div className="entry-form-section">
                            <h2>Write Your Thoughts</h2>
                            <textarea
                                className="journal-input"
                                placeholder="How are you feeling today? Write your thoughts here..."
                                value={journalText}
                                onChange={(e) => setJournalText(e.target.value)}
                                rows={10}
                                disabled={analyzing || isTranscribing}
                            />
                            
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

                            {audioBlob && (
                                <div className="audio-preview">
                                    <p>🎤 Voice recording ready</p>
                                    <audio controls src={URL.createObjectURL(audioBlob)} />
                                    <button 
                                        className="clear-audio-button"
                                        onClick={() => setAudioBlob(null)}
                                    >
                                        Clear Recording
                                    </button>
                                </div>
                            )}

                            <button 
                                className="submit-button" 
                                onClick={submitEntry}
                                disabled={analyzing || isTranscribing || (!journalText.trim() && !audioBlob)}
                            >
                                {isTranscribing ? '🎤 Transcribing audio...' : 
                                 analyzing ? '🤖 Analyzing with AI...' : 
                                 '✨ Save & Analyze Entry'}
                            </button>
                            {(analyzing || isTranscribing) && (
                                <div className="analyzing-info">
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
                                    </div>
                                ))}
                            </div>
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
            </div>

            {showAbout && <About onClose={() => setShowAbout(false)} />}
            {showUsernameSetup && <UsernameSetup onComplete={() => setShowUsernameSetup(false)} />}
        </div>
    );
}

export default App;
