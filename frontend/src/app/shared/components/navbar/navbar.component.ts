import { Component, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent {
  constructor(private authService: AuthService, private router: Router) {}

  user = computed(() => this.authService.currentUser());

  logout() {
    this.authService.logout();
  }

  onSearch(event: any) {
    const value = event.target?.value?.trim();
    if (value) {
      this.router.navigate(['/search'], { queryParams: { q: value } });
    }
  }
}
