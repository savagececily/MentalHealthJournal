import { useState, useEffect, useCallback } from 'react';
import './CalendarView.css';

interface CalendarEntry {
    date: string;
    count: number;
    entries: Array<{
        id: string;
        timestamp: string;
        sentiment: string;
        sentimentScore: number;
        summary: string;
    }>;
}

interface CalendarViewProps {
    token: string;
}

export function CalendarView({ token }: CalendarViewProps) {
    const [currentDate, setCurrentDate] = useState(new Date());
    const [calendarData, setCalendarData] = useState<CalendarEntry[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [selectedDate, setSelectedDate] = useState<CalendarEntry | null>(null);

    const loadCalendarData = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const year = currentDate.getFullYear();
            const month = currentDate.getMonth();
            const startDate = new Date(Date.UTC(year, month, 1, 0, 0, 0));
            const endDate = new Date(Date.UTC(year, month + 1, 0, 23, 59, 59));

            const response = await fetch(
                `/api/journal/calendar?startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`,
                {
                    headers: {
                        Authorization: `Bearer ${token}`,
                    },
                }
            );

            if (!response.ok) {
                throw new Error('Failed to load calendar data');
            }

            const data = await response.json();
            setCalendarData(data);
        } catch (err) {
            console.error('Error loading calendar data:', err);
            setError(err instanceof Error ? err.message : 'Failed to load calendar');
        } finally {
            setLoading(false);
        }
    }, [currentDate, token]);

    useEffect(() => {
        loadCalendarData();
    }, [loadCalendarData]);

    const getDaysInMonth = () => {
        const year = currentDate.getFullYear();
        const month = currentDate.getMonth();
        const firstDay = new Date(year, month, 1);
        const lastDay = new Date(year, month + 1, 0);
        const daysInMonth = lastDay.getDate();
        const startingDayOfWeek = firstDay.getDay();

        return { daysInMonth, startingDayOfWeek };
    };

    const getEntryForDate = (day: number): CalendarEntry | undefined => {
        const dateStr = new Date(currentDate.getFullYear(), currentDate.getMonth(), day).toISOString().split('T')[0];
        return calendarData.find(entry => entry.date.startsWith(dateStr));
    };

    const SENTIMENT_COLOR_MAP: { [key: string]: string } = {
        positive: '#4ade80',
        negative: '#f87171',
        neutral: '#94a3b8',
        mixed: '#fbbf24',
    };

    const getSentimentColor = (sentiment: string): string => {
        const normalizedSentiment = sentiment.toLowerCase();
        return SENTIMENT_COLOR_MAP[normalizedSentiment] ?? '#cbd5e1';
    };

    const renderCalendar = () => {
        const { daysInMonth, startingDayOfWeek } = getDaysInMonth();
        const days = [];
        const today = new Date();
        const isCurrentMonth = 
            today.getFullYear() === currentDate.getFullYear() &&
            today.getMonth() === currentDate.getMonth();

        // Empty cells for days before the first day of the month
        for (let i = 0; i < startingDayOfWeek; i++) {
            days.push(<div key={`empty-${i}`} className="calendar-day empty"></div>);
        }

        // Days of the month
        for (let day = 1; day <= daysInMonth; day++) {
            const entry = getEntryForDate(day);
            const isToday = isCurrentMonth && today.getDate() === day;
            const hasEntries = entry && entry.count > 0;

            days.push(
                <div
                    key={day}
                    className={`calendar-day ${isToday ? 'today' : ''} ${hasEntries ? 'has-entries' : ''}`}
                    onClick={() => hasEntries && setSelectedDate(entry)}
                    onKeyDown={(e) => {
                        if (!hasEntries) {
                            return;
                        }
                        if (e.key === 'Enter' || e.key === ' ') {
                            e.preventDefault();
                            setSelectedDate(entry);
                        }
                    }}
                    role="button"
                    tabIndex={hasEntries ? 0 : -1}
                    aria-label={`${day} ${hasEntries ? `- ${entry.count} entries` : ''}`}
                >
                    <div className="day-number">{day}</div>
                    {hasEntries && (
                        <div className="entry-indicator">
                            <div 
                                className="entry-dot"
                                style={{ backgroundColor: getSentimentColor(entry.entries[0].sentiment) }}
                            ></div>
                            {entry.count > 1 && <span className="entry-count">{entry.count}</span>}
                        </div>
                    )}
                </div>
            );
        }

        return days;
    };

    const previousMonth = () => {
        setCurrentDate(new Date(currentDate.getFullYear(), currentDate.getMonth() - 1, 1));
        setSelectedDate(null);
    };

    const nextMonth = () => {
        setCurrentDate(new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 1));
        setSelectedDate(null);
    };

    const monthNames = [
        'January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'
    ];

    const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

    return (
        <div className="calendar-view">
            <div className="calendar-header">
                <button onClick={previousMonth} aria-label="Previous month" className="nav-button">
                    ←
                </button>
                <h2>
                    {monthNames[currentDate.getMonth()]} {currentDate.getFullYear()}
                </h2>
                <button onClick={nextMonth} aria-label="Next month" className="nav-button">
                    →
                </button>
            </div>

            {loading && <div className="calendar-loading">Loading calendar...</div>}
            {error && <div className="calendar-error">{error}</div>}

            {!loading && !error && (
                <>
                    <div className="calendar-grid">
                        {dayNames.map(day => (
                            <div key={day} className="calendar-day-name">
                                {day}
                            </div>
                        ))}
                        {renderCalendar()}
                    </div>

                    {selectedDate && (
                        <div className="selected-date-details">
                            <h3>
                                {new Date(selectedDate.date).toLocaleDateString('en-US', {
                                    month: 'long',
                                    day: 'numeric',
                                    year: 'numeric'
                                })}
                            </h3>
                            <div className="entries-list">
                                {selectedDate.entries.map(entry => (
                                    <div key={entry.id} className="entry-summary">
                                        <div className="entry-summary-header">
                                            <span 
                                                className="sentiment-badge"
                                                style={{ backgroundColor: getSentimentColor(entry.sentiment) }}
                                            >
                                                {entry.sentiment}
                                            </span>
                                            <span className="entry-time">
                                                {new Date(entry.timestamp).toLocaleTimeString('en-US', {
                                                    hour: 'numeric',
                                                    minute: '2-digit'
                                                })}
                                            </span>
                                        </div>
                                        <p className="entry-summary-text">{entry.summary}</p>
                                    </div>
                                ))}
                            </div>
                            <button 
                                onClick={() => setSelectedDate(null)}
                                className="close-details-button"
                            >
                                Close
                            </button>
                        </div>
                    )}
                </>
            )}
        </div>
    );
}
