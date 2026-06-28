import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-supervisors-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container-fluid">
      <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h1 class="mb-1">Gestion des Encadrants</h1>
          <p class="text-muted">Liste des encadrants de stage du ministère.</p>
        </div>
      </div>

      <div *ngIf="isLoading()" class="text-center py-5">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Chargement...</span>
        </div>
      </div>

      <div *ngIf="!isLoading()" class="card">
        <div class="table-responsive">
          <table class="table table-hover">
            <thead>
              <tr>
                <th>Nom Complet</th>
                <th>Email</th>
                <th>Département</th>
                <th>Stagiaires Actifs</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let s of supervisors()">
                <td><strong>{{ s.fullName }}</strong></td>
                <td>{{ s.email }}</td>
                <td>{{ s.department || 'N/A' }}</td>
                <td><span class="badge bg-info">{{ s.activeInternsCount || 0 }}</span></td>
                <td>
                  <button class="btn btn-sm btn-outline-primary" (click)="openDetails(s)">Détails</button>
                </td>
              </tr>
              <tr *ngIf="supervisors().length === 0">
                <td colspan="5" class="text-center py-4 text-muted">Aucun encadrant trouvé.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
      <!-- Modal Détails -->
      <div *ngIf="selectedSupervisor" class="modal-overlay">
        <div class="modal-card animate-fade-in">
          <div class="d-flex justify-content-between align-items-center mb-4">
            <h3 class="mb-0">Détails Encadrant</h3>
            <button class="btn btn-sm btn-outline-secondary border-0" (click)="selectedSupervisor = null"><i class="fa-solid fa-xmark"></i></button>
          </div>
          
          <div class="mb-3">
            <div class="text-muted text-sm uppercase">Nom Complet</div>
            <div class="fw-bold fs-5">{{ selectedSupervisor.fullName }}</div>
          </div>
          
          <div class="mb-3">
            <div class="text-muted text-sm uppercase">Email de contact</div>
            <div>{{ selectedSupervisor.email }}</div>
          </div>
          
          <div class="mb-3">
            <div class="text-muted text-sm uppercase">Fonction & Département</div>
            <div>{{ selectedSupervisor.fonction || 'Non renseignée' }} — {{ selectedSupervisor.department || 'Non renseigné' }}</div>
          </div>

          <div class="mb-4">
            <div class="text-muted text-sm uppercase">Charge de suivi</div>
            <div><span class="badge bg-primary">{{ selectedSupervisor.activeInternsCount || 0 }} stagiaire(s) en cours</span></div>
          </div>
          
          <div class="text-end mt-4">
            <button class="btn btn-primary" (click)="selectedSupervisor = null">Fermer</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .card { border-radius: 12px; border: 1px solid #eee; overflow: hidden; }
    .table thead th { background: #f8f9fa; border-bottom: 2px solid #eee; }
    .modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal-card { background: white; padding: 2rem; border-radius: 12px; width: 100%; max-width: 500px; box-shadow: 0 10px 25px rgba(0,0,0,0.2); }
    .uppercase { text-transform: uppercase; font-weight: 600; letter-spacing: 0.5px; }
  `]
})
export class SupervisorsListComponent implements OnInit {
  supervisors = signal<any[]>([]);
  isLoading = signal(true);
  selectedSupervisor: any = null;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.load();
  }

  load() {
    this.http.get<any[]>(`${environment.apiUrl}/internships/supervisors`).subscribe({
      next: (data) => {
        this.supervisors.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        // Fallback or empty state
        this.supervisors.set([]);
      }
    });
  }

  openDetails(s: any) {
    this.selectedSupervisor = s;
  }
}
