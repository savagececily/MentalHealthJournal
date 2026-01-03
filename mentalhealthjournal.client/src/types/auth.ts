export interface User {
    id: string;
    email: string;
    name: string;
    username?: string;
    profilePictureUrl?: string;
    provider: string;
    providerId: string;
}

export interface AuthResponse {
    token: string;
    user: User;
}
