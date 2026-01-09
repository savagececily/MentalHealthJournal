import { useState } from 'react';
import './EditEntryModal.css';

interface EditEntryModalProps {
    entryId: string;
    currentText: string;
    onSave: (entryId: string, newText: string) => Promise<void>;
    onClose: () => void;
}

export function EditEntryModal({ entryId, currentText, onSave, onClose }: EditEntryModalProps) {
    const [text, setText] = useState(currentText);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleSave = async () => {
        if (!text.trim()) {
            setError('Entry text cannot be empty');
            return;
        }

        if (text.length > 10000) {
            setError('Text exceeds maximum length of 10,000 characters');
            return;
        }

        setIsSaving(true);
        setError(null);

        try {
            await onSave(entryId, text);
            onClose();
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to save entry');
        } finally {
            setIsSaving(false);
        }
    };

    const handleBackdropClick = (e: React.MouseEvent) => {
        if (e.target === e.currentTarget) {
            onClose();
        }
    };

    return (
        <div className="modal-backdrop" onClick={handleBackdropClick}>
            <div className="edit-entry-modal">
                <div className="modal-header">
                    <h2>Edit Journal Entry</h2>
                    <button 
                        className="close-button" 
                        onClick={onClose}
                        aria-label="Close modal"
                    >
                        ✕
                    </button>
                </div>

                <div className="modal-body">
                    <div className="textarea-wrapper">
                        <textarea
                            className="edit-textarea"
                            value={text}
                            onChange={(e) => setText(e.target.value)}
                            rows={15}
                            disabled={isSaving}
                            aria-label="Edit journal entry text"
                            maxLength={10000}
                        />
                        {text.length > 0 && (
                            <div className="character-count" aria-live="polite">
                                {text.length} / 10,000 characters
                            </div>
                        )}
                    </div>

                    {error && (
                        <div className="error-message" role="alert">
                            {error}
                        </div>
                    )}

                    <div className="info-message">
                        <p>💡 Your entry will be re-analyzed by AI to update sentiment, key phrases, and affirmations.</p>
                    </div>
                </div>

                <div className="modal-footer">
                    <button 
                        className="cancel-button" 
                        onClick={onClose}
                        disabled={isSaving}
                    >
                        Cancel
                    </button>
                    <button 
                        className="save-button" 
                        onClick={handleSave}
                        disabled={isSaving || !text.trim()}
                    >
                        {isSaving ? '💾 Saving...' : '💾 Save Changes'}
                    </button>
                </div>
            </div>
        </div>
    );
}
