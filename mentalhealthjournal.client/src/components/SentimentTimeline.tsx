import React, { useMemo } from 'react';
import './SentimentTimeline.css';

interface JournalEntry {
    id: string;
    timestamp: string;
    sentiment: string;
    sentimentScore: number;
}

interface SentimentTimelineProps {
    entries: JournalEntry[];
}

interface DataPoint {
    date: string;
    score: number;
    sentiment: string;
    count: number;
}

export const SentimentTimeline: React.FC<SentimentTimelineProps> = ({ entries }) => {
    const timelineData = useMemo(() => {
        if (entries.length === 0) return [];

        // Group entries by date and calculate average sentiment score
        const groupedByDate = new Map<string, { scores: number[]; sentiments: string[] }>();

        entries.forEach(entry => {
            const dateObj = new Date(entry.timestamp);
            if (isNaN(dateObj.getTime())) {
                // Skip entries with invalid or malformed timestamps
                return;
            }
            const date = dateObj.toISOString().split('T')[0];
            if (!groupedByDate.has(date)) {
                groupedByDate.set(date, { scores: [], sentiments: [] });
            }
            const group = groupedByDate.get(date)!;
            group.scores.push(entry.sentimentScore);
            group.sentiments.push(entry.sentiment);
        });

        // Convert to data points and sort by date
        const dataPoints: DataPoint[] = Array.from(groupedByDate.entries())
            .map(([date, data]) => {
                const avgScore = data.scores.reduce((sum, score) => sum + score, 0) / data.scores.length;
                // Determine dominant sentiment
                const sentimentCounts = data.sentiments.reduce((acc, s) => {
                    acc[s.toLowerCase()] = (acc[s.toLowerCase()] || 0) + 1;
                    return acc;
                }, {} as Record<string, number>);
                const dominantSentiment = Object.entries(sentimentCounts)
                    .sort(([, a], [, b]) => b - a)[0][0];
                
                return {
                    date,
                    score: avgScore,
                    sentiment: dominantSentiment,
                    count: data.scores.length
                };
            })
            .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())
            .slice(-30); // Last 30 days with entries

        return dataPoints;
    }, [entries]);

    const chartHeight = 200;
    const chartWidth = 100; // percentage

    if (timelineData.length === 0) {
        return (
            <div className="sentiment-timeline-empty">
                <p>No timeline data available yet. Create more entries!</p>
            </div>
        );
    }

    const getSentimentColor = (sentiment: string): string => {
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
    };

    const formatDate = (dateStr: string): string => {
        const date = new Date(dateStr);
        return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    };

    const pointSpacing = chartWidth / Math.max(timelineData.length - 1, 1);

    return (
        <div className="sentiment-timeline">
            <div className="timeline-header">
                <h3>📈 Sentiment Over Time</h3>
                <p className="timeline-subtitle">Your emotional journey over the last {timelineData.length} days with entries</p>
            </div>

            <div className="timeline-chart-container">
                <div className="timeline-y-axis">
                    <span className="y-label">Positive</span>
                    <span className="y-label">Neutral</span>
                    <span className="y-label">Negative</span>
                </div>

                <div className="timeline-chart" style={{ height: `${chartHeight}px` }}>
                    {/* Grid lines */}
                    <div className="grid-lines">
                        <div className="grid-line" style={{ top: '0%' }}></div>
                        <div className="grid-line" style={{ top: '50%' }}></div>
                        <div className="grid-line" style={{ top: '100%' }}></div>
                    </div>

                    {/* Line path */}
                    <svg className="timeline-svg" viewBox={`0 0 ${chartWidth} ${chartHeight}`} preserveAspectRatio="none">
                        <defs>
                            <linearGradient id="sentimentGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                                <stop offset="0%" style={{ stopColor: '#4caf50', stopOpacity: 0.3 }} />
                                <stop offset="100%" style={{ stopColor: '#f44336', stopOpacity: 0.3 }} />
                            </linearGradient>
                        </defs>
                        
                        {/* Area under the line */}
                        <path
                            d={`
                                M 0,${chartHeight}
                                ${timelineData.map((point, index) => {
                                    const x = index * pointSpacing;
                                    const y = chartHeight - (point.score * chartHeight);
                                    return `L ${x},${y}`;
                                }).join(' ')}
                                L ${(timelineData.length - 1) * pointSpacing},${chartHeight}
                                Z
                            `}
                            fill="url(#sentimentGradient)"
                            opacity="0.3"
                        />

                        {/* Line */}
                        <path
                            d={timelineData.map((point, index) => {
                                const x = index * pointSpacing;
                                const y = chartHeight - (point.score * chartHeight);
                                return `${index === 0 ? 'M' : 'L'} ${x},${y}`;
                            }).join(' ')}
                            fill="none"
                            stroke="#667eea"
                            strokeWidth="3"
                            strokeLinecap="round"
                            strokeLinejoin="round"
                        />
                    </svg>

                    {/* Data points */}
                    {timelineData.map((point, index) => {
                        const x = (index * pointSpacing);
                        const y = 100 - (point.score * 100);
                        return (
                            <div
                                key={point.date}
                                className="timeline-point"
                                style={{
                                    left: `${x}%`,
                                    top: `${y}%`,
                                    backgroundColor: getSentimentColor(point.sentiment)
                                }}
                                title={`${formatDate(point.date)}: ${point.sentiment} (${point.count} ${point.count === 1 ? 'entry' : 'entries'})`}
                            >
                                <div className="point-tooltip">
                                    <div className="tooltip-date">{formatDate(point.date)}</div>
                                    <div className="tooltip-sentiment">{point.sentiment}</div>
                                    <div className="tooltip-count">{point.count} {point.count === 1 ? 'entry' : 'entries'}</div>
                                </div>
                            </div>
                        );
                    })}
                </div>

                <div className="timeline-x-axis">
                    {timelineData.filter((_, i) => i % Math.ceil(timelineData.length / 6) === 0 || i === timelineData.length - 1).map((point, index) => (
                        <span key={index} className="x-label">{formatDate(point.date)}</span>
                    ))}
                </div>
            </div>

            <div className="timeline-legend">
                <div className="legend-item">
                    <div className="legend-dot" style={{ backgroundColor: '#4caf50' }}></div>
                    <span>Positive</span>
                </div>
                <div className="legend-item">
                    <div className="legend-dot" style={{ backgroundColor: '#9e9e9e' }}></div>
                    <span>Neutral</span>
                </div>
                <div className="legend-item">
                    <div className="legend-dot" style={{ backgroundColor: '#ff9800' }}></div>
                    <span>Mixed</span>
                </div>
                <div className="legend-item">
                    <div className="legend-dot" style={{ backgroundColor: '#f44336' }}></div>
                    <span>Negative</span>
                </div>
            </div>
        </div>
    );
};
