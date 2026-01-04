import { useState } from 'react';
import { exportService } from '../services/exportService';
import './DataExport.css';

interface DataExportProps {
    token: string;
}

export function DataExport({ token }: DataExportProps) {
    const [isExporting, setIsExporting] = useState(false);
    const [exportError, setExportError] = useState<string | null>(null);
    const [exportSuccess, setExportSuccess] = useState<string | null>(null);

    const handleExport = async (format: 'json' | 'csv') => {
        setIsExporting(true);
        setExportError(null);
        setExportSuccess(null);

        try {
            const blob = await exportService.exportData(token, format);
            const fileName = `mental-health-journal-export-${new Date().toISOString().split('T')[0]}.${format}`;
            exportService.downloadFile(blob, fileName);
            
            setExportSuccess(`Successfully exported your data as ${format.toUpperCase()}`);
            
            // Clear success message after 5 seconds
            setTimeout(() => setExportSuccess(null), 5000);
        } catch (error) {
            console.error('Export error:', error);
            setExportError(error instanceof Error ? error.message : 'Failed to export data');
        } finally {
            setIsExporting(false);
        }
    };

    return (
        <div className="data-export">
            <h2>📦 Export Your Data</h2>
            <p className="export-description">
                Download all your journal entries, including text, sentiment analysis, 
                key phrases, and AI-generated insights.
            </p>

            <div className="export-formats">
                <div className="export-option">
                    <div className="export-icon">📄</div>
                    <h3>JSON Format</h3>
                    <p>Complete structured data with all metadata</p>
                    <button 
                        className="export-button json-button"
                        onClick={() => handleExport('json')}
                        disabled={isExporting}
                    >
                        {isExporting ? 'Exporting...' : 'Export as JSON'}
                    </button>
                </div>

                <div className="export-option">
                    <div className="export-icon">📊</div>
                    <h3>CSV Format</h3>
                    <p>Spreadsheet-compatible format for analysis</p>
                    <button 
                        className="export-button csv-button"
                        onClick={() => handleExport('csv')}
                        disabled={isExporting}
                    >
                        {isExporting ? 'Exporting...' : 'Export as CSV'}
                    </button>
                </div>
            </div>

            {exportSuccess && (
                <div className="export-message success">
                    ✅ {exportSuccess}
                </div>
            )}

            {exportError && (
                <div className="export-message error">
                    ❌ {exportError}
                </div>
            )}

            <div className="export-info">
                <h4>ℹ️ What's included in your export:</h4>
                <ul>
                    <li>All journal entries with timestamps</li>
                    <li>Entry text and voice transcriptions</li>
                    <li>Sentiment analysis and confidence scores</li>
                    <li>Extracted key phrases and topics</li>
                    <li>AI-generated summaries and affirmations</li>
                    <li>Audio file URLs (for voice entries)</li>
                </ul>
            </div>

            <div className="export-privacy">
                <h4>🔒 Privacy & Security</h4>
                <p>
                    Your data is exported directly to your device. No copies are stored 
                    or transmitted to third parties. All exports are generated in real-time 
                    and include only your personal journal entries.
                </p>
            </div>
        </div>
    );
}
