import { Component, signal, ViewChild, ElementRef, AfterViewChecked, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';

interface ChatMessage {
  role: 'user' | 'model';
  text: string;
  source?: 'ai' | 'local';
}

@Component({
  selector: 'app-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="chat-container mx-auto animate-fade-in shadow-sm rounded-4 bg-white border d-flex flex-column" style="max-width: 800px; height: 80vh; margin-top: 2rem;">
      <!-- Header -->
      <div class="chat-header p-4 border-bottom bg-light rounded-top-4 d-flex align-items-center">
        <div class="avatar bg-primary text-white rounded-circle d-flex align-items-center justify-content-center me-3" style="width: 50px; height: 50px;">
          <i class="fa-solid fa-robot fa-lg"></i>
        </div>
        <div>
          <h4 class="mb-0 fw-bold">Assistant IA</h4>
          <p class="text-muted small mb-0">Posez vos questions sur la plateforme (stages, conventions...)</p>
        </div>
      </div>

      <!-- Messages Area -->
      <div class="chat-messages flex-grow-1 p-4 overflow-y-auto" #scrollMe>
        
        <!-- Welcome Message -->
        <div class="message-wrapper d-flex mb-4 model-message">
          <div class="avatar bg-light text-primary rounded-circle d-flex align-items-center justify-content-center me-3 shadow-sm border" style="width: 40px; height: 40px; min-width: 40px;">
            <i class="fa-solid fa-robot"></i>
          </div>
          <div class="message-bubble bg-light p-3 rounded-4 shadow-sm" style="max-width: 80%; border-top-left-radius: 0 !important;">
            Bonjour ! Je suis l'assistant virtuel de la plateforme. Comment puis-je vous aider aujourd'hui ?
          </div>
        </div>

        <!-- Suggestions (Only show when empty) -->
        <div class="suggestions-container mb-4" *ngIf="messages().length === 0">
          <p class="text-muted small mb-2 ms-5">Questions fréquentes :</p>
          <div class="d-flex flex-wrap gap-2 ms-5">
            <button *ngFor="let sug of suggestions" class="btn btn-outline-primary rounded-pill btn-sm" (click)="useSuggestion(sug)">
              {{ sug }}
            </button>
          </div>
        </div>

        <!-- Chat History -->
        <div *ngFor="let msg of messages()" class="message-wrapper d-flex mb-4" [ngClass]="msg.role === 'user' ? 'user-message justify-content-end' : 'model-message'">
          
          <!-- Model Avatar -->
          <div *ngIf="msg.role === 'model'" class="avatar bg-light text-primary rounded-circle d-flex align-items-center justify-content-center me-3 shadow-sm border" style="width: 40px; height: 40px; min-width: 40px;">
            <i class="fa-solid fa-robot"></i>
          </div>

          <!-- Bubble -->
          <div class="message-bubble p-3 rounded-4 shadow-sm" 
               [ngClass]="msg.role === 'user' ? 'bg-primary text-white' : 'bg-light text-dark'"
               [style.border-top-right-radius]="msg.role === 'user' ? '0 !important' : '1rem'"
               [style.border-top-left-radius]="msg.role === 'model' ? '0 !important' : '1rem'"
               style="max-width: 80%; white-space: pre-wrap;"
               [innerHTML]="formatMessage(msg.text)">
          </div>
          <!-- Source badge for model messages -->
          <span *ngIf="msg.role === 'model'" class="source-badge ms-2" [ngClass]="msg.source === 'ai' ? 'badge-ai' : 'badge-faq'">
            {{ msg.source === 'ai' ? '✨ IA' : '📋 FAQ' }}
          </span>

        </div>

        <!-- Loading Indicator -->
        <div *ngIf="isLoading()" class="message-wrapper d-flex mb-4 model-message">
          <div class="avatar bg-light text-primary rounded-circle d-flex align-items-center justify-content-center me-3 shadow-sm border" style="width: 40px; height: 40px; min-width: 40px;">
            <i class="fa-solid fa-robot"></i>
          </div>
          <div class="message-bubble bg-light p-3 rounded-4 shadow-sm d-flex align-items-center gap-2" style="max-width: 80%; border-top-left-radius: 0 !important;">
            <div class="typing-dot"></div>
            <div class="typing-dot"></div>
            <div class="typing-dot"></div>
          </div>
        </div>
      </div>

      <!-- Input Area -->
      <div class="chat-input p-3 border-top bg-light rounded-bottom-4">
        <form (ngSubmit)="sendMessage()" class="d-flex gap-2">
          <input type="text" class="form-control rounded-pill px-4 shadow-sm" 
                 placeholder="Écrivez votre question ici..." 
                 [(ngModel)]="questionInput" 
                 name="question" 
                 [disabled]="isLoading()"
                 autocomplete="off" required>
          <button type="submit" class="btn btn-primary rounded-circle shadow-sm d-flex align-items-center justify-content-center" 
                  style="width: 45px; height: 45px; min-width: 45px;" 
                  [disabled]="isLoading() || !questionInput.trim()">
            <i class="fa-solid fa-paper-plane"></i>
          </button>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .chat-messages::-webkit-scrollbar { width: 6px; }
    .chat-messages::-webkit-scrollbar-track { background: #f1f1f1; }
    .chat-messages::-webkit-scrollbar-thumb { background: #ccc; border-radius: 3px; }
    .chat-messages::-webkit-scrollbar-thumb:hover { background: #999; }
    
    .typing-dot {
      width: 8px; height: 8px; background: var(--text-muted); border-radius: 50%;
      animation: typing 1.4s infinite ease-in-out both;
    }
    .typing-dot:nth-child(1) { animation-delay: -0.32s; }
    .typing-dot:nth-child(2) { animation-delay: -0.16s; }
    @keyframes typing {
      0%, 80%, 100% { transform: scale(0); }
      40% { transform: scale(1); }
    }
    
    .source-badge {
      font-size: 0.68rem;
      font-weight: 600;
      padding: 0.1rem 0.45rem;
      border-radius: 50rem;
      letter-spacing: 0.03em;
      align-self: flex-start;
      margin-top: 4px;
    }
    .badge-ai {
      background: linear-gradient(135deg, #6366f1, #8b5cf6);
      color: white;
    }
    .badge-faq {
      background: #e2e8f0;
      color: #64748b;
    }
    ::ng-deep .chat-link {
      color: var(--primary, #0d6efd);
      font-weight: 600;
      text-decoration: none;
      cursor: pointer;
    }
    ::ng-deep .chat-link:hover {
      text-decoration: underline;
    }
    ::ng-deep .bg-primary .chat-link {
      color: #fff;
      text-decoration: underline;
    }
  `]
})
export class ChatbotComponent implements AfterViewChecked {
  @ViewChild('scrollMe') private myScrollContainer!: ElementRef;
  private http = inject(HttpClient);
  private router = inject(Router);
  private authService = inject(AuthService);
  
  messages = signal<ChatMessage[]>([]);
  isLoading = signal(false);
  questionInput = '';
  
  suggestions = [
    'Comment postuler ?',
    'Qui signe en premier ?',
    'Où voir mes rapports ?'
  ];

  @HostListener('click', ['$event'])
  onInternalClick(event: Event) {
    const target = event.target as HTMLElement;
    if (target.tagName === 'A' && target.classList.contains('chat-link')) {
      event.preventDefault();
      const href = target.getAttribute('href');
      if (href) {
        this.router.navigateByUrl(href);
      }
    }
  }

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  scrollToBottom(): void {
    try {
      this.myScrollContainer.nativeElement.scrollTop = this.myScrollContainer.nativeElement.scrollHeight;
    } catch(err) { }
  }

  useSuggestion(suggestion: string) {
    this.questionInput = suggestion;
    this.sendMessage();
  }

  formatMessage(text: string): string {
    if (!text) return '';
    let formatted = text;
    formatted = formatted.replace(/\[(.*?)\]\((.*?)\)/g, '<a href="$2" class="chat-link">$1</a>');
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

    const payload = {
      question: question,
      history: historyToSend,
      role: currentRole
    };

    this.http.post<{ reponse: string; source?: 'ai' | 'local' }>(`${environment.apiUrl}/ai/chatbot`, payload).subscribe({
      next: (res) => {
        this.messages.update(msgs => [...msgs, { role: 'model', text: res.reponse, source: res.source ?? 'local' }]);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        const friendlyMsg = err.error?.error || "Le service est momentanément indisponible. Veuillez réessayer.";
        this.messages.update(msgs => [...msgs, { role: 'model', text: '⚠️ ' + friendlyMsg, source: 'local' }]);
        this.isLoading.set(false);
      }
    });
  }
}
