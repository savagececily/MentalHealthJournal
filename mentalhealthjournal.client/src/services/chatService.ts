const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

export interface ChatMessage {
    role: 'user' | 'assistant';
    content: string;
    timestamp: string;
}

export interface ChatSession {
    id: string;
    userId: string;
    messages: ChatMessage[];
    createdAt: string;
    lastMessageAt: string;
    title: string;
    isActive: boolean;
}

export interface ChatRequest {
    message: string;
    sessionId?: string;
}

export interface ChatResponse {
    sessionId: string;
    message: string;
    timestamp: string;
}

const getAuthHeader = () => {
    const token = localStorage.getItem('token');
    if (!token) throw new Error('Not authenticated');
    return {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
    };
};

export const chatService = {
    async sendMessage(request: ChatRequest): Promise<ChatResponse> {
        const response = await fetch(`${API_BASE_URL}/chat/message`, {
            method: 'POST',
            headers: getAuthHeader(),
            body: JSON.stringify(request),
        });

        if (!response.ok) {
            throw new Error('Failed to send message');
        }

        return response.json();
    },

    async getSession(sessionId: string): Promise<ChatSession> {
        const response = await fetch(`${API_BASE_URL}/chat/session/${sessionId}`, {
            headers: getAuthHeader(),
        });

        if (!response.ok) {
            throw new Error('Failed to get session');
        }

        return response.json();
    },

    async getSessions(): Promise<ChatSession[]> {
        const response = await fetch(`${API_BASE_URL}/chat/sessions`, {
            headers: getAuthHeader(),
        });

        if (!response.ok) {
            throw new Error('Failed to get sessions');
        }

        return response.json();
    },

    async deleteSession(sessionId: string): Promise<void> {
        const response = await fetch(`${API_BASE_URL}/chat/session/${sessionId}`, {
            method: 'DELETE',
            headers: getAuthHeader(),
        });

        if (!response.ok) {
            throw new Error('Failed to delete session');
        }
    },
};
