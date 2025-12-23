import { useState, useEffect } from 'react';
import { useAppInsightsContext } from '@microsoft/applicationinsights-react-js';
import './App.css';

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
    const [entries, setEntries] = useState<JournalEntry[]>([]);
    const [journalText, setJournalText] = useState('');
    const [userId] = useState('demo-user');
    const [loading, setLoading] = useState(false);
    const [analyzing, setAnalyzing] = useState(false);
    const [showTrends, setShowTrends] = useState(false);
    const [trends, setTrends] = useState<TrendData | null>(null);

    useEffect(() => {
        loadEntries();
    }, []);

    useEffect(() => {
        if (entries.length > 0) {
            calculateTrends();
        }
    }, [entries]);

    async function loadEntries() {
        try {
            setLoading(true);
            const response = await fetch(`/api/journal?userId=${userId}`);
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
        if (!journalText.trim()) {
            alert('Please enter some text for your journal entry');
            return;
        }

        const startTime = Date.now();
        try {
            setAnalyzing(true);
            const response = await fetch('/api/journal/analyze', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    userId: userId,
                    text: journalText,
                }),
            });

            if (response.ok) {
                const newEntry = await response.json();
                setEntries([newEntry, ...entries]);
                setJournalText('');
                
                const duration = Date.now() - startTime;
                appInsights.trackEvent({ 
                    name: 'JournalEntrySubmitted', 
                    properties: { 
                        sentiment: newEntry.sentiment,
                        sentimentScore: newEntry.sentimentScore,
                        textLength: journalText.length,
                        duration
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

    return (
        <div className="app-container">
            <header className="app-header">
                <h1>🌱 Mental Health Journal</h1>
                <p>Track your thoughts, understand your emotions</p>
            </header>

            <div className="content-wrapper">
                <div className="sidebar">
                    <div className="new-entry-section">
                        <h2>New Journal Entry</h2>
                        <textarea
                            className="journal-input"
                            placeholder="How are you feeling today? Write your thoughts here..."
                            value={journalText}
                            onChange={(e) => setJournalText(e.target.value)}
                            rows={6}
                            disabled={analyzing}
                        />
                        <button 
                            className="submit-button" 
                            onClick={submitEntry}
                            disabled={analyzing || !journalText.trim()}
                        >
                            {analyzing ? 'Analyzing with AI...' : 'Save & Analyze Entry'}
                        </button>
                        {analyzing && (
                            <div className="analyzing-info">
                                <p>🤖 AI is analyzing your entry for sentiment, key phrases, and generating insights...</p>
                            </div>
                        )}
                    </div>

                    {trends && entries.length > 0 && (
                        <div className="trends-section">
                            <div className="trends-header">
                                <h2>📊 Your Trends</h2>
                                <button 
                                    className="toggle-trends"
                                    onClick={() => setShowTrends(!showTrends)}
                                >
                                    {showTrends ? 'Hide' : 'Show'}
                                </button>
                            </div>
                            
                            {showTrends && (
                                <>
                                    <div className="trend-stat">
                                        <span className="stat-label">Total Entries</span>
                                        <span className="stat-value">{trends.totalEntries}</span>
                                    </div>
                                    
                                    <div className="trend-stat">
                                        <span className="stat-label">Recent Trend</span>
                                        <span className="stat-value">
                                            {getTrendEmoji(trends.recentTrend)} {trends.recentTrend}
                                        </span>
                                    </div>

                                    <div className="sentiment-chart">
                                        <h3>Sentiment Distribution</h3>
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
                                                            <span className="chart-count">{count}</span>
                                                        </div>
                                                    </div>
                                                ) : null;
                                            })}
                                        </div>
                                    </div>
                                </>
                            )}
                        </div>
                    )}
                </div>

                <div className="entries-section">
                    <h2>Your Journal Entries</h2>
                    {loading ? (
                        <p className="loading-text">Loading your entries...</p>
                    ) : entries.length === 0 ? (
                        <div className="empty-state">
                            <p>No journal entries yet.</p>
                            <p>Start writing to track your mental wellness and see AI-powered insights!</p>
                            <div className="features-preview">
                                <div className="feature">✨ Sentiment Analysis</div>
                                <div className="feature">🔑 Key Phrase Extraction</div>
                                <div className="feature">📝 AI-Generated Summaries</div>
                                <div className="feature">💫 Personal Affirmations</div>
                            </div>
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
                                            <strong>�� AI Summary:</strong> {entry.summary}
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
            </div>
        </div>
    );
}

export default App;
