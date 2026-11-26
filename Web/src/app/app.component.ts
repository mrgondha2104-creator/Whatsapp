import { Component, OnInit, OnDestroy, ViewChild, ElementRef, HostListener } from '@angular/core';
import { Subscription } from 'rxjs';
import { ApiService } from './services/api.service';
import { SignalrService } from './services/signalr.service';
import { WebrtcService } from './services/webrtc.service';
import { SoundService } from './services/sound.service';
import { AudioRecorderService } from './services/audio-recorder.service';
import { User, Chat, Message, UserStatusGroup, CallLog, ActiveCall, EmployeeForm } from './models/whatsapp.models';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy {
  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;
  @ViewChild('fileInput') private fileInput!: ElementRef;
  @ViewChild('chatInput') private chatInputRef?: ElementRef<HTMLInputElement>;
  @ViewChild('localVideo') private localVideoRef!: ElementRef<HTMLVideoElement>;
  @ViewChild('remoteVideo') private remoteVideoRef!: ElementRef<HTMLVideoElement>;

  // Authentication & Employee Session
  currentUser: User | null = null;
  loginInput: string = '1';
  loginPassword: string = 'Password@123';
  showLoginPassword: boolean = false;
  loginName: string = 'Yash';
  isLoggedIn = false;
  isLightMode = false;
  loginError: string = '';

  // In-House Employee Directory & Permissions
  employees: User[] = [];
  showEmpModal: boolean = false;
  empModalTab: 'directory' | 'add' = 'directory';
  newEmpForm: EmployeeForm = {
    empCode: '',
    name: '',
    password: 'Password@123',
    isActive: true,
    department: 'Engineering',
    designation: 'Software Developer',
    email: '',
    role: 'Employee',
    canSendFiles: true,
    canMakeCalls: true,
    canCreateGroups: true,
    canPostStatus: true,
    canDeleteMessages: false
  };

  // Safe Profile Editing
  editProfileName: string = '';
  editProfileAbout: string = '';

  // Conversations & Messages
  chats: Chat[] = [];
  filteredChats: Chat[] = [];
  activeChat: Chat | null = null;
  messages: Message[] = [];
  chatSearchQuery: string = '';
  activeFilter: 'all' | 'unread' | 'groups' = 'all';

  // Input & Reply
  messageText: string = '';
  replyingTo: Message | null = null;
  typingTimeout: any = null;
  typingUsers: { [chatId: string]: string } = {};

  // Attachments & Menus
  showAttachMenu: boolean = false;
  showEmojiPicker: boolean = false;
  showProfileDrawer: boolean = false;
  showNewChatModal: boolean = false;
  showCreateGroupModal: boolean = false;
  showStatusViewer: boolean = false;
  showCreateStatusModal: boolean = false;
  showStarredDrawer: boolean = false;
  showChatInfoDrawer: boolean = false;
  showMenuDropdown: boolean = false;
  activeMessageDropdownId: string | null = null;

  // Multi-select & Forward Messages
  isSelectionMode: boolean = false;
  selectedMessageIds: string[] = [];
  showForwardModal: boolean = false;
  forwardSearchQuery: string = '';
  selectedForwardChatIds: string[] = [];
  isForwarding: boolean = false;

  // Group creation
  allUsers: User[] = [];
  selectedGroupMemberIds: string[] = [];
  newGroupName: string = '';
  newGroupDescription: string = '';

  // Status Stories
  statusGroups: UserStatusGroup[] = [];
  activeStatusGroup: UserStatusGroup | null = null;
  currentStoryIndex: number = 0;
  storyProgress: number = 0;
  storyTimer: any = null;
  newStatusText: string = '';
  newStatusColor: string = '#075e54';
  newStatusFont: string = 'sans-serif';
  availableColors: string[] = ['#075e54', '#128c7e', '#8b5cf6', '#ec4899', '#f97316', '#3b82f6'];

  // Media Lightbox
  lightboxMedia: { url: string; type: 'IMAGE' | 'VIDEO'; caption?: string } | null = null;

  // WebRTC Calling
  activeCall: ActiveCall | null = null;
  callDurationSeconds: number = 0;
  callDurationTimer: any = null;
  callLogs: CallLog[] = [];

  // Audio Playback Speeds for Voice Notes
  audioPlayRates: { [messageId: string]: number } = {};

  // Starred messages
  starredMessages: Message[] = [];

  // Emojis
  emojiList: string[] = ['😀', '😂', '😍', '🔥', '👍', '❤️', '🎉', '🙏', '😎', '🙌', '💯', '👏', '✨', '🥳', '🤔', '😢', '🚀', '⭐', '🍕', '☕'];
  quickReactions: string[] = ['👍', '❤️', '😂', '😮', '😢', '🙏'];

  private subscriptions: Subscription[] = [];

  constructor(
    public api: ApiService,
    public signalr: SignalrService,
    public webrtc: WebrtcService,
    public sound: SoundService,
    public recorder: AudioRecorderService
  ) {}

  ngOnInit() {
    const savedTheme = localStorage.getItem('whatsapp_theme');
    if (savedTheme === 'light') {
      this.isLightMode = true;
      document.body.classList.add('light-theme');
    }

    this.loadEmployees();

    const storedUser = this.api.currentUser;
    if (storedUser) {
      this.currentUser = storedUser;
      this.isLoggedIn = true;
      this.initUserData();
    }
  }

  ngOnDestroy() {
    this.subscriptions.forEach((s) => s.unsubscribe());
    this.signalr.stopConnection();
    this.stopStoryTimer();
    this.stopCallTimer();
  }

  // Employee Management
  loadEmployees() {
    this.api.getEmployees().subscribe({
      next: (res) => {
        if (res.success) {
          this.employees = res.employees;
        }
      }
    });
  }

  openEmployeeModal(tab: 'directory' | 'add' = 'directory') {
    this.empModalTab = tab;
    this.showEmpModal = true;
    this.loadEmployees();
  }

  addNewEmployee() {
    if (!this.newEmpForm.name.trim()) {
      alert('Please enter employee name.');
      return;
    }

    if (!this.newEmpForm.empCode.trim()) {
      this.newEmpForm.empCode = 'EMP-' + (this.employees.length + 101);
    }

    this.api.addEmployee(this.newEmpForm).subscribe({
      next: (res) => {
        if (res.success) {
          alert(`Employee ${this.newEmpForm.name} (${this.newEmpForm.empCode}) registered successfully!`);
          this.newEmpForm = {
            empCode: '',
            name: '',
            department: 'Engineering',
            designation: 'Software Developer',
            email: '',
            role: 'Employee',
            canSendFiles: true,
            canMakeCalls: true,
            canCreateGroups: true,
            canPostStatus: true,
            canDeleteMessages: false
          };
          this.empModalTab = 'directory';
          this.loadEmployees();
          this.loadUsers();
        }
      },
      error: (err) => {
        alert('Error adding employee: ' + (err.error?.message || err.message));
      }
    });
  }

  togglePermission(emp: User, permissionKey: string) {
    const patch: any = {};
    if (permissionKey === 'canSendFiles') patch.canSendFiles = !emp.canSendFiles;
    if (permissionKey === 'canMakeCalls') patch.canMakeCalls = !emp.canMakeCalls;
    if (permissionKey === 'canCreateGroups') patch.canCreateGroups = !emp.canCreateGroups;
    if (permissionKey === 'canPostStatus') patch.canPostStatus = !emp.canPostStatus;
    if (permissionKey === 'canDeleteMessages') patch.canDeleteMessages = !emp.canDeleteMessages;
    if (permissionKey === 'isActive') patch.isActive = !emp.isActive;

    this.api.updateEmployeePermissions(emp.id, patch).subscribe({
      next: (res) => {
        if (res.success && res.employee) {
          Object.assign(emp, res.employee);
          if (this.currentUser?.id === emp.id) {
            Object.assign(this.currentUser, res.employee);
            this.api.setSession(this.currentUser, this.api.token || '');
          }
        }
      }
    });
  }

  // Login handler
  loginWithEmpCode(empCode: string, password: string = 'Password@123') {
    this.loginInput = empCode;
    this.loginPassword = password;
    this.login();
  }

  login() {
    this.loginError = '';
    const userId = this.loginInput.trim();
    const pass = this.loginPassword.trim();

    if (!userId) {
      this.loginError = 'Please enter your Employee ID / User ID.';
      return;
    }

    if (!pass) {
      this.loginError = 'Please enter your account password.';
      return;
    }

    this.api.login(userId, pass, this.loginName).subscribe({
      next: (res) => {
        if (res.success && res.user) {
          this.currentUser = res.user;
          this.isLoggedIn = true;
          this.initUserData();
        }
      },
      error: (err) => {
        this.loginError = err.error?.message || 'Login failed. Please check User ID and Password.';
      }
    });
  }

  logout() {
    this.api.logout();
    this.signalr.stopConnection();
    this.currentUser = null;
    this.isLoggedIn = false;
    this.activeChat = null;
    this.messages = [];
  }

  toggleTheme() {
    this.isLightMode = !this.isLightMode;
    if (this.isLightMode) {
      document.body.classList.add('light-theme');
      localStorage.setItem('whatsapp_theme', 'light');
    } else {
      document.body.classList.remove('light-theme');
      localStorage.setItem('whatsapp_theme', 'dark');
    }
  }

  openProfileDrawer() {
    if (this.currentUser) {
      this.editProfileName = this.currentUser.name || '';
      this.editProfileAbout = this.currentUser.about || '';
    }
    this.showProfileDrawer = true;
  }

  saveProfileChanges() {
    if (!this.currentUser) return;
    this.api.updateProfile({ name: this.editProfileName, about: this.editProfileAbout }).subscribe({
      next: (res) => {
        if (res.success && res.user) {
          this.currentUser = res.user;
          this.showProfileDrawer = false;
        }
      }
    });
  }

  // Initialize data once logged in
  initUserData() {
    if (!this.currentUser) return;

    this.signalr.startConnection(this.currentUser.id);

    this.subscriptions.push(
      this.signalr.messageReceived$.subscribe((msg) => this.handleIncomingMessage(msg)),
      this.signalr.userTyping$.subscribe((data) => this.handleUserTyping(data)),
      this.signalr.messagesRead$.subscribe((data) => this.handleMessagesRead(data)),
      this.signalr.reactionReceived$.subscribe((data) => this.handleReactionReceived(data)),
      this.signalr.userPresence$.subscribe((data) => this.handleUserPresence(data)),
      this.signalr.messageDeleted$.subscribe((data) => this.handleMessageDeleted(data)),
      this.signalr.incomingCall$.subscribe((call) => this.handleIncomingCall(call)),
      this.signalr.callAccepted$.subscribe((data) => this.handleCallAccepted(data)),
      this.signalr.callRejected$.subscribe((data) => this.handleCallRejected(data)),
      this.signalr.callEnded$.subscribe(() => this.handleCallEnded())
    );

    this.loadChats();
    this.loadStatuses();
    this.loadUsers();
  }

  get totalUnreadChatsCount(): number {
    return this.chats ? this.chats.filter((c) => (c.unreadCount || 0) > 0).length : 0;
  }

  loadChats() {
    this.api.getChats().subscribe({
      next: (res) => {
        if (res.success) {
          this.chats = res.chats;
          // Join SignalR rooms for all chats
          for (const c of this.chats) {
            this.signalr.joinChat(c.id);
          }
          this.applyChatFilter();
          // If we already have an active chat selected, keep its reference updated
          if (this.activeChat) {
            const found = this.chats.find(c => c.id === this.activeChat?.id);
            if (found) {
              this.activeChat = found;
            }
          }
        }
      }
    });
  }

  loadStatuses() {
    this.api.getStatuses().subscribe({
      next: (res) => {
        if (res.success) {
          this.statusGroups = res.statuses;
        }
      }
    });
  }

  loadUsers() {
    this.api.getAllUsers().subscribe({
      next: (res) => {
        if (res.success) {
          this.allUsers = res.users;
        }
      }
    });
  }

  selectChat(chat: Chat) {
    if (this.activeChat?.id === chat.id) return;

    if (this.activeChat) {
      this.signalr.leaveChat(this.activeChat.id);
    }

    this.activeChat = chat;
    this.replyingTo = null;
    this.isSelectionMode = false;
    this.selectedMessageIds = [];
    this.signalr.joinChat(chat.id);

    this.loadMessages(chat.id);
    this.markChatAsRead(chat.id);
  }

  loadMessages(chatId: string) {
    this.api.getMessages(chatId).subscribe({
      next: (res) => {
        if (res.success) {
          this.messages = res.messages;
          this.scrollToBottom();
        }
      }
    });
  }

  markChatAsRead(chatId: string) {
    if (!this.currentUser) return;
    this.api.markAsRead(chatId).subscribe(() => {
      const chat = this.chats.find((c) => c.id === chatId);
      if (chat) chat.unreadCount = 0;
      this.signalr.markAsRead(chatId, this.currentUser!.id);
    });
  }

  // Messaging
  sendMessage() {
    if (!this.messageText.trim() || !this.activeChat) return;

    const text = this.messageText.trim();
    const replyId = this.replyingTo?.id;

    this.messageText = '';
    this.replyingTo = null;
    this.showEmojiPicker = false;
    this.stopTypingNotification();

    this.api.sendMessage(this.activeChat.id, text, replyId).subscribe({
      next: (res) => {
        if (res.success && res.message) {
          this.sound.playSentMessageSound();
          this.updateChatLastMessage(res.message);
          this.scrollToBottom();
        }
      }
    });
  }

  onTyping() {
    if (!this.activeChat || !this.currentUser) return;

    this.signalr.sendTyping(this.activeChat.id, this.currentUser.id, this.currentUser.name, true);

    if (this.typingTimeout) clearTimeout(this.typingTimeout);
    this.typingTimeout = setTimeout(() => {
      this.stopTypingNotification();
    }, 2000);
  }

  stopTypingNotification() {
    if (!this.activeChat || !this.currentUser) return;
    this.signalr.sendTyping(this.activeChat.id, this.currentUser.id, this.currentUser.name, false);
  }

  // File Upload with Permission Check
  triggerFileUpload(type?: string) {
    if (this.currentUser && this.currentUser.canSendFiles === false) {
      alert('Permission Denied: Your employee account does not have permission to send files.');
      this.showAttachMenu = false;
      return;
    }

    this.showAttachMenu = false;
    if (this.fileInput) {
      if (type === 'IMAGE_VIDEO') {
        this.fileInput.nativeElement.accept = 'image/*,video/*';
      } else if (type === 'AUDIO') {
        this.fileInput.nativeElement.accept = 'audio/*';
      } else {
        this.fileInput.nativeElement.accept = '*/*';
      }
      this.fileInput.nativeElement.click();
    }
  }

  onFileSelected(event: any) {
    const file = event.target.files?.[0];
    if (!file || !this.activeChat) return;

    if (this.currentUser && this.currentUser.canSendFiles === false) {
      alert('Permission Denied: Your employee account does not have permission to send files.');
      event.target.value = '';
      return;
    }

    const replyId = this.replyingTo?.id;
    this.replyingTo = null;

    this.api.uploadFile(this.activeChat.id, file, undefined, undefined, replyId).subscribe({
      next: (res) => {
        if (res.success && res.message) {
          this.sound.playSentMessageSound();
          this.updateChatLastMessage(res.message);
          this.scrollToBottom();
        }
      },
      error: (err) => {
        alert('File upload failed: ' + (err.error?.message || err.message));
      }
    });

    event.target.value = '';
  }

  // Voice Note Recording
  async startVoiceRecording() {
    if (this.currentUser && this.currentUser.canSendFiles === false) {
      alert('Permission Denied: Your employee account cannot send audio recordings.');
      return;
    }

    try {
      await this.recorder.startRecording();
    } catch (e) {
      alert('Could not access microphone: ' + e);
    }
  }

  async stopAndSendVoiceRecording() {
    if (!this.activeChat) return;
    try {
      const { blob, duration } = await this.recorder.stopRecording();
      const file = new File([blob], `voice_${Date.now()}.webm`, { type: 'audio/webm' });

      this.api.uploadFile(this.activeChat.id, file, 'VOICE', duration).subscribe({
        next: (res) => {
          if (res.success && res.message) {
            this.sound.playSentMessageSound();
            this.updateChatLastMessage(res.message);
            this.scrollToBottom();
          }
        }
      });
    } catch (e) {
      console.error(e);
    }
  }

  cancelVoiceRecording() {
    this.recorder.cancelRecording();
  }

  toggleAudioSpeed(msgId: string, audioElement: HTMLAudioElement) {
    let currentSpeed = this.audioPlayRates[msgId] || 1;
    let nextSpeed = currentSpeed === 1 ? 1.5 : currentSpeed === 1.5 ? 2 : 1;
    this.audioPlayRates[msgId] = nextSpeed;
    audioElement.playbackRate = nextSpeed;
  }

  reactToMessage(message: Message, emoji: string) {
    this.api.reactToMessage(message.id, emoji).subscribe();
  }

  toggleStar(message: Message) {
    this.api.toggleStarMessage(message.id).subscribe({
      next: (res) => {
        if (res.success) {
          message.isStarred = res.isStarred;
        }
      }
    });
  }

  // Global Click Listener for Dismissing Popups & Dropdowns
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    if (!target) return;

    // 1. Close 3-dot menu dropdown if clicked outside
    if (!target.closest('.dropdown-wrapper')) {
      this.showMenuDropdown = false;
    }

    // 2. Close message bubble corner dropdown if clicked outside
    if (!target.closest('.bubble-dropdown-wrapper')) {
      this.activeMessageDropdownId = null;
    }

    // 3. Close emoji picker if clicked outside
    if (!target.closest('.emoji-wrapper')) {
      this.showEmojiPicker = false;
    }

    // 4. Close attachment menu if clicked outside
    if (!target.closest('.attach-wrapper')) {
      this.showAttachMenu = false;
    }
  }

  toggleMessageDropdown(msgId: string) {
    if (this.activeMessageDropdownId === msgId) {
      this.activeMessageDropdownId = null;
    } else {
      this.activeMessageDropdownId = msgId;
    }
  }

  copyMessageText(text?: string) {
    if (!text) return;
    navigator.clipboard?.writeText(text);
  }

  deleteMessage(message: Message) {
    if (this.currentUser && this.currentUser.canDeleteMessages === false) {
      alert('Permission Denied: Your employee account does not have permission to delete messages.');
      return;
    }

    if (!confirm('Delete this message for everyone?')) return;
    this.api.deleteMessage(message.id).subscribe({
      next: (res) => {
        if (res.success) {
          message.isDeleted = true;
          message.text = 'This message was deleted';
          message.fileUrl = undefined;
          this.activeMessageDropdownId = null;
        }
      },
      error: (err) => {
        alert(err.error?.message || 'Cannot delete message.');
      }
    });
  }

  replyToMessage(message: Message) {
    this.replyingTo = message;
    this.activeMessageDropdownId = null;
    setTimeout(() => {
      this.chatInputRef?.nativeElement?.focus();
    }, 30);
  }

  cancelReply() {
    this.replyingTo = null;
  }

  // Multi-message Selection & Forwarding
  startSelectionMode(msg?: Message) {
    this.isSelectionMode = true;
    this.activeMessageDropdownId = null;
    this.selectedMessageIds = msg && !msg.isDeleted ? [msg.id] : [];
  }

  cancelSelectionMode() {
    this.isSelectionMode = false;
    this.selectedMessageIds = [];
  }

  toggleMessageSelection(msg: Message) {
    if (msg.isDeleted || msg.type === 'SYSTEM') return;
    const idx = this.selectedMessageIds.indexOf(msg.id);
    if (idx > -1) {
      this.selectedMessageIds.splice(idx, 1);
    } else {
      this.selectedMessageIds.push(msg.id);
    }
  }

  isMessageSelected(msgId: string): boolean {
    return this.selectedMessageIds.includes(msgId);
  }

  canDeleteSelected(): boolean {
    if (!this.currentUser?.canDeleteMessages || this.selectedMessageIds.length === 0) return false;
    return this.selectedMessageIds.every((id) => {
      const m = this.messages.find((msg) => msg.id === id);
      return m && (m.senderId === this.currentUser?.id || this.currentUser?.role === 'Admin');
    });
  }

  deleteSelectedMessages() {
    if (!this.canDeleteSelected()) return;
    if (!confirm(`Delete ${this.selectedMessageIds.length} selected message(s) for everyone?`)) return;

    const idsToDelete = [...this.selectedMessageIds];
    idsToDelete.forEach((id) => {
      const m = this.messages.find((msg) => msg.id === id);
      if (m) {
        this.api.deleteMessage(id).subscribe({
          next: (res) => {
            if (res.success) {
              m.isDeleted = true;
              m.text = 'This message was deleted';
              m.fileUrl = undefined;
            }
          }
        });
      }
    });
    this.cancelSelectionMode();
  }

  openForwardModal(singleMsg?: Message) {
    if (singleMsg) {
      this.selectedMessageIds = [singleMsg.id];
    }
    if (this.selectedMessageIds.length === 0) return;
    this.activeMessageDropdownId = null;
    this.selectedForwardChatIds = [];
    this.forwardSearchQuery = '';
    this.showForwardModal = true;
  }

  closeForwardModal() {
    this.showForwardModal = false;
    this.selectedForwardChatIds = [];
  }

  toggleForwardTarget(chatId: string) {
    const idx = this.selectedForwardChatIds.indexOf(chatId);
    if (idx > -1) {
      this.selectedForwardChatIds.splice(idx, 1);
    } else {
      this.selectedForwardChatIds.push(chatId);
    }
  }

  get filteredForwardChats(): Chat[] {
    const q = this.forwardSearchQuery.trim().toLowerCase();
    if (!q) return this.chats;
    return this.chats.filter((c) => (c.name || '').toLowerCase().includes(q));
  }

  confirmForwardMessages() {
    if (this.selectedMessageIds.length === 0 || this.selectedForwardChatIds.length === 0 || this.isForwarding) return;
    this.isForwarding = true;

    const targetIds = [...this.selectedForwardChatIds];
    this.api.forwardMessages(this.selectedMessageIds, targetIds).subscribe({
      next: (res) => {
        this.isForwarding = false;
        if (res.success && res.messages) {
          for (const msg of res.messages as Message[]) {
            this.updateChatLastMessage(msg);
            if (this.activeChat?.id === msg.chatId && !this.messages.some((m) => m.id === msg.id)) {
              this.messages.push(msg);
            }
          }
          this.sound.playSentMessageSound();
        }
        this.closeForwardModal();
        this.cancelSelectionMode();

        // If forwarded to a single user/chat, switch to that chat directly like WhatsApp
        if (targetIds.length === 1) {
          const targetChat = this.chats.find((c) => c.id === targetIds[0]);
          if (targetChat) {
            this.selectChat(targetChat);
            this.scrollToBottom();
          }
        }
      },
      error: (err) => {
        this.isForwarding = false;
        alert('Error forwarding messages: ' + (err.error?.message || err.message));
      }
    });
  }

  // WebRTC Calling with Permission Check
  startCall(callType: 'AUDIO' | 'VIDEO') {
    if (this.currentUser && this.currentUser.canMakeCalls === false) {
      alert('Permission Denied: Your employee account does not have permission to make calls.');
      return;
    }

    if (!this.activeChat || !this.currentUser) return;

    let targetUserId = '';
    let targetUserName = '';
    let targetUserAvatar = '';

    if (!this.activeChat.isGroup && this.activeChat.otherUser) {
      targetUserId = this.activeChat.otherUser.id;
      targetUserName = this.activeChat.otherUser.name;
      targetUserAvatar = this.activeChat.otherUser.avatar || '';
    } else {
      const other = this.activeChat.members.find((m) => m.id !== this.currentUser!.id);
      if (other) {
        targetUserId = other.id;
        targetUserName = other.name;
        targetUserAvatar = other.avatar || '';
      }
    }

    if (!targetUserId) {
      alert('Cannot start call in this chat.');
      return;
    }

    this.activeCall = {
      callerId: this.currentUser.id,
      callerName: this.currentUser.name,
      callerAvatar: this.currentUser.avatar,
      targetUserId,
      targetUserName,
      targetUserAvatar,
      callType,
      isIncoming: false,
      status: 'ringing'
    };

    this.signalr.callUser(
      targetUserId,
      this.currentUser.id,
      this.currentUser.name,
      this.currentUser.avatar || '',
      callType,
      this.activeChat.id
    );

    this.webrtc.initPeerConnection(targetUserId, true, callType === 'VIDEO').then(() => {
      this.attachLocalStream();
    });
  }

  acceptIncomingCall() {
    if (!this.activeCall || !this.currentUser) return;

    this.signalr.acceptCall(this.activeCall.callerId, this.currentUser.id);
    this.activeCall.status = 'connected';
    this.startCallTimer();

    this.webrtc.initPeerConnection(this.activeCall.callerId, false, this.activeCall.callType === 'VIDEO').then(() => {
      this.attachLocalStream();
      this.attachRemoteStream();
    });
  }

  rejectIncomingCall() {
    if (!this.activeCall) return;
    this.signalr.rejectCall(this.activeCall.callerId, 'Declined');
    this.endCallSession();
  }

  endCall() {
    if (!this.activeCall) return;
    const peerId = this.activeCall.isIncoming ? this.activeCall.callerId : (this.activeCall.targetUserId || '');
    if (peerId) {
      this.signalr.endCall(peerId);
    }
    this.endCallSession();
  }

  private endCallSession() {
    this.stopCallTimer();
    this.webrtc.endConnection();
    this.activeCall = null;
    this.callDurationSeconds = 0;
  }

  private startCallTimer() {
    this.stopCallTimer();
    this.callDurationSeconds = 0;
    this.callDurationTimer = setInterval(() => {
      this.callDurationSeconds++;
    }, 1000);
  }

  private stopCallTimer() {
    if (this.callDurationTimer) {
      clearInterval(this.callDurationTimer);
      this.callDurationTimer = null;
    }
  }

  formatDuration(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  }

  attachLocalStream() {
    setTimeout(() => {
      if (this.localVideoRef && this.webrtc.localStream$.value) {
        this.localVideoRef.nativeElement.srcObject = this.webrtc.localStream$.value;
      }
    }, 200);
  }

  attachRemoteStream() {
    this.webrtc.remoteStream$.subscribe((stream) => {
      if (stream && this.remoteVideoRef) {
        this.remoteVideoRef.nativeElement.srcObject = stream;
      }
    });
  }

  // SignalR Event Handlers
  private handleIncomingMessage(msg: Message) {
    if (this.activeChat && msg.chatId === this.activeChat.id) {
      if (!this.messages.some((m) => m.id === msg.id)) {
        this.messages.push(msg);
        this.scrollToBottom();
      }
      if (msg.senderId !== this.currentUser?.id) {
        this.markChatAsRead(msg.chatId);
      }
    }
    this.updateChatLastMessage(msg);
  }

  private handleUserTyping(data: { chatId: string; userId: string; userName: string; isTyping: boolean }) {
    if (data.isTyping) {
      this.typingUsers[data.chatId] = `${data.userName} is typing...`;
    } else {
      delete this.typingUsers[data.chatId];
    }
  }

  private handleMessagesRead(data: { chatId: string; userId: string }) {
    if (this.activeChat && this.activeChat.id === data.chatId) {
      this.messages.forEach((m) => {
        if (m.senderId === this.currentUser?.id) {
          m.status = 'READ';
        }
      });
    }
  }

  private handleReactionReceived(data: { chatId: string; messageId: string; reaction: any; action: string }) {
    const msg = this.messages.find((m) => m.id === data.messageId);
    if (!msg) return;

    if (!msg.reactions) msg.reactions = [];

    if (data.action === 'removed') {
      msg.reactions = msg.reactions.filter((r) => r.userId !== data.reaction.userId);
    } else if (data.action === 'added') {
      msg.reactions.push(data.reaction);
    } else if (data.action === 'updated') {
      const idx = msg.reactions.findIndex((r) => r.userId === data.reaction.userId);
      if (idx !== -1) msg.reactions[idx] = data.reaction;
      else msg.reactions.push(data.reaction);
    }
  }

  private handleUserPresence(data: { userId: string; isOnline: boolean; lastSeen: Date }) {
    this.chats.forEach((chat) => {
      if (!chat.isGroup && chat.otherUser?.id === data.userId) {
        chat.otherUser.isOnline = data.isOnline;
        chat.otherUser.lastSeen = data.lastSeen;
      }
      chat.members.forEach((m) => {
        if (m.id === data.userId) {
          m.isOnline = data.isOnline;
          m.lastSeen = data.lastSeen;
        }
      });
    });
  }

  private handleMessageDeleted(data: { messageId: string; chatId: string }) {
    const msg = this.messages.find((m) => m.id === data.messageId);
    if (msg) {
      msg.isDeleted = true;
      msg.text = 'This message was deleted';
      msg.fileUrl = undefined;
    }
  }

  private handleIncomingCall(call: any) {
    this.activeCall = {
      callerId: call.callerId,
      callerName: call.callerName,
      callerAvatar: call.callerAvatar,
      callType: call.callType,
      isIncoming: true,
      status: 'ringing'
    };
  }

  private handleCallAccepted(data: any) {
    if (this.activeCall) {
      this.activeCall.status = 'connected';
      this.startCallTimer();
      this.attachRemoteStream();
    }
  }

  private handleCallRejected(data: any) {
    alert('Call was rejected: ' + data.reason);
    this.endCallSession();
  }

  private handleCallEnded() {
    this.endCallSession();
  }

  // Status Stories Carousel
  openStatusViewer(group: UserStatusGroup) {
    this.activeStatusGroup = group;
    this.currentStoryIndex = 0;
    this.showStatusViewer = true;
    this.startStoryTimer();
  }

  closeStatusViewer() {
    this.stopStoryTimer();
    this.showStatusViewer = false;
    this.activeStatusGroup = null;
  }

  private startStoryTimer() {
    this.stopStoryTimer();
    this.storyProgress = 0;
    const intervalMs = 50;
    const totalDurationMs = 5000;
    const step = (intervalMs / totalDurationMs) * 100;

    this.storyTimer = setInterval(() => {
      this.storyProgress += step;
      if (this.storyProgress >= 100) {
        this.nextStory();
      }
    }, intervalMs);
  }

  private stopStoryTimer() {
    if (this.storyTimer) {
      clearInterval(this.storyTimer);
      this.storyTimer = null;
    }
  }

  nextStory() {
    if (!this.activeStatusGroup) return;
    if (this.currentStoryIndex < this.activeStatusGroup.stories.length - 1) {
      this.currentStoryIndex++;
      this.startStoryTimer();
    } else {
      this.closeStatusViewer();
    }
  }

  prevStory() {
    if (!this.activeStatusGroup) return;
    if (this.currentStoryIndex > 0) {
      this.currentStoryIndex--;
      this.startStoryTimer();
    }
  }

  postTextStatus() {
    if (this.currentUser && this.currentUser.canPostStatus === false) {
      alert('Permission Denied: Your employee account does not have permission to post status stories.');
      return;
    }

    if (!this.newStatusText.trim()) return;

    this.api.createStatus('TEXT', this.newStatusText.trim(), undefined, this.newStatusColor, this.newStatusFont).subscribe({
      next: (res) => {
        if (res.success) {
          this.newStatusText = '';
          this.showCreateStatusModal = false;
          this.loadStatuses();
        }
      }
    });
  }

  // Employee Contacts & Filtered List
  get filteredEmployees(): User[] {
    // Only authorized active employees from the SQL database (excluding current user)
    let list = this.employees.filter((e) => e.isActive && e.id !== this.currentUser?.id);
    if (this.chatSearchQuery && this.chatSearchQuery.trim()) {
      const q = this.chatSearchQuery.toLowerCase();
      list = list.filter((e) =>
        (e.name || '').toLowerCase().includes(q) ||
        (e.empCode || '').toLowerCase().includes(q) ||
        (e.department || '').toLowerCase().includes(q) ||
        (e.designation || '').toLowerCase().includes(q) ||
        (e.about || '').toLowerCase().includes(q)
      );
    }
    return list;
  }

  // Open or Create 1-on-1 Chat with Employee
  openEmployeeChat(employee: User) {
    this.showNewChatModal = false;

    // 1. Check if chat already exists in current chats list
    const existingChat = this.chats.find(
      (c) => !c.isGroup && c.otherUser?.id === employee.id
    );

    if (existingChat) {
      this.selectChat(existingChat);
      return;
    }

    // 2. Otherwise create/open direct chat via API
    this.api.createDirectChat(employee.id).subscribe({
      next: (res) => {
        if (res.success) {
          this.api.getChats().subscribe({
            next: (chatRes) => {
              if (chatRes.success) {
                this.chats = chatRes.chats;
                this.applyChatFilter();
                const target = this.chats.find(
                  (c) => c.id === res.chatId || (!c.isGroup && c.otherUser?.id === employee.id)
                );
                if (target) {
                  this.selectChat(target);
                }
              }
            }
          });
        }
      },
      error: (err) => {
        alert('Could not open chat with employee: ' + (err.error?.message || err.message));
      }
    });
  }

  // Groups and Contacts
  startDirectChat(user: User) {
    this.openEmployeeChat(user);
  }

  toggleGroupMemberSelection(userId: string) {
    const idx = this.selectedGroupMemberIds.indexOf(userId);
    if (idx > -1) {
      this.selectedGroupMemberIds.splice(idx, 1);
    } else {
      this.selectedGroupMemberIds.push(userId);
    }
  }

  createGroup() {
    if (this.currentUser && this.currentUser.canCreateGroups === false) {
      alert('Permission Denied: Your employee account does not have permission to create groups.');
      return;
    }

    if (!this.newGroupName.trim() || this.selectedGroupMemberIds.length === 0) return;

    this.api.createGroupChat(this.newGroupName.trim(), this.selectedGroupMemberIds, this.newGroupDescription).subscribe({
      next: (res) => {
        this.showCreateGroupModal = false;
        this.newGroupName = '';
        this.newGroupDescription = '';
        this.selectedGroupMemberIds = [];
        this.loadChats();
      },
      error: (err) => {
        alert(err.error?.message || 'Error creating group.');
      }
    });
  }

  // Filter and Helpers
  applyChatFilter() {
    let list = this.chats;

    if (this.activeFilter === 'unread') {
      list = list.filter((c) => c.unreadCount > 0);
    } else if (this.activeFilter === 'groups') {
      list = list.filter((c) => c.isGroup);
    }

    if (this.chatSearchQuery.trim()) {
      const q = this.chatSearchQuery.toLowerCase();
      list = list.filter((c) => (c.name || '').toLowerCase().includes(q));
    }

    this.filteredChats = list;
  }

  onSearchChange() {
    this.applyChatFilter();
  }

  setFilter(filter: 'all' | 'unread' | 'groups') {
    this.activeFilter = filter;
    this.applyChatFilter();
  }

  openLightbox(url: string, type: 'IMAGE' | 'VIDEO', caption?: string) {
    this.lightboxMedia = { url: this.api.getFullMediaUrl(url), type, caption };
  }

  closeLightbox() {
    this.lightboxMedia = null;
  }

  private updateChatLastMessage(msg: Message) {
    const chat = this.chats.find((c) => c.id === msg.chatId);
    if (chat) {
      const isAlreadyLast = chat.lastMessage?.id === msg.id;
      chat.lastMessage = msg;
      chat.updatedAt = msg.createdAt;
      if (!isAlreadyLast && this.activeChat?.id !== msg.chatId && msg.senderId !== this.currentUser?.id) {
        chat.unreadCount = (chat.unreadCount || 0) + 1;
      }
      this.chats.sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime());
      this.applyChatFilter();
    } else {
      this.loadChats();
    }
  }

  private scrollToBottom() {
    setTimeout(() => {
      if (this.messagesContainer) {
        this.messagesContainer.nativeElement.scrollTop = this.messagesContainer.nativeElement.scrollHeight;
      }
    }, 100);
  }

  formatTime(dateStr?: string | Date): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  shouldShowDateBadge(msg: Message, index: number): boolean {
    if (index === 0) return true;
    const prevMsg = this.messages[index - 1];
    if (!prevMsg || !prevMsg.createdAt || !msg.createdAt) return false;

    const prevDate = new Date(prevMsg.createdAt).toDateString();
    const currDate = new Date(msg.createdAt).toDateString();
    return prevDate !== currDate;
  }

  getMessageDateBadge(dateStr?: string | Date): string {
    const d = dateStr ? new Date(dateStr) : new Date();
    return this.formatDateLabel(d);
  }

  formatDateLabel(d: Date): string {
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const target = new Date(d.getFullYear(), d.getMonth(), d.getDate());
    const diffDays = Math.round((today.getTime() - target.getTime()) / (1000 * 3600 * 24));

    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    const dateFormatted = `${day}/${month}/${year}`;

    if (diffDays === 0) {
      return `TODAY • ${dateFormatted}`;
    } else if (diffDays === 1) {
      return `YESTERDAY • ${dateFormatted}`;
    } else {
      const monthNames = [
        'January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'
      ];
      return `${d.getDate()} ${monthNames[d.getMonth()]} ${year} • ${dateFormatted}`;
    }
  }

  formatChatListTime(dateStr?: string | Date): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const target = new Date(d.getFullYear(), d.getMonth(), d.getDate());
    const diffDays = Math.round((today.getTime() - target.getTime()) / (1000 * 3600 * 24));

    if (diffDays === 0) {
      return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } else if (diffDays === 1) {
      return 'Yesterday';
    } else {
      const day = String(d.getDate()).padStart(2, '0');
      const month = String(d.getMonth() + 1).padStart(2, '0');
      return `${day}/${month}/${d.getFullYear()}`;
    }
  }

  formatFullDateTime(dateStr?: string | Date): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    return d.toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' });
  }

  formatLastSeen(dateStr?: string | Date): string {
    if (!dateStr) return 'Offline';
    const d = new Date(dateStr);
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const target = new Date(d.getFullYear(), d.getMonth(), d.getDate());
    const diffDays = Math.round((today.getTime() - target.getTime()) / (1000 * 3600 * 24));
    const time = d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    if (diffDays === 0) {
      return `Last seen today at ${time}`;
    } else if (diffDays === 1) {
      return `Last seen yesterday at ${time}`;
    } else {
      const day = String(d.getDate()).padStart(2, '0');
      const month = String(d.getMonth() + 1).padStart(2, '0');
      return `Last seen ${day}/${month}/${d.getFullYear()} at ${time}`;
    }
  }

  formatFileSize(bytes?: number): string {
    if (!bytes) return '';
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  }
}
