import React, { useMemo } from 'react';
import './KeyPhrasesCloud.css';

interface JournalEntry {
    keyPhrases: string[];
    sentiment: string;
}

interface KeyPhrasesCloudProps {
    entries: JournalEntry[];
}

interface PhraseData {
    phrase: string;
    count: number;
    sentiments: string[];
}

export const KeyPhrasesCloud: React.FC<KeyPhrasesCloudProps> = ({ entries }) => {
    const phrasesData = useMemo(() => {
        if (entries.length === 0) return [];

        // Count phrase occurrences and track associated sentiments
        const phraseMap = new Map<string, { count: number; sentiments: string[] }>();

        entries.forEach(entry => {
            entry.keyPhrases.forEach(phrase => {
                const lowerPhrase = phrase.toLowerCase();
                if (!phraseMap.has(lowerPhrase)) {
                    phraseMap.set(lowerPhrase, { count: 0, sentiments: [] });
                }
                const data = phraseMap.get(lowerPhrase)!;
                data.count++;
                data.sentiments.push(entry.sentiment.toLowerCase());
            });
        });

        // Convert to array and sort by count
        const phrasesArray: PhraseData[] = Array.from(phraseMap.entries())
            .map(([phrase, data]) => ({
                phrase,
                count: data.count,
                sentiments: data.sentiments
            }))
            .sort((a, b) => b.count - a.count)
            .slice(0, 30); // Top 30 phrases

        return phrasesArray;
    }, [entries]);

    if (phrasesData.length === 0) {
        return (
            <div className="key-phrases-empty">
                <p>No key phrases yet. Write more journal entries to see your common themes!</p>
            </div>
        );
    }

    const maxCount = phrasesData[0]?.count || 1;
    const minCount = phrasesData[phrasesData.length - 1]?.count || 1;

    const getSentimentColor = (sentiments: string[]): string => {
        // Determine dominant sentiment
        const counts = sentiments.reduce((acc, s) => {
            acc[s] = (acc[s] || 0) + 1;
            return acc;
        }, {} as Record<string, number>);

        const dominant = Object.entries(counts).sort(([, a], [, b]) => b - a)[0][0];

        switch (dominant) {
            case 'positive':
                return '#4caf50';
            case 'negative':
                return '#f44336';
            case 'neutral':
                return '#757575';
            case 'mixed':
                return '#ff9800';
            default:
                return '#2196f3';
        }
    };

    const getFontSize = (count: number): number => {
        // Scale font size between 14px and 32px
        const minSize = 14;
        const maxSize = 32;
        const range = maxSize - minSize;
        const countRange = maxCount - minCount;
        
        if (countRange === 0) return maxSize;
        
        return minSize + ((count - minCount) / countRange) * range;
    };

    const getDominantSentiment = (sentiments: string[]): string => {
        const counts = sentiments.reduce((acc, s) => {
            acc[s] = (acc[s] || 0) + 1;
            return acc;
        }, {} as Record<string, number>);

        return Object.entries(counts).sort(([, a], [, b]) => b - a)[0][0];
    };

    return (
        <div className="key-phrases-cloud">
            <div className="phrases-header">
                <h3>💭 Common Themes</h3>
                <p className="phrases-subtitle">
                    The most frequent topics in your journal entries. Size indicates frequency, color shows dominant sentiment.
                </p>
            </div>

            <div className="phrases-cloud-container">
                {phrasesData.map((data, index) => {
                    const fontSize = getFontSize(data.count);
                    const color = getSentimentColor(data.sentiments);
                    const dominantSentiment = getDominantSentiment(data.sentiments);
                    
                    return (
                        <div
                            key={index}
                            className="phrase-item"
                            style={{
                                fontSize: `${fontSize}px`,
                                color: color
                            }}
                            title={`"${data.phrase}" - appears ${data.count} times, mostly in ${dominantSentiment} entries`}
                        >
                            {data.phrase}
                        </div>
                    );
                })}
            </div>

            <div className="phrases-stats">
                <div className="stat-item">
                    <span className="stat-value">{phrasesData.length}</span>
                    <span className="stat-label">Unique Themes</span>
                </div>
                <div className="stat-item">
                    <span className="stat-value">{phrasesData.reduce((sum, p) => sum + p.count, 0)}</span>
                    <span className="stat-label">Total Mentions</span>
                </div>
                <div className="stat-item">
                    <span className="stat-value">{phrasesData[0]?.phrase || 'N/A'}</span>
                    <span className="stat-label">Most Common</span>
                </div>
            </div>
        </div>
    );
};
