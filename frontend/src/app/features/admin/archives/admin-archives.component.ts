import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-admin-archives',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="mb-4 d-flex justify-content-between align-items-center flex-column flex-sm-row gap-3 animate-fade-in">
      <div>
        <h1 class="h2 fw-bold text-dark mb-1">Historique des stages</h1>
        <p class="text-muted">Consultation et filtrage des stages historiques et archivés.</p>
      </div>
      
      <!-- Filtre par année -->
      <div class="d-flex align-items-center gap-2">
        <label for="yearFilterSelect" class="form-label mb-0 text-secondary fw-semibold text-nowrap">Année :</label>
        <select id="yearFilterSelect" class="form-control bg-white shadow-sm" style="width: auto; min-width: 180px;" 
                [(ngModel)]="tempYear" (change)="onYearChange(tempYear)">
          <option value="">Toutes les années</option>
          <option value="2023">2023</option>
          <option value="2024">2024</option>
          <option value="2025">2025</option>
          <option value="2026">2026</option>
        </select>
        <button class="btn btn-outline-success text-nowrap shadow-sm" (click)="exportToExcel()"><i class="fa-solid fa-file-excel me-2"></i>Exporter</button>
      </div>
    </div>

    <!-- Spinner de chargement -->
    <div *ngIf="isLoading()" class="text-center py-5">
      <div class="spinner"></div>
      <p class="text-muted mt-2">Chargement des archives...</p>
    </div>

    <!-- Contenu des archives -->
    <div *ngIf="!isLoading()" class="card border-0 shadow-sm rounded-4 animate-fade-in">
      <div class="table-responsive">
        <table class="table mb-0">
          <thead>
            <tr>
              <th style="width: 90px;">Année</th>
              <th>Étudiant</th>
              <th>Téléphone</th>
              <th>Établissement / École</th>
              <th>Dates du stage</th>
              <th>Sujet du stage</th>
              <th>Encadrant</th>
              <th>Convention</th>
              <th>Documents</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let i of archives()">
              <td>
                <span class="badge badge-primary">{{ i.annee }}</span>
              </td>
              <td>
                <div class="fw-bold text-dark text-nowrap">{{ i.fullName }}</div>
              </td>
              <td>
                <span [ngClass]="{'text-muted italic': i.telephone === 'Non renseigné'}">
                  {{ i.telephone }}
                </span>
              </td>
              <td>
                <span [ngClass]="{'text-muted italic': i.etablissement === 'Non renseigné'}">
                  {{ i.etablissement }}
                </span>
              </td>
              <td>
                <div class="text-secondary text-sm text-nowrap">
                  {{ i.dateDebut | date:'dd/MM/yyyy' }} 
                  <span class="text-muted">au</span> 
                  {{ i.dateFin ? (i.dateFin | date:'dd/MM/yyyy') : 'Non renseigné' }}
                </div>
              </td>
              <td>
                <div [ngClass]="{'text-muted italic': i.sujet === 'Non renseigné'}" class="text-secondary text-sm">
                  {{ i.sujet }}
                </div>
              </td>
              <td>
                <span [ngClass]="{'text-muted italic': i.encadrant === 'Non renseigné'}">
                  {{ i.encadrant }}
                </span>
              </td>
              <td>
                <ng-container *ngIf="i.isArchived; else activeConvention">
                  <span class="badge text-nowrap badge-success">Signée</span>
                </ng-container>
                <ng-template #activeConvention>
                  <span class="badge text-nowrap" [ngClass]="getConventionBadgeClass(i.agreementStatus)">
                    {{ getConventionStatusLabel(i.agreementStatus) }}
                  </span>
                </ng-template>
              </td>
              <td>
                <div class="d-flex flex-wrap gap-1" style="max-width: 250px;">
                  <span *ngFor="let doc of i.documents" class="badge badge-info text-xs">
                    {{ doc }}
                  </span>
                  <span *ngIf="!i.documents || i.documents.length === 0" class="text-muted italic text-sm">
                    Aucun
                  </span>
                </div>
              </td>
            </tr>
            <tr *ngIf="archives().length === 0">
              <td colspan="9" class="text-center py-5 text-muted">
                Aucun stage archivé trouvé pour les critères sélectionnés.
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  `,
  styles: [`
    .spinner { 
      width: 2.5rem; 
      height: 2.5rem; 
      border: 3px solid var(--border); 
      border-radius: 50%; 
      border-top-color: var(--primary); 
      animation: spin 1s linear infinite; 
      margin: 0 auto; 
    }
    @keyframes spin { to { transform: rotate(360deg); } }
    .italic { font-style: italic; }
    .fw-bold { font-weight: 600; }
    .text-sm { font-size: 0.85rem; }
    .text-xs { font-size: 0.7rem; padding: 0.2rem 0.5rem; text-transform: none; }
    .gap-1 { gap: 0.25rem; }
    .text-nowrap { white-space: nowrap; }
  `]
})
export class AdminArchivesComponent implements OnInit {
  private http = inject(HttpClient);
  
  archives = signal<any[]>([]);
  isLoading = signal(true);
  selectedYear = signal<string>('');
  tempYear = '';

  ngOnInit() {
    this.load();
  }

  load() {
    this.isLoading.set(true);
    let url = `${environment.apiUrl}/internships/archived`;
    const year = this.selectedYear();
    if (year) {
      url += `?year=${year}`;
    }

    this.http.get<any[]>(url).subscribe({
      next: (data) => {
        this.archives.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Erreur de chargement des archives', err);
        this.isLoading.set(false);
      }
    });
  }

  onYearChange(year: string) {
    this.selectedYear.set(year);
    this.load();
  }

  getConventionBadgeClass(status: string): string {
    if (!status) return 'badge-danger';
    if (status === 'Signee' || status === 'Active' || status === 'Terminee') {
      return 'badge-success';
    }
    if (status.startsWith('Attente')) {
      return 'badge-warning';
    }
    return 'badge-danger';
  }

  getConventionStatusLabel(status: string): string {
    if (!status) return 'Non signée';
    if (status === 'Signee' || status === 'Active' || status === 'Terminee') {
      return 'Signée';
    }
    if (status.startsWith('Attente')) {
      return 'En attente';
    }
    return 'Non signée';
  }

  exportToExcel() {
    const data = this.archives().map(i => ({
      Année: i.annee,
      Étudiant: i.fullName,
      Téléphone: i.telephone,
      Établissement: i.etablissement,
      'Date Début': i.dateDebut ? new Date(i.dateDebut).toLocaleDateString('fr-FR') : '',
      'Date Fin': i.dateFin ? new Date(i.dateFin).toLocaleDateString('fr-FR') : 'Non renseigné',
      'Sujet du stage': i.sujet,
      Encadrant: i.encadrant,
      Convention: i.isArchived ? 'Signée' : this.getConventionStatusLabel(i.agreementStatus)
    }));
    
    if (data.length === 0) return alert('Aucune donnée à exporter.');

    const headers = Object.keys(data[0]);
    const csvRows = [];
    csvRows.push(headers.join(';'));

    for (const row of data) {
      const values = headers.map(header => {
        const val = (row as any)[header] ?? '';
        const escaped = ('' + val).replace(/"/g, '""');
        return `"${escaped}"`;
      });
      csvRows.push(values.join(';'));
    }

    const csvContent = "\ufeff" + csvRows.join('\r\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', `archives_stages_${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
}
