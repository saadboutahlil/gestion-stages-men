import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { SidebarComponent } from '../shared/components/sidebar/sidebar.component';
import { NavbarComponent } from '../shared/components/navbar/navbar.component';
import { ChatbotWidgetComponent } from '../shared/components/chatbot-widget/chatbot-widget.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent, NavbarComponent, ChatbotWidgetComponent],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.css']
})
export class LayoutComponent {
  breadcrumbs = signal<{label: string, url: string}[]>([]);
  private router = inject(Router);

  constructor() {
    this.buildBreadcrumbs(this.router.url);
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.buildBreadcrumbs(event.urlAfterRedirects);
    });
  }

  private buildBreadcrumbs(url: string) {
    const segments = url.split('/').filter(s => s && s.indexOf('?') === -1);
    const crumbs = [];
    let currentUrl = '';
    
    // Add home
    crumbs.push({ label: 'Accueil', url: '/dashboard' });

    for (const segment of segments) {
      if (segment === 'dashboard') continue; // Skip dashboard as it's already 'Accueil'
      
      currentUrl += `/${segment}`;
      // Basic formatting: capitalize first letter
      const label = segment.charAt(0).toUpperCase() + segment.slice(1).replace(/-/g, ' ');
      crumbs.push({ label, url: currentUrl });
    }
    
    this.breadcrumbs.set(crumbs);
  }
}
