import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-placeholder',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="placeholder-container">
      <div class="placeholder-card">
        <div class="icon-wrapper">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-12 h-12">
            <path stroke-linecap="round" stroke-linejoin="round" d="M11.42 15.17L17.25 21A2.652 2.652 0 0021 17.25l-5.877-5.877M11.42 15.17l2.496-3.03c.317-.384.74-.626 1.208-.766M11.42 15.17l-4.655 5.653a2.548 2.548 0 11-3.586-3.586l6.837-5.63m5.108-.233c.55-.164 1.163-.188 1.743-.14a4.5 4.5 0 004.486-6.336l-3.276 3.277a3.004 3.004 0 01-2.25-2.25l3.276-3.276a4.5 4.5 0 00-6.336 4.486c.091 1.076-.071 2.264-.904 2.95l-.102.085m-1.745 1.437L5.909 7.5H4.5L2.25 3.75l1.5-1.5L7.5 4.5v1.409l4.26 4.26m-1.745 1.437l1.745-1.437m6.615 8.206L15.75 15.75M4.867 19.125h.008v.008h-.008v-.008z" />
          </svg>
        </div>
        <h2>{{ title }}</h2>
        <p>Cette fonctionnalité est actuellement en cours de construction.</p>
        <div class="badge">Bientôt disponible</div>
      </div>
    </div>
  `,
  styles: [`
    .placeholder-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 60vh;
    }
    .placeholder-card {
      background: var(--surface);
      padding: 3rem;
      border-radius: var(--radius-xl);
      box-shadow: var(--shadow-md);
      text-align: center;
      max-width: 450px;
      border: 1px solid var(--border-light);
      animation: fadeIn 0.5s ease-out;
    }
    .icon-wrapper {
      width: 80px;
      height: 80px;
      background: var(--primary-alpha);
      color: var(--primary);
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 1.5rem;
    }
    .icon-wrapper svg {
      width: 40px;
      height: 40px;
    }
    h2 {
      font-family: var(--font-heading);
      color: var(--text-main);
      font-size: 1.5rem;
      margin-bottom: 0.5rem;
    }
    p {
      color: var(--text-secondary);
      margin-bottom: 2rem;
    }
    .badge {
      display: inline-block;
      padding: 0.5rem 1rem;
      background: var(--surface-hover);
      color: var(--text-muted);
      border-radius: 20px;
      font-size: 0.8rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
  `]
})
export class PlaceholderComponent {
  title = 'En construction';

  constructor(private route: ActivatedRoute) {
    this.route.data.subscribe(data => {
      if (data['title']) {
        this.title = data['title'];
      }
    });
  }
}
