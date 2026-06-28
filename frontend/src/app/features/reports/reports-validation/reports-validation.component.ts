import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-reports-validation',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container-fluid">
      <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h1 class="mb-1">Validation des Rapports</h1>
          <p class="text-muted">Examiner et valider les rapports de stage soumis par les étudiants.</p>
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
                <th>Étudiant</th>
                <th>Titre du Rapport</th>
                <th>Date de Soumission</th>
                <th>Statut</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let r of reports()">
                <td><strong>{{ r.studentName }}</strong></td>
                <td>{{ r.title }}</td>
                <td>{{ r.submittedAt | date:'dd/MM/yyyy HH:mm' }}</td>
                <td>
                  <span class="badge" [ngClass]="{
                    'bg-warning text-dark': r.status === 'Pending',
                    'bg-success': r.status === 'Validated',
                    'bg-danger': r.status === 'Rejected'
                  }">{{ r.status }}</span>
                </td>
                <td>
                  <button class="btn btn-sm btn-outline-primary border-primary me-2" (click)="summarizeWithAI(r.id)" title="Résumer par IA">✨ Résumer</button>
                  <a [href]="apiUrl + '/reports/download/' + r.id" class="btn btn-sm btn-primary me-2" target="_blank">Voir</a>
                  <button class="btn btn-sm btn-success" *ngIf="r.status === 'EnAttente'" (click)="approveReport(r.id)">Valider</button>
                </td>
              </tr>
              <tr *ngIf="reports().length === 0">
                <td colspan="5" class="text-center py-4 text-muted">Aucun rapport en attente de validation.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Modal IA Gemini -->
    <div *ngIf="isAiModalOpen()" class="modal-overlay">
      <div class="modal-card" style="max-width: 900px; padding: 2rem; background: white; border-radius: 8px; margin: 5% auto; position: relative; z-index: 1050;">
        <div class="d-flex justify-content-between align-items-center mb-4">
          <h3 class="m-0 fs-4"><i class="fa-solid fa-wand-magic-sparkles text-primary me-2"></i> Résumé IA Gemini</h3>
          <button class="btn btn-text border-0 bg-transparent fs-5" (click)="closeAiModal()">✕</button>
        </div>

        <div *ngIf="isAnalyzing()" class="text-center py-5">
          <div class="spinner-border text-primary mb-3" style="width: 3rem; height: 3rem;" role="status"></div>
          <p class="text-muted fs-5">L'IA analyse le rapport en cours...</p>
          <p class="text-muted small">Cela peut prendre jusqu'à 30 secondes.</p>
        </div>

        <!-- Erreur IA -->
        <div *ngIf="!isAnalyzing() && aiError()" class="text-center py-4">
          <div class="mb-3"><i class="fa-solid fa-triangle-exclamation fa-3x text-warning"></i></div>
          <h5 class="fw-bold text-dark mb-2">Service temporairement indisponible</h5>
          <p class="text-muted mb-4">{{ aiError() }}</p>
          <div class="d-flex gap-2 justify-content-center">
            <button class="btn btn-primary rounded-pill px-4" (click)="retryAI()"><i class="fa-solid fa-rotate-right me-2"></i>Réessayer</button>
            <button class="btn btn-outline-secondary rounded-pill px-4" (click)="closeAiModal()">Fermer</button>
          </div>
        </div>
        
        <div *ngIf="!isAnalyzing() && aiSummary()">
          <div class="row mb-4">
            <div class="col-md-3 text-center d-flex flex-column justify-content-center">
              <h1 class="display-2 fw-bold mb-0" [ngClass]="{'text-success': aiSummary().score >= 70, 'text-warning': aiSummary().score >= 50 && aiSummary().score < 70, 'text-danger': aiSummary().score < 50}">
                {{ aiSummary().score }}%
              </h1>
              <div class="text-muted text-uppercase fw-bold tracking-wide small mt-2">Qualité estimée</div>
            </div>
            <div class="col-md-9">
              <div class="p-4 bg-light rounded-3 border shadow-sm h-100">
                <h5 class="mb-3 text-primary"><i class="fa-solid fa-comment-dots me-2"></i> Évaluation technique de l'IA :</h5>
                <div style="max-height: 400px; overflow-y: auto;">
                  <p class="mb-0 lh-lg" style="font-size: 1.1rem; text-align: justify; white-space: pre-wrap;">{{ aiSummary().summary }}</p>
                </div>
              </div>
            </div>
          </div>
          <div class="text-end">
            <button class="btn btn-outline-primary btn-lg px-4 me-2" (click)="downloadSummary()">
              <i class="fa-solid fa-download me-2"></i>Télécharger le résumé
            </button>
            <button type="button" class="btn btn-primary btn-lg px-5" (click)="closeAiModal()">Fermer</button>
          </div>
        </div>
      </div>
      <div class="modal-backdrop fade show" style="position: fixed; top: 0; left: 0; width: 100vw; height: 100vh; background: rgba(0,0,0,0.5); z-index: -1;"></div>
    </div>
  `,
  styles: [`
    .card { border-radius: 12px; border: 1px solid #eee; overflow: hidden; }
    .table thead th { background: #f8f9fa; border-bottom: 2px solid #eee; }
  `]
})
export class ReportsValidationComponent implements OnInit {
  reports = signal<any[]>([]);
  isLoading = signal(true);
  apiUrl = environment.apiUrl;

  isAiModalOpen = signal(false);
  isAnalyzing = signal(false);
  aiSummary = signal<any>(null);
  aiError = signal<string | null>(null);
  private aiLastReportId = '';

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.load();
  }

  load() {
    this.http.get<any[]>(`${environment.apiUrl}/reports/pending`).subscribe({
      next: (data) => {
        const normalized = data.map(r => ({
          id: r.id || r.Id,
          studentName: r.etudiant || r.Etudiant,
          title: r.titre || r.Titre,
          submittedAt: r.dateDepot || r.DateDepot,
          status: r.statut || r.Statut
        }));
        this.reports.set(normalized);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.reports.set([]);
      }
    });
  }

  summarizeWithAI(reportId: string) {
    this.aiLastReportId = reportId;
    this.isAiModalOpen.set(true);
    this.isAnalyzing.set(true);
    this.aiSummary.set(null);
    this.aiError.set(null);

    this.http.post(`${environment.apiUrl}/ai/summarize-report/${reportId}`, {}).subscribe({
      next: (res: any) => {
        this.aiSummary.set(res);
        this.isAnalyzing.set(false);
      },
      error: (err) => {
        console.error('AI Error:', err);
        const msg = err.error?.error || err.error?.message || 'Le service IA est temporairement indisponible. Veuillez réessayer dans quelques instants.';
        this.aiError.set(msg);
        this.isAnalyzing.set(false);
      }
    });
  }

  retryAI() {
    if (this.aiLastReportId) this.summarizeWithAI(this.aiLastReportId);
  }

  closeAiModal() {
    this.isAiModalOpen.set(false);
    this.aiSummary.set(null);
    this.aiError.set(null);
  }

  downloadSummary() {
    if (!this.aiSummary()) return;
    const content = `Score d'évaluation : ${this.aiSummary().score}%\n\nRésumé détaillé :\n${this.aiSummary().summary}`;
    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `Evaluation_IA_Rapport.txt`;
    a.click();
    window.URL.revokeObjectURL(url);
  }

  approveReport(reportId: string) {
    if (!confirm("Voulez-vous vraiment valider ce rapport ?")) return;

    this.http.put(`${this.apiUrl}/reports/${reportId}/approve`, { commentaire: "Rapport validé par le responsable RH." }).subscribe({
      next: () => {
        alert("Rapport validé avec succès !");
        this.load(); // Refresh list
      },
      error: (err) => {
        console.error(err);
        alert("Erreur lors de la validation du rapport.");
      }
    });
  }
}
