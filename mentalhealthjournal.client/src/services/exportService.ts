const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

export const exportService = {
    async exportData(token: string, format: 'json' | 'csv'): Promise<Blob> {
        const response = await fetch(`${API_BASE_URL}/journal/export/${format}`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
            },
        });

        if (!response.ok) {
            const error = await response.text();
            throw new Error(`Export failed: ${error}`);
        }

        return response.blob();
    },

    downloadFile(blob: Blob, fileName: string) {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
    }
};
