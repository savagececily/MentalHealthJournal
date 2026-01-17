import { useState, useEffect, useRef } from 'react';
import { chatService } from '../services/chatService';
import './VirtualTherapist.css';

interface ChatMessage {
    role: 'user' | 'assistant';
    content: string;
    timestamp: string;
}

interface ChatSession {
    id: string;
    title: string;
    messages: ChatMessage[];
    lastMessageAt: string;
}

export const VirtualTherapist = () => {
    const [sessions, setSessions] = useState<ChatSession[]>([]);
    const [currentSession, setCurrentSession] = useState<ChatSession | null>(null);
    const [message, setMessage] = useState('');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const messagesEndRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        loadSessions();
    }, []);

    useEffect(() => {
        scrollToBottom();
    }, [currentSession?.messages]);

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    };

    const loadSessions = async () => {
        try {
            const data = await chatService.getSessions();
            setSessions(data);
        } catch (err) {
            console.error('Error loading sessions:', err);
            setError('Failed to load chat sessions');
        }
    };

    const startNewSession = () => {
        setCurrentSession(null);
        setMessage('');
        setError(null);
    };

    const loadSession = async (sessionId: string) => {
        try {
            const session = await chatService.getSession(sessionId);
            setCurrentSession(session);
            setError(null);
        } catch (err) {
            console.error('Error loading session:', err);
            setError('Failed to load session');
        }
    };

    const sendMessage = async (e: React.FormEvent) => {
        e.preventDefault();
        
        if (!message.trim() || loading) return;

        setLoading(true);
        setError(null);

        try {
            const response = await chatService.sendMessage({
                message: message.trim(),
                sessionId: currentSession?.id
            });

            // Update current session with new messages
            const updatedSession = await chatService.getSession(response.sessionId);
            setCurrentSession(updatedSession);
            
            // Reload sessions list
            await loadSessions();
            
            setMessage('');
        } catch (err) {
            console.error('Error sending message:', err);
            setError('Failed to send message. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    const deleteSession = async (sessionId: string) => {
        if (!confirm('Are you sure you want to delete this conversation?')) return;

        try {
            await chatService.deleteSession(sessionId);
            if (currentSession?.id === sessionId) {
                setCurrentSession(null);
            }
            await loadSessions();
        } catch (err) {
            console.error('Error deleting session:', err);
            setError('Failed to delete session');
        }
    };

    const formatTime = (timestamp: string) => {
        return new Date(timestamp).toLocaleTimeString('en-US', {
            hour: 'numeric',
            minute: '2-digit'
        });
    };

    const formatDate = (timestamp: string) => {
        return new Date(timestamp).toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric'
        });
    };

    return (
        <div className="virtual-therapist">
            <div className="sidebar">
                <div className="sidebar-header">
                    <h2>Conversations</h2>
                    <button 
                        className="new-chat-btn"
                        onClick={startNewSession}
                        title="Start new conversation"
                    >
                        + New Chat
                    </button>
                </div>
                
                <div className="sessions-list">
                    {sessions.length === 0 ? (
                        <p className="no-sessions">No conversations yet</p>
                    ) : (
                        sessions.map((session) => (
                            <div
                                key={session.id}
                                className={`session-item ${currentSession?.id === session.id ? 'active' : ''}`}
                            >
                                <div 
                                    className="session-content"
                                    onClick={() => loadSession(session.id)}
                                >
                                    <div className="session-title">{session.title}</div>
                                    <div className="session-date">
                                        {formatDate(session.lastMessageAt)}
                                    </div>
                                </div>
                                <button
                                    className="delete-session-btn"
                                    onClick={(e) => {
                                        e.stopPropagation();
                                        deleteSession(session.id);
                                    }}
                                    title="Delete conversation"
                                >
                                    🗑️
                                </button>
                            </div>
                        ))
                    )}
                </div>
            </div>

            <div className="chat-container">
                <div className="chat-header">
                    <h1>Virtual Support Companion</h1>
                    <p className="disclaimer">
                        This is a supportive AI companion, not a replacement for professional therapy.
                        If you're in crisis, please contact a crisis hotline.
                    </p>
                </div>

                {error && (
                    <div className="error-banner">
                        {error}
                        <button onClick={() => setError(null)}>×</button>
                    </div>
                )}

                <div className="messages-container">
                    {!currentSession && (
                        <div className="welcome-message">
                            <h2>Welcome! How can I support you today?</h2>
                            <p>Feel free to share what's on your mind. I'm here to listen without judgment.</p>
                            <div className="starter-prompts">
                                <button onClick={() => setMessage("I've been feeling anxious lately")}>
                                    "I've been feeling anxious lately"
                                </button>
                                <button onClick={() => setMessage("I'm struggling with stress")}>
                                    "I'm struggling with stress"
                                </button>
                                <button onClick={() => setMessage("I need help with my thoughts")}>
                                    "I need help with my thoughts"
                                </button>
                            </div>
                        </div>
                    )}

                    {currentSession?.messages
                        .filter(msg => msg.role !== 'system')
                        .map((msg, index) => (
                            <div 
                                key={index} 
                                className={`message ${msg.role}`}
                            >
                                <div className="message-content">
                                    {msg.content}
                                </div>
                                <div className="message-time">
                                    {formatTime(msg.timestamp)}
                                </div>
                            </div>
                        ))}

                    {loading && (
                        <div className="message assistant">
                            <div className="message-content typing">
                                <span></span>
                                <span></span>
                                <span></span>
                            </div>
                        </div>
                    )}

                    <div ref={messagesEndRef} />
                </div>

                <form className="message-input-container" onSubmit={sendMessage}>
                    <textarea
                        value={message}
                        onChange={(e) => setMessage(e.target.value)}
                        placeholder="Share what's on your mind..."
                        disabled={loading}
                        rows={3}
                        onKeyDown={(e) => {
                            if (e.key === 'Enter' && !e.shiftKey) {
                                e.preventDefault();
                                sendMessage(e);
                            }
                        }}
                    />
                    <button 
                        type="submit" 
                        disabled={!message.trim() || loading}
                        className="send-btn"
                    >
                        {loading ? 'Sending...' : 'Send'}
                    </button>
                </form>
            </div>
        </div>
    );
};
