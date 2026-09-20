import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, inject, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

interface User {
  id: string;
  phoneNumber: string;
  displayName: string;
  profilePicUrl?: string;
  isOnline: boolean;
}

interface Message {
  id: string;
  content: string;
  senderId: string;
  sentAt: string;
}

interface Conversation {
  id: string;
  otherParticipant: User;
  lastMessage?: Message;
  unreadCount: number;
  createdAt: string;
}

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = window.location.port === '4200' ? 'http://localhost:5289' : '';

  phoneNumber = '';
  otpCode = '';
  contactPhone = '';
  messageDraft = '';
  statusMessage = '';
  errorMessage = '';
  token: string | null = null;
  currentUser: User | null = null;
  conversations: Conversation[] = [];
  selectedConversation: Conversation | null = null;
  messages: Message[] = [];
  isSendingOtp = false;
  isVerifying = false;
  isLoadingConversations = false;
  isCreatingConversation = false;
  isRealtimeConnected = false;
  private hubConnection: HubConnection | null = null;

  constructor() {
    const savedSession = sessionStorage.getItem('chatapp-session');
    if (savedSession) {
      const session = JSON.parse(savedSession) as { token: string; user: User };
      this.token = session.token;
      this.currentUser = session.user;
      this.connectRealtime();
      this.loadConversations();
    }
  }

  get isAuthenticated(): boolean {
    return this.token !== null && this.currentUser !== null;
  }

  sendOtp(): void {
    this.clearMessages();
    if (!this.phoneNumber.trim()) {
      this.errorMessage = 'Enter a phone number first.';
      return;
    }

    this.isSendingOtp = true;
    this.http.post(`${this.apiBaseUrl}/api/auth/send-otp`, { phoneNumber: this.phoneNumber }).subscribe({
      next: () => {
        this.statusMessage = 'OTP sent. Demo mode accepts any code.';
        this.isSendingOtp = false;
      },
      error: (error) => {
        this.errorMessage = error.error?.error ?? 'Could not reach the API.';
        this.isSendingOtp = false;
      }
    });
  }

  verifyOtp(): void {
    this.clearMessages();
    this.isVerifying = true;
    this.http.post<{ token: string; user: User }>(`${this.apiBaseUrl}/api/auth/verify-otp`, {
      phoneNumber: this.phoneNumber,
      code: this.otpCode
    }).subscribe({
      next: (response) => {
        this.token = response.token;
        this.currentUser = response.user;
        sessionStorage.setItem('chatapp-session', JSON.stringify(response));
        this.statusMessage = `Welcome back, ${response.user.displayName}.`;
        this.isVerifying = false;
        this.connectRealtime();
        this.loadConversations();
      },
      error: (error) => {
        this.errorMessage = error.error?.error ?? 'That OTP could not be verified.';
        this.isVerifying = false;
      }
    });
  }

  loadConversations(): void {
    if (!this.token) return;
    this.isLoadingConversations = true;
    this.http.get<Conversation[]>(`${this.apiBaseUrl}/api/conversations`, { headers: this.authHeaders() }).subscribe({
      next: (conversations) => {
        this.conversations = conversations;
        this.isLoadingConversations = false;
        if (!this.selectedConversation && conversations.length > 0) {
          this.selectConversation(conversations[0]);
        }
      },
      error: () => {
        this.errorMessage = 'Conversations could not be loaded.';
        this.isLoadingConversations = false;
      }
    });
  }

  createConversation(): void {
    if (!this.token || !this.contactPhone.trim()) return;
    this.clearMessages();
    this.isCreatingConversation = true;
    this.http.post<Conversation>(`${this.apiBaseUrl}/api/conversations`, { phoneNumber: this.contactPhone }, { headers: this.authHeaders() }).subscribe({
      next: (conversation) => {
        this.contactPhone = '';
        this.isCreatingConversation = false;
        this.conversations = [conversation, ...this.conversations.filter(item => item.id !== conversation.id)];
        this.selectConversation(conversation);
      },
      error: (error) => {
        this.errorMessage = error.error?.error ?? 'That contact could not be found.';
        this.isCreatingConversation = false;
      }
    });
  }

  selectConversation(conversation: Conversation): void {
    this.selectedConversation = conversation;
    this.messages = conversation.lastMessage ? [conversation.lastMessage] : [];
    void this.hubConnection?.invoke('JoinConversation', conversation.id);
  }

  async sendMessage(): Promise<void> {
    if (!this.messageDraft.trim()) return;
    if (!this.hubConnection || !this.selectedConversation) {
      this.errorMessage = 'Real-time chat is not connected yet.';
      return;
    }

    try {
      await this.hubConnection.invoke('SendMessage', this.selectedConversation.id, this.messageDraft.trim());
      this.messageDraft = '';
      this.clearMessages();
    } catch {
      this.errorMessage = 'The message could not be sent.';
    }
  }

  logout(): void {
    sessionStorage.removeItem('chatapp-session');
    this.token = null;
    this.currentUser = null;
    this.conversations = [];
    this.selectedConversation = null;
    this.messages = [];
    this.hubConnection?.stop();
    this.hubConnection = null;
    this.isRealtimeConnected = false;
    this.clearMessages();
  }

  ngOnDestroy(): void {
    void this.hubConnection?.stop();
  }

  private authHeaders(): HttpHeaders {
    return new HttpHeaders({
      Authorization: `Bearer ${this.token}`,
      'X-User-Id': this.currentUser?.id ?? ''
    });
  }

  private connectRealtime(): void {
    if (!this.currentUser) return;

    void this.hubConnection?.stop();
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${this.apiBaseUrl}/hubs/chat?userId=${encodeURIComponent(this.currentUser.id)}`)
      .configureLogging(LogLevel.Warning)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveMessage', (message: Message & { conversationId: string }) => {
      if (this.selectedConversation?.id !== message.conversationId) return;
      this.messages = [...this.messages, message];
    });

    this.hubConnection.on('ConversationCreated', (conversation: Conversation) => {
      this.conversations = [
        conversation,
        ...this.conversations.filter(item => item.id !== conversation.id)
      ];
    });

    this.hubConnection.onreconnecting(() => this.isRealtimeConnected = false);
    this.hubConnection.onreconnected(() => this.isRealtimeConnected = true);
    this.hubConnection.onclose(() => this.isRealtimeConnected = false);
    void this.hubConnection.start()
      .then(() => {
        this.isRealtimeConnected = true;
        if (this.selectedConversation) {
          return this.hubConnection?.invoke('JoinConversation', this.selectedConversation.id);
        }
        return undefined;
      })
      .catch(() => this.errorMessage = 'Real-time chat is unavailable.');
  }

  private clearMessages(): void {
    this.statusMessage = '';
    this.errorMessage = '';
  }
}
