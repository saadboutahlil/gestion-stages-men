import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-admin-logs',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="mb-4">
      <h1 class="h2 fw-bold text-dark mb-1">Journaux d'Audit</h1>
      <p class="text-muted">Historique des actions effectuées par les utilisateurs sur la plateforme.</p>
    </div>

    <div class="card border-0 shadow-sm rounded-4 overflow-hidden">
      <div class="table-responsive">
        <table class="table table-hover mb-0 align-middle">
          <thead class="bg-light small text-uppercase text-muted">
            <tr>
              <th class="ps-4">Date & Heure</th>
              <th>Utilisateur</th>
              <th>Action</th>
              <th>Détails</th>
              <th class="pe-4">IP Address</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let l of logs()">
              <td class="ps-4 text-sm">{{ l.timestamp | date:'dd/MM/yyyy HH:mm:ss' }}</td>
              <td><span class="badge bg-light text-dark fw-normal">{{ l.userName || 'Système' }}</span></td>
              <td><span class="badge" [ngClass]="getActionClass(l.action)">{{ l.action }}</span></td>
              <td class="text-muted small">{{ l.details }}</td>
              <td class="pe-4 small text-muted">{{ l.ipAddress || 'Internal' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  `,
  styles: [`
    .text-sm { font-size: 0.85rem; }
  `]
})
export class AdminLogsComponent implements OnInit {
  http = inject(HttpClient);
  logs = signal<any[]>([]);

  ngOnInit() {
    this.http.get<any[]>(`${environment.apiUrl}/admin/logs`).subscribe(data => this.logs.set(data));
  }

  getActionClass(action: string) {
    if (action.includes('Create')) return 'bg-success text-white';
    if (action.includes('Delete')) return 'bg-danger text-white';
    if (action.includes('Update')) return 'bg-warning text-dark';
    return 'bg-info text-white';
  }
}
