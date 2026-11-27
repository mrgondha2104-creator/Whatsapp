import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { User, Chat, Message, UserStatusGroup, CallLog, EmployeeForm } from '../models/whatsapp.models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  public get baseUrl(): string {
    return environment.apiBaseUrl;
  }
  private tokenKey = 'whatsapp_auth_token';
  private currentUserKey = 'whatsapp_current_user';

  private currentUserSubject = new BehaviorSubject<User | null>(this.getStoredUser());
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) { }

  public get currentUser(): User | null {
    return this.currentUserSubject.value;
  }

  private getStoredUser(): User | null {
    const raw = localStorage.getItem(this.currentUserKey);
    return raw ? JSON.parse(raw) : null;
  }

  public get token(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  public setSession(user: User, token: string) {
    localStorage.setItem(this.currentUserKey, JSON.stringify(user));
    localStorage.setItem(this.tokenKey, token);
    this.currentUserSubject.next(user);
  }

  public logout() {
    localStorage.removeItem(this.currentUserKey);
    localStorage.removeItem(this.tokenKey);
    this.currentUserSubject.next(null);
  }

  private getHeaders(): HttpHeaders {
    let headers = new HttpHeaders();
    if (this.token) {
      headers = headers.set('Authorization', `Bearer ${this.token}`);
    }
    return headers;
  }

  // Auth by UserID / EmpCode and Password
  login(userId: string, password?: string, name?: string): Observable<any> {
    const payload = {
      userId: userId.trim(),
      empCode: userId.trim().toUpperCase(),
      password: password ? password.trim() : 'Password@123',
      name
    };

    return this.http.post<any>(`${this.baseUrl}/api/auth/login`, payload).pipe(
      tap(res => {
        if (res.success && res.token && res.user) {
          this.setSession(res.user, res.token);
        }
      })
    );
  }

  getMe(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/api/auth/me`, { headers: this.getHeaders() });
  }

  updateProfile(data: { name?: string; about?: string; avatar?: string }): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/api/auth/profile`, data, { headers: this.getHeaders() }).pipe(
      tap(res => {
        if (res.success && res.user) {
          this.setSession(res.user, this.token || '');
        }
      })
    );
  }

  uploadAvatar(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<any>(`${this.baseUrl}/api/auth/avatar`, formData, { headers: this.getHeaders() });
  }

  // Employee Directory & Permissions 
  getEmployees(): Observable<{ success: boolean; employees: User[] }> {
    return this.http.get<{ success: boolean; employees: User[] }>(`${this.baseUrl}/api/employees`, { headers: this.getHeaders() });
  }

  addEmployee(form: EmployeeForm): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/employees`, form, { headers: this.getHeaders() });
  }

  updateEmployeePermissions(id: string, permissions: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/api/employees/${id}/permissions`, permissions, { headers: this.getHeaders() });
  }

  deleteEmployee(id: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/api/employees/${id}`, { headers: this.getHeaders() });
  }

  // Users (Company in-house contacts)
  getAllUsers(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/api/users`, { headers: this.getHeaders() });
  }

  searchUsers(query: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/api/users/search?query=${encodeURIComponent(query)}`, { headers: this.getHeaders() });
  }

  // Chats
  getChats(): Observable<{ success: boolean; chats: Chat[] }> {
    const userId = this.currentUser?.id || '';
    return this.http.get<{ success: boolean; chats: Chat[] }>(`${this.baseUrl}/api/chats?userId=${userId}`, { headers: this.getHeaders() });
  }

  createDirectChat(recipientId: string): Observable<any> {
    const userId = this.currentUser?.id || '';
    return this.http.post<any>(`${this.baseUrl}/api/chats/direct?userId=${userId}`, { recipientId }, { headers: this.getHeaders() });
  }

  createGroupChat(name: string, memberIds: string[], description?: string, avatar?: string): Observable<any> {
    const userId = this.currentUser?.id || '';
    return this.http.post<any>(`${this.baseUrl}/api/chats/group?userId=${userId}`, { name, memberIds, description, avatar }, { headers: this.getHeaders() });
  }

  togglePinChat(chatId: string): Observable<any> {
    const userId = this.currentUser?.id || '';
    return this.http.put<any>(`${this.baseUrl}/api/chats/${chatId}/pin?userId=${userId}`, {}, { headers: this.getHeaders() });
  }

  toggleMuteChat(chatId: string): Observable<any> {
    const userId = this.currentUser?.id || '';
    return this.http.put<any>(`${this.baseUrl}/api/chats/${chatId}/mute?userId=${userId}`, {}, { headers: this.getHeaders() });
  }

  // Messages
  getMessages(chatId: string): Observable<{ success: boolean; messages: Message[] }> {
    const userId = this.currentUser?.id || '';
    return this.http.get<{ success: boolean; messages: Message[] }>(`${this.baseUrl}/api/messages/${chatId}?userId=${userId}`, { headers: this.getHeaders() });
  }

  sendMessage(chatId: string, text?: string, replyToId?: string): Observable<any> {
    const userId = this.currentUser?.id || '';
    return this.http.post<any>(`${this.baseUrl}/api/messages?userId=${userId}`, {
      chatId,
      text,
      type: 'TEXT',
      replyToId
    }, { headers: this.getHeaders() });
  }

  forwardMessages(messageIds: string[], targetChatIds: string[]): Observable<any> {
    const userId = this.currentUser?.id || '';
    return this.http.post<any>(`${this.baseUrl}/api/messages/forward?userId=${userId}`, {
      messageIds,
      targetChatIds
    }, { headers: this.getHeaders() });
  }

  uploadFile(chatId: string, file: File, customType?: string, duration?: number, replyToId?: string, text?: string): Observable<any> {
    const userId = this.currentUser?.id || '';
    const formData = new FormData();
    formData.append('file', file);
    formData.append('chatId', chatId);
    if (text) formData.append('text', text);
    if (customType) formData.append('customType', customType);
    if (duration) formData.append('duration', duration.toString());
    if (replyToId) formData.append('replyToId', replyToId);

    return this.http.post<any>(`${this.baseUrl}/api/messages/upload?userId=${userId}`, formData, { headers: this.getHeaders() });
  }

  reactToMessage(messageId: string, emoji: string): Observable<any> {
    const userId = this.currentUser?.id || '';
    return this.http.post<any>(`${this.baseUrl}/api/messages/${messageId}/react?userId=${userId}`, { emoji }, { headers: this.getHeaders() });
  }

  toggleStarMessage(messageId: string): Observable<any> {
    const userId = this.currentUser?.id || '';
    return this.http.post<any>(`${this.baseUrl}/api/messages/${messageId}/star?userId=${userId}`, {}, { headers: this.getHeaders() });
  }

  getStarredMessages(): Observable<any> {
    const userId = this.currentUser?.id || '';
    return this.http.get<any>(`${this.baseUrl}/api/messages/starred?userId=${userId}`, { headers: this.getHeaders() });
  }

  deleteMessage(messageId: string): Observable<any> {
    const userId = this.currentUser?.id || '';
    return this.http.delete<any>(`${this.baseUrl}/api/messages/${messageId}?userId=${userId}`, { headers: this.getHeaders() });
  }

  markAsRead(chatId: string): Observable<any> {
    const userId = this.currentUser?.id || '';
    return this.http.put<any>(`${this.baseUrl}/api/messages/${chatId}/read?userId=${userId}`, {}, { headers: this.getHeaders() });
  }

  // Status Stories
  getStatuses(): Observable<{ success: boolean; statuses: UserStatusGroup[] }> {
    return this.http.get<{ success: boolean; statuses: UserStatusGroup[] }>(`${this.baseUrl}/api/status`);
  }

  createStatus(type: string, content?: string, caption?: string, backgroundColor?: string, font?: string, file?: File): Observable<any> {
    const userId = this.currentUser?.id || '';
    const formData = new FormData();
    formData.append('type', type);
    if (content) formData.append('content', content);
    if (caption) formData.append('caption', caption);
    if (backgroundColor) formData.append('backgroundColor', backgroundColor);
    if (font) formData.append('font', font);
    if (file) formData.append('file', file);

    return this.http.post<any>(`${this.baseUrl}/api/status?userId=${userId}`, formData, { headers: this.getHeaders() });
  }

  // Calls
  getCallLogs(): Observable<{ success: boolean; callLogs: CallLog[] }> {
    const userId = this.currentUser?.id || '';
    return this.http.get<{ success: boolean; callLogs: CallLog[] }>(`${this.baseUrl}/api/calls?userId=${userId}`, { headers: this.getHeaders() });
  }

  createCallLog(log: any): Observable<any> {
    const userId = this.currentUser?.id || '';
    return this.http.post<any>(`${this.baseUrl}/api/calls?userId=${userId}`, log, { headers: this.getHeaders() });
  }

  getFullMediaUrl(path?: string): string {
    if (!path) return '';
    if (path.startsWith('http://') || path.startsWith('https://')) return path;
    return `${this.baseUrl}${path.startsWith('/') ? path : '/' + path}`;
  }
}
