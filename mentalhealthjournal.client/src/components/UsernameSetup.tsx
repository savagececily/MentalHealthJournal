import React, { useState, useEffect } from 'react';
import { useAuth } from '../hooks/useAuth';
import { authService } from '../services/authService';
import './UsernameSetup.css';

interface UsernameSetupProps {
    onComplete: () => void;
}

const UsernameSetup: React.FC<UsernameSetupProps> = ({ onComplete }) => {
    const { token, updateUser } = useAuth();
    const [username, setUsername] = useState('');
    const [isChecking, setIsChecking] = useState(false);
    const [isAvailable, setIsAvailable] = useState<boolean | null>(null);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState('');

    useEffect(() => {
        let isCancelled = false;
        
        const checkAvailability = async () => {
            if (!token || username.length < 3) {
                setIsAvailable(null);
                return;
            }

            setIsChecking(true);
            try {
                const available = await authService.checkUsernameAvailability(token, username);
                if (!isCancelled) {
                    setIsAvailable(available);
                }
            } catch (err) {
                console.error('Error checking username:', err);
                if (!isCancelled) {
                    setIsAvailable(null);
                }
            } finally {
                if (!isCancelled) {
                    setIsChecking(false);
                }
            }
        };

        const debounce = setTimeout(checkAvailability, 500);
        return () => {
            clearTimeout(debounce);
            isCancelled = true;
        };
    }, [username, token]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        
        if (!token || !username.trim()) return;

        if (username.length < 3 || username.length > 20) {
            setError('Username must be between 3 and 20 characters');
            return;
        }

        // Validate username format: alphanumeric and underscores only
        if (!/^[a-z0-9_]+$/.test(username)) {
            setError('Username can only contain lowercase letters, numbers, and underscores');
            return;
        }

        if (!isAvailable) {
            setError('Username is not available');
            return;
        }

        setIsSaving(true);
        setError('');

        try {
            const updatedUser = await authService.updateUsername(token, username);
            updateUser(updatedUser);
            onComplete();
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to set username');
        } finally {
            setIsSaving(false);
        }
    };

    const getUsernameStatus = () => {
        if (username.length < 3) return '';
        if (isChecking) return '⏳ Checking...';
        if (isAvailable === true) return '✓ Available';
        if (isAvailable === false) return '✗ Taken';
        return '';
    };

    return (
        <div className="username-setup-overlay">
            <div className="username-setup-modal">
                <h2>Choose Your Username</h2>
                <p className="username-subtitle">Pick a unique username to personalize your journal</p>

                <form onSubmit={handleSubmit}>
                    <div className="username-input-group">
                        <input
                            type="text"
                            value={username}
                            onChange={(e) => setUsername(e.target.value.toLowerCase().replace(/[^a-z0-9_]/g, ''))}
                            placeholder="Enter username"
                            className="username-input"
                            maxLength={20}
                            autoFocus
                        />
                        <span className={`username-status ${isAvailable === true ? 'available' : isAvailable === false ? 'taken' : ''}`}>
                            {getUsernameStatus()}
                        </span>
                    </div>

                    {error && <p className="username-error">{error}</p>}

                    <div className="username-actions">
                        <button
                            type="button"
                            onClick={onComplete}
                            className="skip-button"
                        >
                            Skip for now
                        </button>
                        <button
                            type="submit"
                            disabled={!isAvailable || isSaving || username.length < 3}
                            className="save-username-button"
                        >
                            {isSaving ? 'Saving...' : 'Continue'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default UsernameSetup;
