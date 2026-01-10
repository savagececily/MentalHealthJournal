import { useState, useEffect, useCallback } from 'react';
import './StreakCounter.css';

interface StreakData {
    currentStreak: number;
    longestStreak: number;
    calculatedAt: string;
}

interface StreakCounterProps {
    token: string;
}

export function StreakCounter({ token }: StreakCounterProps) {
    const [streakData, setStreakData] = useState<StreakData | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const loadStreakData = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const response = await fetch('/api/journal/streak', {
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            });

            if (!response.ok) {
                throw new Error('Failed to load streak data');
            }

            const data = await response.json();
            setStreakData(data);
        } catch (err) {
            console.error('Error loading streak data:', err);
            setError(err instanceof Error ? err.message : 'Failed to load streak');
        } finally {
            setLoading(false);
        }
    }, [token]);

    useEffect(() => {
        loadStreakData();
    }, [loadStreakData]);

    // Allow manual refresh
    const refreshStreak = () => {
        loadStreakData();
    };

    if (loading) {
        return (
            <div className="streak-counter loading">
                <div className="streak-spinner"></div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="streak-counter error">
                <p>Unable to load streak</p>
                <button onClick={refreshStreak} className="retry-button">
                    Retry
                </button>
            </div>
        );
    }

    if (!streakData) return null;

    const getStreakMessage = (streak: number): string => {
        if (streak === 0) return "Start your journey today! 🌱";
        if (streak === 1) return "Great start! Keep it up! 🎯";
        if (streak < 7) return "Building momentum! 🔥";
        if (streak < 30) return "Amazing consistency! 🌟";
        if (streak < 100) return "You're on fire! 🔥💪";
        return "Legendary streak! 🏆👑";
    };

    return (
        <div className="streak-counter">
            <div className="streak-main">
                <div className="streak-item current">
                    <div className="streak-icon">🔥</div>
                    <div className="streak-content">
                        <div className="streak-value">{streakData.currentStreak}</div>
                        <div className="streak-label">Day Streak</div>
                    </div>
                </div>

                <div className="streak-divider"></div>

                <div className="streak-item longest">
                    <div className="streak-icon">🏆</div>
                    <div className="streak-content">
                        <div className="streak-value">{streakData.longestStreak}</div>
                        <div className="streak-label">Best Streak</div>
                    </div>
                </div>
            </div>

            <div className="streak-message">
                {getStreakMessage(streakData.currentStreak)}
            </div>

            <button 
                onClick={refreshStreak} 
                className="refresh-streak-button"
                aria-label="Refresh streak data"
                title="Refresh streak data"
            >
                ↻
            </button>
        </div>
    );
}
