export interface User {
  id: string;
  empCode?: string;
  phone: string;
  name: string;
  department?: string;
  designation?: string;
  email?: string;
  role?: string;
  canSendFiles?: boolean;
  canMakeCalls?: boolean;
  canCreateGroups?: boolean;
  canPostStatus?: boolean;
  canDeleteMessages?: boolean;
  isActive?: boolean;
  avatar?: string;
  about?: string;
  isOnline?: boolean;
  lastSeen?: string | Date;
}

export interface EmployeeForm {
  empCode: string;
  name: string;
  password?: string;
  isActive?: boolean;
  department: string;
  designation: string;
  email: string;
  phone?: string;
  role: string;
  canSendFiles: boolean;
  canMakeCalls: boolean;
  canCreateGroups: boolean;
  canPostStatus: boolean;
  canDeleteMessages: boolean;
}

export interface Chat {
  id: string;
  isGroup: boolean;
  name?: string;
  avatar?: string;
  groupDescription?: string;
  isPinned: boolean;
  isMuted: boolean;
  role: string;
  otherUser?: User;
  members: User[];
  lastMessage?: Message;
  unreadCount: number;
  updatedAt: string | Date;
}

export interface Reaction {
  id: string;
  userId: string;
  userName: string;
  emoji: string;
}

export interface ReplyToMessage {
  id: string;
  text?: string;
  type: string;
  fileName?: string;
  senderId: string;
  senderName?: string;
}

export interface Message {
  id: string;
  chatId: string;
  senderId: string;
  senderName?: string;
  senderAvatar?: string;
  text?: string;
  type: 'TEXT' | 'IMAGE' | 'VIDEO' | 'AUDIO' | 'VOICE' | 'DOCUMENT' | 'SYSTEM';
  fileUrl?: string;
  fileName?: string;
  fileSize?: number;
  fileMime?: string;
  duration?: number;
  status: 'SENT' | 'DELIVERED' | 'READ';
  isDeleted?: boolean;
  isStarred?: boolean;
  replyToId?: string;
  replyTo?: ReplyToMessage;
  reactions?: Reaction[];
  createdAt: string | Date;
}

export interface StatusStory {
  id: string;
  type: 'TEXT' | 'IMAGE' | 'VIDEO';
  content: string;
  caption?: string;
  backgroundColor?: string;
  font?: string;
  createdAt: string | Date;
  expiresAt: string | Date;
}

export interface UserStatusGroup {
  userId: string;
  user: {
    id: string;
    name: string;
    avatar?: string;
    phone: string;
  };
  stories: StatusStory[];
  latestUpdate: string | Date;
}

export interface CallLog {
  id: string;
  callerId: string;
  callerName: string;
  callerAvatar?: string;
  receiverId: string;
  receiverName: string;
  receiverAvatar?: string;
  type: 'AUDIO' | 'VIDEO';
  status: 'MISSED' | 'REJECTED' | 'COMPLETED';
  duration: number;
  isOutgoing: boolean;
  createdAt: string | Date;
}

export interface ActiveCall {
  callerId: string;
  callerName: string;
  callerAvatar?: string;
  targetUserId?: string;
  targetUserName?: string;
  targetUserAvatar?: string;
  callType: 'AUDIO' | 'VIDEO';
  isIncoming: boolean;
  status: 'ringing' | 'connected' | 'ended';
  startedAt?: Date;
}
