import { useState, useRef } from 'react';
import { audioRecordingService } from '../services/audioRecordingService';
import './VoiceRecorder.css';

interface VoiceRecorderProps {
    onRecordingComplete: (audioBlob: Blob) => void;
    disabled?: boolean;
}

export function VoiceRecorder({ onRecordingComplete, disabled }: VoiceRecorderProps) {
    const [isRecording, setIsRecording] = useState(false);
    const [recordingTime, setRecordingTime] = useState(0);
    const timerRef = useRef<NodeJS.Timeout | null>(null);

    const startRecording = async () => {
        try {
            await audioRecordingService.startRecording();
            setIsRecording(true);
            setRecordingTime(0);

            timerRef.current = setInterval(() => {
                setRecordingTime(prev => prev + 1);
            }, 1000);
        } catch (error) {
            alert(error instanceof Error ? error.message : 'Failed to start recording');
        }
    };

    const stopRecording = async () => {
        try {
            const audioBlob = await audioRecordingService.stopRecording();
            setIsRecording(false);
            
            if (timerRef.current) {
                clearInterval(timerRef.current);
                timerRef.current = null;
            }
            
            setRecordingTime(0);
            onRecordingComplete(audioBlob);
        } catch (error) {
            console.error('Error stopping recording:', error);
            alert('Failed to stop recording');
        }
    };

    const formatTime = (seconds: number): string => {
        const mins = Math.floor(seconds / 60);
        const secs = seconds % 60;
        return `${mins}:${secs.toString().padStart(2, '0')}`;
    };

    return (
        <div className="voice-recorder">
            {!isRecording ? (
                <button
                    className="record-button"
                    onClick={startRecording}
                    disabled={disabled}
                    title="Start voice recording"
                >
                    <span className="mic-icon">🎤</span> Record Voice Entry
                </button>
            ) : (
                <div className="recording-controls">
                    <div className="recording-indicator">
                        <span className="pulse-dot"></span>
                        <span className="recording-time">{formatTime(recordingTime)}</span>
                    </div>
                    <button
                        className="stop-button"
                        onClick={stopRecording}
                        title="Stop recording"
                    >
                        <span className="stop-icon">⏹️</span> Stop
                    </button>
                </div>
            )}
        </div>
    );
}
