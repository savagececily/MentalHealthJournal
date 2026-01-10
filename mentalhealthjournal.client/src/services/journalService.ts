const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

export const journalService = {
    async updateEntry(token: string, entryId: string, text: string) {
        const response = await fetch(`${API_BASE_URL}/journal/${entryId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
            },
            body: JSON.stringify({ text }),
        });

        if (!response.ok) {
            const error = await response.text();
            throw new Error(`Failed to update entry: ${error}`);
        }

        return response.json();
    },

    async deleteEntry(token: string, entryId: string) {
        const response = await fetch(`${API_BASE_URL}/journal/${entryId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${token}`,
            },
        });

        if (!response.ok) {
            const error = await response.text();
            throw new Error(`Failed to delete entry: ${error}`);
        }

        return; // 204 No Content
    }
};
