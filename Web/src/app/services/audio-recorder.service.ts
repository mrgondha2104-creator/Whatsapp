import { Injectable } from '@angular/core';
import { SoundService } from './sound.service';

@Injectable({
  providedIn: 'root'
})
export class AudioRecorderService {
  private mediaRecorder: MediaRecorder | null = null;
  private audioChunks: Blob[] = [];
  private stream: MediaStream | null = null;
  private timerInterval: any = null;

  public isRecording = false;
  public recordingSeconds = 0;
  public waveformBars: number[] = [];

  constructor(private soundService: SoundService) {}

  public async startRecording(): Promise<void> {
    try {
      this.soundService.playRecordBeep();
      this.stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      this.audioChunks = [];
      this.recordingSeconds = 0;
      this.waveformBars = [];

      this.mediaRecorder = new MediaRecorder(this.stream);

      this.mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          this.audioChunks.push(event.data);
        }
      };

      this.mediaRecorder.start(100);
      this.isRecording = true;

      // Timer and waveform simulator
      this.timerInterval = setInterval(() => {
        this.recordingSeconds++;
        const randomHeight = Math.floor(Math.random() * 80) + 20;
        this.waveformBars.push(randomHeight);
        if (this.waveformBars.length > 30) {
          this.waveformBars.shift();
        }
      }, 1000);
    } catch (err) {
      console.error('Error starting audio recording:', err);
      throw err;
    }
  }

  public stopRecording(): Promise<{ blob: Blob; duration: number }> {
    return new Promise((resolve, reject) => {
      if (!this.mediaRecorder || this.mediaRecorder.state === 'inactive') {
        return reject('Recorder not active');
      }

      this.mediaRecorder.onstop = () => {
        const audioBlob = new Blob(this.audioChunks, { type: 'audio/webm' });
        const duration = this.recordingSeconds;
        this.cleanup();
        resolve({ blob: audioBlob, duration });
      };

      this.mediaRecorder.stop();
    });
  }

  public cancelRecording(): void {
    if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
      this.mediaRecorder.stop();
    }
    this.cleanup();
  }

  private cleanup(): void {
    this.isRecording = false;
    this.recordingSeconds = 0;
    this.waveformBars = [];
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
    if (this.stream) {
      this.stream.getTracks().forEach((track) => track.stop());
      this.stream = null;
    }
    this.mediaRecorder = null;
  }
}
