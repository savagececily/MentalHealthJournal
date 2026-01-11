import React, { useMemo } from 'react';
import './TimePatterns.css';

interface JournalEntry {
    timestamp: string;
    sentiment: string;
}

interface TimePatternsProps {
    entries: JournalEntry[];
}

export const TimePatterns: React.FC<TimePatternsProps> = ({ entries }) => {
    const { hourlyData, dayOfWeekData, bestTime, mostProductiveDay } = useMemo(() => {
        if (entries.length === 0) {
            return { hourlyData: [], dayOfWeekData: [], bestTime: null, mostProductiveDay: null };
        }

        // Initialize hourly data (24 hours)
        const hours = Array.from({ length: 24 }, (_, i) => ({
            slot: `${i}:00`,
            hour: i,
            count: 0,
            sentiments: { positive: 0, negative: 0, neutral: 0, mixed: 0 }
        }));

        // Initialize day of week data
        const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'].map(day => ({
            slot: day,
            count: 0,
            sentiments: { positive: 0, negative: 0, neutral: 0, mixed: 0 }
        }));

        // Process entries
        entries.forEach(entry => {
            const date = new Date(entry.timestamp);
            const hour = date.getHours();
            const dayOfWeek = date.getDay();
            const sentiment = entry.sentiment.toLowerCase() as 'positive' | 'negative' | 'neutral' | 'mixed';

            hours[hour].count++;
            hours[hour].sentiments[sentiment]++;

            days[dayOfWeek].count++;
            days[dayOfWeek].sentiments[sentiment]++;
        });

        // Find best time (highest positive sentiment ratio)
        const bestTimeSlot = hours
            .filter(h => h.count > 0)
            .sort((a, b) => {
                const aRatio = a.sentiments.positive / a.count;
                const bRatio = b.sentiments.positive / b.count;
                return bRatio - aRatio;
            })[0];

        // Find most productive day
        const mostActiveDay = days
            .sort((a, b) => b.count - a.count)[0];

        return {
            hourlyData: hours,
            dayOfWeekData: days,
            bestTime: bestTimeSlot,
            mostProductiveDay: mostActiveDay
        };
    }, [entries]);

    if (entries.length === 0) {
        return (
            <div className="time-patterns-empty">
                <p>No timing data available yet. Create more entries to discover your patterns!</p>
            </div>
        );
    }

    const getTimeOfDayLabel = (hour: number): string => {
        if (hour >= 5 && hour < 12) return 'Morning';
        if (hour >= 12 && hour < 17) return 'Afternoon';
        if (hour >= 17 && hour < 21) return 'Evening';
        return 'Night';
    };

    const maxHourlyCount = Math.max(...hourlyData.map(h => h.count));
    const maxDayCount = Math.max(...dayOfWeekData.map(d => d.count));

    return (
        <div className="time-patterns">
            <div className="patterns-header">
                <h3>⏰ Journaling Patterns</h3>
                <p className="patterns-subtitle">Discover when you tend to journal and how your mood varies by time</p>
            </div>

            {/* Insights Summary */}
            <div className="patterns-insights">
                {bestTime && (
                    <div className="insight-box">
                        <div className="insight-icon">🌟</div>
                        <div className="insight-content">
                            <div className="insight-title">Best Time</div>
                            <div className="insight-value">
                                {bestTime.hour === 0 ? '12' : bestTime.hour > 12 ? bestTime.hour - 12 : bestTime.hour}
                                {bestTime.hour >= 12 ? ' PM' : ' AM'}
                            </div>
                            <div className="insight-label">{getTimeOfDayLabel(bestTime.hour)}</div>
                        </div>
                    </div>
                )}
                {mostProductiveDay && (
                    <div className="insight-box">
                        <div className="insight-icon">📅</div>
                        <div className="insight-content">
                            <div className="insight-title">Most Active Day</div>
                            <div className="insight-value">{mostProductiveDay.slot}</div>
                            <div className="insight-label">{mostProductiveDay.count} entries</div>
                        </div>
                    </div>
                )}
            </div>

            {/* Hourly Distribution */}
            <div className="pattern-section">
                <h4>📊 Time of Day Distribution</h4>
                <div className="hourly-chart">
                    {hourlyData.map((data, index) => {
                        const height = maxHourlyCount > 0 ? (data.count / maxHourlyCount) * 100 : 0;
                        const dominantSentiment = data.count > 0
                            ? Object.entries(data.sentiments).sort(([, a], [, b]) => b - a)[0][0]
                            : 'neutral';
                        
                        return (
                            <div key={index} className="hour-bar-container">
                                <div
                                    className="hour-bar"
                                    style={{
                                        height: `${height}%`,
                                        backgroundColor: getSentimentColor(dominantSentiment)
                                    }}
                                    title={`${data.slot}: ${data.count} entries (${dominantSentiment})`}
                                >
                                    {data.count > 0 && <span className="bar-count">{data.count}</span>}
                                </div>
                                {index % 3 === 0 && (
                                    <div className="hour-label">
                                        {data.hour === 0 ? '12 AM' : data.hour < 12 ? `${data.hour} AM` : data.hour === 12 ? '12 PM' : `${data.hour - 12} PM`}
                                    </div>
                                )}
                            </div>
                        );
                    })}
                </div>
            </div>

            {/* Day of Week Distribution */}
            <div className="pattern-section">
                <h4>📆 Day of Week Distribution</h4>
                <div className="day-chart">
                    {dayOfWeekData.map((data, index) => {
                        const width = maxDayCount > 0 ? (data.count / maxDayCount) * 100 : 0;
                        const positiveRatio = data.count > 0 ? (data.sentiments.positive / data.count) * 100 : 0;
                        
                        return (
                            <div key={index} className="day-row">
                                <div className="day-label">{data.slot.substring(0, 3)}</div>
                                <div className="day-bar-wrapper">
                                    <div
                                        className="day-bar"
                                        style={{
                                            width: `${width}%`,
                                            background: `linear-gradient(90deg, #4caf50 0%, #4caf50 ${positiveRatio}%, #e0e0e0 ${positiveRatio}%, #e0e0e0 100%)`
                                        }}
                                    >
                                        <span className="day-count">{data.count}</span>
                                    </div>
                                </div>
                            </div>
                        );
                    })}
                </div>
            </div>
        </div>
    );
};

function getSentimentColor(sentiment: string): string {
    switch (sentiment.toLowerCase()) {
        case 'positive':
            return '#4caf50';
        case 'negative':
            return '#f44336';
        case 'neutral':
            return '#9e9e9e';
        case 'mixed':
            return '#ff9800';
        default:
            return '#2196f3';
    }
}
