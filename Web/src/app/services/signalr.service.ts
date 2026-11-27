import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, BehaviorSubject } from 'rxjs';
import { Message } from '../models/whatsapp.models';
import { SoundService } from './sound.service';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private hubConnection: signalR.HubConnection | null = null;
  public isConnected$ = new BehaviorSubject<boolean>(false);

  // Event Streams
  public messageReceived$ = new Subject<Message>();
  public userTyping$ = new Subject<{ chatId: string; userId: string; userName: string; isTyping: boolean }>();
  public messagesRead$ = new Subject<{ chatId: string; userId: string }>();
  public reactionReceived$ = new Subject<{ chatId: string; messageId: string; reaction: any; action: string }>();
  public userPresence$ = new Subject<{ userId: string; isOnline: boolean; lastSeen: Date }>();
  public messageDeleted$ = new Subject<{ messageId: string; chatId: string }>();

  // Call Signaling Streams
  public incomingCall$ = new Subject<{ callerId: string; callerName: string; callerAvatar?: string; callType: 'AUDIO' | 'VIDEO'; chatId?: string }>();
  public callAccepted$ = new Subject<{ targetUserId: string }>();
  public callRejected$ = new Subject<{ reason: string }>();
  public signalReceived$ = new Subject<{ signal: any; senderConnectionId: string }>();
  public callEnded$ = new Subject<void>();

  constructor(private soundService: SoundService) {}

  public async startConnection(userId: string): Promise<void> {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('JoinUser', userId);
      return;
    }

    const hubUrl = environment.signalrHubUrl;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.registerHandlers();

    try {
      await this.hubConnection.start();
      this.isConnected$.next(true);
      console.log('SignalR connected to ' + hubUrl);

      // Join user's individual channel
      await this.hubConnection.invoke('JoinUser', userId);
    } catch (err) {
      console.error('SignalR connection error: ', err);
      this.isConnected$.next(false);
    }

    this.hubConnection.onreconnected(async () => {
      this.isConnected$.next(true);
      await this.hubConnection?.invoke('JoinUser', userId);
    });

    this.hubConnection.onclose(() => {
      this.isConnected$.next(false);
    });
  }

  private registerHandlers(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('ReceiveMessage', (message: Message) => {
      this.messageReceived$.next(message);
      this.soundService.playIncomingMessageSound();
    });

    this.hubConnection.on('UserTyping', (data) => {
      this.userTyping$.next(data);
    });

    this.hubConnection.on('MessagesMarkedRead', (data) => {
      this.messagesRead$.next(data);
    });

    this.hubConnection.on('ReactionReceived', (data) => {
      this.reactionReceived$.next(data);
    });

    this.hubConnection.on('UserPresenceChanged', (data) => {
      this.userPresence$.next(data);
    });

    this.hubConnection.on('MessageDeleted', (data) => {
      this.messageDeleted$.next(data);
    });

    // Call signaling
    this.hubConnection.on('IncomingCall', (call) => {
      this.soundService.startRingtone();
      this.incomingCall$.next(call);
    });

    this.hubConnection.on('CallAccepted', (data) => {
      this.soundService.stopRingtone();
      this.callAccepted$.next(data);
    });

    this.hubConnection.on('CallRejected', (data) => {
      this.soundService.stopRingtone();
      this.callRejected$.next(data);
    });

    this.hubConnection.on('ReceiveSignal', (data) => {
      this.signalReceived$.next(data);
    });

    this.hubConnection.on('CallEnded', () => {
      this.soundService.stopRingtone();
      this.callEnded$.next();
    });
  }

  // Client invocations
  public async joinChat(chatId: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('JoinChat', chatId);
    }
  }

  public async leaveChat(chatId: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('LeaveChat', chatId);
    }
  }

  public async sendTyping(chatId: string, userId: string, userName: string, isTyping: boolean): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('SendTyping', chatId, userId, userName, isTyping);
    }
  }

  public async markAsRead(chatId: string, userId: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('MarkAsRead', chatId, userId);
    }
  }

  // Calling invocations
  public async callUser(targetUserId: string, callerId: string, callerName: string, callerAvatar: string, callType: 'AUDIO' | 'VIDEO', chatId?: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('CallUser', targetUserId, callerId, callerName, callerAvatar, callType, chatId);
    }
  }

  public async acceptCall(callerUserId: string, targetUserId: string): Promise<void> {
    this.soundService.stopRingtone();
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('AcceptCall', callerUserId, targetUserId);
    }
  }

  public async rejectCall(callerUserId: string, reason: string = 'Declined'): Promise<void> {
    this.soundService.stopRingtone();
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('RejectCall', callerUserId, reason);
    }
  }

  public async sendSignal(targetUserId: string, signal: any): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('SendSignal', targetUserId, signal);
    }
  }

  public async endCall(targetUserId: string): Promise<void> {
    this.soundService.stopRingtone();
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('EndCall', targetUserId);
    }
  }

  public stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
      this.isConnected$.next(false);
    }
  }
}
