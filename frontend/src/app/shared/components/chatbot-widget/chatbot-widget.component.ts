import { Component, signal, ViewChild, ElementRef, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/services/auth.service';

interface ChatMessage {
  role: 'user' | 'model';
  text: string;
  source?: 'ai' | 'local';
}

@Component({
  selector: 'app-chatbot-widget',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <!-- Floating Button -->
    <button class="chatbot-fab shadow-lg" 
            (click)="toggleWidget()" 
            [style.display]="isOpen() ? 'none' : 'flex'">
      <i class="fa-solid fa-robot fa-lg"></i>
    </button>

    <!-- Floating Window -->
    <div class="chatbot-window shadow-lg animate-fade-in" 
         [style.display]="isOpen() ? 'flex' : 'none'"
         [style.transform]="transformStyle">
      
      <!-- Header -->
      <div class="chat-header" (mousedown)="onDragStart($event)">
        <div class="header-left">
          <i class="fa-solid fa-robot header-icon"></i>
          <div class="header-title-container">
            <span class="header-title">Assistant IA</span>
            <span class="header-subtitle">En ligne</span>
          </div>
        </div>
        <button class="close-btn" (click)="toggleWidget()">
          <i class="fa-solid fa-xmark fa-xl"></i>
        </button>
      </div>

      <!-- Messages Area -->
      <div class="chat-messages" #scrollMe>
        
        <!-- Welcome Message -->
        <div class="message-row">
          <div class="model-bubble">
            Bonjour ! Je suis l'assistant virtuel de la plateforme. Comment puis-je vous aider ?
          </div>
        </div>

        <!-- Chat History -->
        <div *ngFor="let msg of messages()" class="message-row" [ngClass]="{'user': msg.role === 'user'}">
          <div class="bubble-wrapper" *ngIf="msg.role === 'model'">
            <div class="model-bubble" [innerHTML]="formatMessage(msg.text)"></div>
            <span class="source-badge" [ngClass]="msg.source === 'ai' ? 'badge-ai' : 'badge-faq'">
              {{ msg.source === 'ai' ? '✨ IA' : '📋 FAQ' }}
            </span>
          </div>
          <div class="user-bubble" *ngIf="msg.role === 'user'" [innerHTML]="formatMessage(msg.text)"></div>
        </div>

        <!-- Suggestions (Only show when empty) -->
        <div class="suggestions-container" *ngIf="messages().length === 0">
          <span class="suggestions-title">Questions fréquentes :</span>
          <div class="suggestions-list">
            <button *ngFor="let sug of suggestions" class="suggestion-chip" (click)="useSuggestion(sug)">
              {{ sug }}
            </button>
          </div>
        </div>

        <!-- Loading Indicator -->
        <div *ngIf="isLoading()" class="message-row">
          <div class="model-bubble typing-bubble">
            <div class="typing-dot"></div>
            <div class="typing-dot"></div>
            <div class="typing-dot"></div>
          </div>
        </div>
      </div>

      <!-- Input Area -->
      <div class="chat-input-area">
        <form (ngSubmit)="sendMessage()" class="chat-form">
          <input type="text" class="chat-input-field" 
                 placeholder="Écrivez votre question ici..." 
                 [(ngModel)]="questionInput" 
                 name="question" 
                 [disabled]="isLoading()"
                 autocomplete="off" required>
          <button type="submit" class="send-btn" 
                  [disabled]="isLoading() || !questionInput.trim()">
            <i class="fa-solid fa-paper-plane"></i>
          </button>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .chatbot-fab {
      position: fixed;
      bottom: 2rem;
      right: 2rem;
      width: 65px;
      height: 65px;
      z-index: 999999 !important;
      background-color: var(--primary, #0d6efd);
      color: white;
      border: none;
      border-radius: 50%;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: transform 0.2s;
    }
    .chatbot-fab:hover { transform: scale(1.05); }

    .chatbot-window {
      position: fixed;
      bottom: 2rem;
      right: 2rem;
      width: 420px;
      height: 620px;
      max-height: calc(100vh - 3rem);
      z-index: 999999 !important;
      background-color: #ffffff !important;
      border-radius: 1rem;
      overflow: hidden;
      box-shadow: 0 15px 50px rgba(0,0,0,0.2) !important;
      display: flex;
      flex-direction: column;
      border: 1px solid rgba(0,0,0,0.1);
      will-change: transform;
    }

    .chat-header {
      background-color: var(--primary, #0d6efd) !important;
      padding: 1.25rem 1.5rem;
      display: flex;
      align-items: center;
      justify-content: space-between;
      cursor: grab;
      user-select: none;
    }
    .chat-header:active { cursor: grabbing !important; }

    .header-left {
      display: flex;
      align-items: center;
      pointer-events: none;
    }
    .header-icon {
      color: white !important;
      font-size: 2rem;
      margin-right: 1rem;
    }
    .header-title-container {
      display: flex;
      flex-direction: column;
      justify-content: center;
    }
    .header-title {
      color: white !important;
      font-weight: bold;
      font-size: 1.15rem;
      line-height: 1.2;
    }
    .header-subtitle {
      color: rgba(255, 255, 255, 0.8) !important;
      font-size: 0.85rem;
      margin-top: 2px;
    }

    .close-btn {
      background: transparent;
      border: none;
      color: white;
      cursor: pointer;
      padding: 0.5rem;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: opacity 0.2s;
    }
    .close-btn:hover { opacity: 0.7; }

    .chat-messages {
      flex-grow: 1;
      overflow-y: auto;
      background-color: #f8f9fa !important;
      padding: 1.5rem !important;
      display: flex;
      flex-direction: column;
    }

    .message-row {
      display: flex;
      width: 100%;
      margin-bottom: 1.25rem;
    }
    .message-row.user {
      justify-content: flex-end;
    }

    .bubble-wrapper {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      max-width: 85%;
    }
    .source-badge {
      font-size: 0.7rem;
      font-weight: 600;
      padding: 0.15rem 0.5rem;
      border-radius: 50rem;
      margin-top: 0.35rem;
      letter-spacing: 0.03em;
    }
    .badge-ai {
      background: linear-gradient(135deg, #6366f1, #8b5cf6);
      color: white;
    }
    .badge-faq {
      background: #e2e8f0;
      color: #64748b;
    }
    .model-bubble {
      background-color: white !important;
      border: 1px solid #e9ecef !important;
      color: #333 !important;
      padding: 1rem 1.25rem !important;
      border-radius: 1rem !important;
      border-top-left-radius: 0 !important;
      box-shadow: 0 2px 5px rgba(0,0,0,0.02) !important;
      width: 100%;
      font-size: 0.95rem;
      line-height: 1.6;
      white-space: pre-wrap;
    }
    /* Dynamic link styling injected via innerHTML */
    ::ng-deep .chat-link {
      color: var(--primary, #0d6efd);
      font-weight: 600;
      text-decoration: none;
      cursor: pointer;
    }
    ::ng-deep .chat-link:hover {
      text-decoration: underline;
    }

    .user-bubble {
      background-color: var(--primary, #0d6efd) !important;
      color: white !important;
      padding: 1rem 1.25rem !important;
      border-radius: 1rem !important;
      border-top-right-radius: 0 !important;
      box-shadow: 0 2px 5px rgba(0,0,0,0.05) !important;
      max-width: 85%;
      font-size: 0.95rem;
      line-height: 1.6;
      white-space: pre-wrap;
    }

    .suggestions-container {
      margin-top: auto;
      margin-bottom: 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      animation: fadein 0.5s;
    }
    .suggestions-title {
      font-size: 0.8rem;
      color: #6c757d;
      font-weight: 600;
      padding-left: 0.5rem;
    }
    .suggestions-list {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
    }
    .suggestion-chip {
      background-color: white;
      border: 1px solid var(--primary, #0d6efd);
      color: var(--primary, #0d6efd);
      border-radius: 50rem;
      padding: 0.4rem 0.8rem;
      font-size: 0.85rem;
      cursor: pointer;
      transition: all 0.2s;
    }
    .suggestion-chip:hover {
      background-color: var(--primary, #0d6efd);
      color: white;
    }

    .chat-input-area {
      padding: 1rem;
      background-color: white;
      border-top: 1px solid #eaeaea;
    }
    
    .chat-form {
      display: flex;
      gap: 0.75rem;
      margin: 0;
      align-items: center;
    }

    .chat-input-field {
      flex-grow: 1;
      padding: 0.75rem 1.25rem;
      border: 1px solid #dee2e6;
      border-radius: 50rem;
      background-color: #f8f9fa;
      outline: none;
      font-size: 0.95rem;
      transition: border-color 0.2s, box-shadow 0.2s;
    }
    .chat-input-field:focus {
      border-color: var(--primary, #0d6efd);
      box-shadow: 0 0 0 3px rgba(13, 110, 253, 0.1);
      background-color: white;
    }

    .send-btn {
      width: 44px;
      height: 44px;
      border-radius: 50%;
      background-color: var(--primary, #0d6efd);
      color: white;
      border: none;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      transition: transform 0.1s, background-color 0.2s;
    }
    .send-btn:hover:not(:disabled) { transform: scale(1.05); }
    .send-btn:disabled { opacity: 0.5; cursor: not-allowed; }

    .chat-messages::-webkit-scrollbar { width: 6px; }
    .chat-messages::-webkit-scrollbar-track { background: transparent; }
    .chat-messages::-webkit-scrollbar-thumb { background: #cbd5e1; border-radius: 3px; }
    
    .typing-bubble { display: flex; align-items: center; gap: 4px; padding: 1rem !important; }
    .typing-dot {
      width: 6px; height: 6px; background: #888; border-radius: 50%;
      animation: typing 1.4s infinite ease-in-out both;
    }
    .typing-dot:nth-child(1) { animation-delay: -0.32s; }
    .typing-dot:nth-child(2) { animation-delay: -0.16s; }
    @keyframes typing {
      0%, 80%, 100% { transform: scale(0); }
      40% { transform: scale(1); }
    }
    @keyframes fadein { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
  `]
})
export class ChatbotWidgetComponent {
  @ViewChild('scrollMe') private myScrollContainer!: ElementRef;
  private http = inject(HttpClient);
  private router = inject(Router);
  private authService = inject(AuthService);
  
  isOpen = signal(false);
  messages = signal<ChatMessage[]>([]);
  isLoading = signal(false);
  questionInput = '';
  
  suggestions = [
    'Comment postuler ?',
    'Qui signe en premier ?',
    'Où voir mes rapports ?'
  ];

  // Variables for dragging
  private isDragging = false;
  private translateX = 0;
  private translateY = 0;
  transformStyle = 'translate3d(0px, 0px, 0)';

  @HostListener('click', ['$event'])
  onInternalClick(event: Event) {
    const target = event.target as HTMLElement;
    if (target.tagName === 'A' && target.classList.contains('chat-link')) {
      event.preventDefault();
      const href = target.getAttribute('href');
      if (href) {
        this.router.navigateByUrl(href);
        this.isOpen.set(false); // Close widget on navigation
      }
    }
  }

  onDragStart(event: MouseEvent) {
    if ((event.target as HTMLElement).closest('button') || (event.target as HTMLElement).tagName.toLowerCase() === 'input') return;
    this.isDragging = true;
    event.preventDefault(); // Prevents text selection while dragging
  }

  @HostListener('document:mousemove', ['$event'])
  onDragMove(event: MouseEvent) {
    if (!this.isDragging) return;
    event.preventDefault();
    this.translateX += event.movementX;
    this.translateY += event.movementY;
    this.transformStyle = `translate3d(${this.translateX}px, ${this.translateY}px, 0)`;
  }

  @HostListener('document:mouseup')
  onDragEnd() {
    this.isDragging = false;
  }

  toggleWidget() {
    this.isOpen.set(!this.isOpen());
    if (this.isOpen()) {
      setTimeout(() => this.scrollToBottom(), 50);
    }
  }

  scrollToBottom(): void {
    try {
      if (this.myScrollContainer) {
        this.myScrollContainer.nativeElement.scrollTop = this.myScrollContainer.nativeElement.scrollHeight;
      }
    } catch(err) { }
  }

  useSuggestion(suggestion: string) {
    this.questionInput = suggestion;
    this.sendMessage();
  }

  formatMessage(text: string): string {
    if (!text) return '';
    let formatted = text;
    // Replace markdown links [Text](/route) with <a> tags
    formatted = formatted.replace(/\[(.*?)\]\((.*?)\)/g, '<a href="$2" class="chat-link">$1</a>');
    // Replace markdown bold **text** with <strong>text</strong>
    formatted = formatted.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
    return formatted;
  }

  sendMessage() {
    if (!this.questionInput.trim()) return;

    const question = this.questionInput.trim();
    this.questionInput = '';
    const historyToSend = [...this.messages()];
    const currentRole = this.authService.currentUser()?.role || '';

    this.messages.update(msgs => [...msgs, { role: 'user', text: question }]);
    this.isLoading.set(true);
    setTimeout(() => this.scrollToBottom(), 50);

    const payload = { 
      question, 
      history: historyToSend,
      role: currentRole 
    };

    this.http.post<{ reponse: string; source?: 'ai' | 'local' }>(`${environment.apiUrl}/ai/chatbot`, payload).subscribe({
      next: (res) => {
        this.messages.update(msgs => [...msgs, { role: 'model', text: res.reponse, source: res.source ?? 'local' }]);
        this.isLoading.set(false);
        setTimeout(() => this.scrollToBottom(), 50);
      },
      error: (err) => {
        console.error(err);
        const friendlyMsg = err.error?.error || "Le service est momentanément indisponible. Veuillez réessayer dans quelques instants.";
        this.messages.update(msgs => [...msgs, { role: 'model', text: '⚠️ ' + friendlyMsg, source: 'local' }]);
        this.isLoading.set(false);
        setTimeout(() => this.scrollToBottom(), 50);
      }
    });
  }
}
