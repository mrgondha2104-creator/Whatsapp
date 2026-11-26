import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { SignalrService } from './signalr.service';

@Injectable({
  providedIn: 'root'
})
export class WebrtcService {
  private peerConnection: RTCPeerConnection | null = null;
  public localStream$ = new BehaviorSubject<MediaStream | null>(null);
  public remoteStream$ = new BehaviorSubject<MediaStream | null>(null);

  public isMuted = false;
  public isCameraOff = false;
  public isScreenSharing = false;

  private rtcConfig: RTCConfiguration = {
    iceServers: [
      { urls: 'stun:stun.l.google.com:19302' },
      { urls: 'stun:stun1.l.google.com:19302' }
    ]
  };

  constructor(private signalrService: SignalrService) {
    this.signalrService.signalReceived$.subscribe(async (data) => {
      await this.handleSignal(data.signal);
    });
  }

  public async startLocalStream(video: boolean = true): Promise<MediaStream> {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        audio: true,
        video: video ? { width: 1280, height: 720 } : false
      });
      this.localStream$.next(stream);
      return stream;
    } catch (e) {
      console.warn('Could not acquire camera/mic:', e);
      // Fallback to audio-only if video failed
      const audioStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false });
      this.localStream$.next(audioStream);
      return audioStream;
    }
  }

  public async initPeerConnection(targetUserId: string, isInitiator: boolean, isVideo: boolean = true) {
    this.peerConnection = new RTCPeerConnection(this.rtcConfig);

    const localStream = this.localStream$.value || await this.startLocalStream(isVideo);
    localStream.getTracks().forEach(track => {
      this.peerConnection?.addTrack(track, localStream);
    });

    const remoteStream = new MediaStream();
    this.remoteStream$.next(remoteStream);

    this.peerConnection.ontrack = (event) => {
      event.streams[0].getTracks().forEach(track => {
        remoteStream.addTrack(track);
      });
    };

    this.peerConnection.onicecandidate = (event) => {
      if (event.candidate) {
        this.signalrService.sendSignal(targetUserId, {
          type: 'candidate',
          candidate: event.candidate
        });
      }
    };

    if (isInitiator) {
      const offer = await this.peerConnection.createOffer();
      await this.peerConnection.setLocalDescription(offer);
      this.signalrService.sendSignal(targetUserId, {
        type: 'offer',
        sdp: offer
      });
    }
  }

  private async handleSignal(signal: any) {
    if (!this.peerConnection) return;

    if (signal.type === 'offer') {
      await this.peerConnection.setRemoteDescription(new RTCSessionDescription(signal.sdp));
      const answer = await this.peerConnection.createAnswer();
      await this.peerConnection.setLocalDescription(answer);
      // The answer will be sent back via target user handled in caller
    } else if (signal.type === 'answer') {
      await this.peerConnection.setRemoteDescription(new RTCSessionDescription(signal.sdp));
    } else if (signal.type === 'candidate') {
      await this.peerConnection.addIceCandidate(new RTCIceCandidate(signal.candidate));
    }
  }

  public toggleMute(): boolean {
    const stream = this.localStream$.value;
    if (stream) {
      const audioTrack = stream.getAudioTracks()[0];
      if (audioTrack) {
        audioTrack.enabled = !audioTrack.enabled;
        this.isMuted = !audioTrack.enabled;
      }
    }
    return this.isMuted;
  }

  public toggleCamera(): boolean {
    const stream = this.localStream$.value;
    if (stream) {
      const videoTrack = stream.getVideoTracks()[0];
      if (videoTrack) {
        videoTrack.enabled = !videoTrack.enabled;
        this.isCameraOff = !videoTrack.enabled;
      }
    }
    return this.isCameraOff;
  }

  public endConnection() {
    if (this.peerConnection) {
      this.peerConnection.close();
      this.peerConnection = null;
    }
    const stream = this.localStream$.value;
    if (stream) {
      stream.getTracks().forEach(t => t.stop());
      this.localStream$.next(null);
    }
    this.remoteStream$.next(null);
    this.isMuted = false;
    this.isCameraOff = false;
    this.isScreenSharing = false;
  }
}
