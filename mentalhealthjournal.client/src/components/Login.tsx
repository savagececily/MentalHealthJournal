import React from 'react';
import { GoogleLogin, type CredentialResponse } from '@react-oauth/google';
import { useAuth } from '../contexts/AuthContext';
import { authService } from '../services/authService';
import './Login.css';

const Login: React.FC = () => {
    const { login } = useAuth();

    const handleGoogleSuccess = async (credentialResponse: CredentialResponse) => {
        try {
            if (!credentialResponse.credential) {
                console.error('No credential in response');
                return;
            }

            const authResponse = await authService.loginWithGoogle(credentialResponse.credential);
            login(authResponse);
        } catch (error) {
            console.error('Login failed:', error);
            alert('Login failed. Please try again.');
        }
    };

    const handleGoogleError = () => {
        console.error('Google login failed');
        alert('Google login failed. Please try again.');
    };

    return (
        <div className="login-container">
            <div className="login-card">
                <div className="login-header">
                    <h1>Inside Journal</h1>
                    <p>Your private space for mental wellness</p>
                </div>
                
                <div className="login-content">
                    <h2>Welcome</h2>
                    <p className="login-subtitle">Sign in to access your journal</p>
                    
                    <div className="google-login-wrapper">
                        <GoogleLogin
                            onSuccess={handleGoogleSuccess}
                            onError={handleGoogleError}
                            useOneTap
                            theme="filled_blue"
                            size="large"
                            text="signin_with"
                            shape="rectangular"
                        />
                    </div>
                </div>

                <div className="login-footer">
                    <p>By signing in, you agree to our terms of service and privacy policy.</p>
                </div>
            </div>
        </div>
    );
};

export default Login;
