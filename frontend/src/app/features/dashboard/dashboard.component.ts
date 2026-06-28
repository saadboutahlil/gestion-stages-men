import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  dashboardData = signal<any>(null);
  isLoading = signal<boolean>(true);
  today = new Date();

  constructor(private http: HttpClient, public authService: AuthService) {}

  ngOnInit() {
    this.loadDashboardData();
  }

  loadDashboardData() {
    this.http.get(`${environment.apiUrl}/dashboard/stats`).subscribe({
      next: (data) => {
        this.dashboardData.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }
}
