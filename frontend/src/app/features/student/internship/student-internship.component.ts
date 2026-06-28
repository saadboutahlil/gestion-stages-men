import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-student-internship',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="mb-4 d-flex justify-content-between align-items-center">
      <div>
        <h1 class="mb-1 text-dark fw-bold">Mon Stage</h1>
        <p class="text-muted">Suivi opérationnel, gestion de vos tâches et évaluation de vos rapports.</p>
      </div>
      <button *ngIf="data()" class="btn btn-outline btn-sm" (click)="load()" [disabled]="isLoading()">
        <i class="fa-solid fa-sync" [class.fa-spin]="isLoading()"></i> Actualiser
      </button>
    </div>

    <div *ngIf="isLoading()" class="text-center py-5">
      <div class="spinner"></div>
    </div>

    <!-- ÉTAT VIDE : ONBOARDING ÉTUDIANT -->
    <div *ngIf="!isLoading() && !data()" class="card border-0 shadow-sm rounded-4 p-5 text-center animate-fade-in">
      <div class="mb-4">
        <div class="icon-circle bg-light text-primary mx-auto d-flex align-items-center justify-content-center" style="width: 80px; height: 80px; border-radius: 50%;">
          <i class="fa-solid fa-graduation-cap fa-3x"></i>
        </div>
      </div>
      <h3 class="fw-bold text-dark mb-2">Bienvenue sur votre Espace Stage</h3>
      <p class="text-muted mx-auto" style="max-width: 600px; font-size: 1.05rem;">
        Vous n'avez pas encore de stage en cours au Ministère. Pour démarrer votre parcours, suivez les étapes indispensables ci-dessous :
      </p>

      <div class="row g-4 my-4 text-start justify-content-center" style="max-width: 800px; margin: 0 auto;">
        <div class="col-md-4">
          <div class="card h-100 p-3 border-0 bg-light rounded-3">
            <h5 class="fw-bold text-primary mb-2">1. Postuler</h5>
            <p class="small text-muted mb-0">Consultez nos offres de stage publiées par les directions et soumettez votre dossier.</p>
          </div>
        </div>
        <div class="col-md-4">
          <div class="card h-100 p-3 border-0 bg-light rounded-3">
            <h5 class="fw-bold text-primary mb-2">2. Convention</h5>
            <p class="small text-muted mb-0">Une fois accepté par le Ministère, votre école initie la convention de stage tripartite.</p>
          </div>
        </div>
        <div class="col-md-4">
          <div class="card h-100 p-3 border-0 bg-light rounded-3">
            <h5 class="fw-bold text-primary mb-2">3. Signature</h5>
            <p class="small text-muted mb-0">Dès signature électronique des trois parties, votre stage est activé ici.</p>
          </div>
        </div>
      </div>

      <div class="mt-4">
        <a routerLink="/offers" class="btn btn-primary btn-lg px-4 rounded-pill shadow-sm">
          <i class="fa-solid fa-search me-2"></i>Découvrir les offres de stage
        </a>
      </div>
    </div>

    <!-- ÉTAT ACTIF : INTERFACE DU HUB -->
    <div *ngIf="!isLoading() && data() as s" class="animate-fade-in">
      
      <!-- SUB-NAVIGATION BAR (ONGLETS) -->
      <div class="mb-4">
        <ul class="nav nav-pills nav-fill bg-white shadow-sm rounded-4 p-1">
          <li class="nav-item">
            <a class="nav-link cursor-pointer rounded-4" [class.active]="activeTab() === 'overview'" (click)="activeTab.set('overview')">
              <i class="fa-solid fa-circle-info me-2"></i>Vue d'ensemble
            </a>
          </li>
          <li class="nav-item">
            <a class="nav-link cursor-pointer rounded-4" [class.active]="activeTab() === 'tasks'" (click)="activeTab.set('tasks')">
              <i class="fa-solid fa-list-check me-2"></i>Missions & Tâches
            </a>
          </li>
          <li class="nav-item">
            <a class="nav-link cursor-pointer rounded-4" [class.active]="activeTab() === 'reports'" (click)="activeTab.set('reports')">
              <i class="fa-solid fa-file-pdf me-2"></i>Rapports & IA Gemini
            </a>
          </li>
        </ul>
      </div>

      <!-- ONGLET 1 : VUE D'ENSEMBLE -->
      <div *ngIf="activeTab() === 'overview'" class="card p-4 rounded-4 border-0 shadow-sm mb-4">
        <div class="d-flex justify-content-between align-items-center mb-4 pb-3 border-bottom">
          <div>
            <h3 class="fw-bold text-dark mb-1">{{ s.sujet }}</h3>
            <span class="badge" [ngClass]="{'badge-success': s.statut==='EnCours', 'badge-info': s.statut==='EnAttente', 'badge-warning': s.statut==='Termine'}">{{ s.statut }}</span>
          </div>
          <button *ngIf="s.statut === 'Termine'" class="btn btn-primary btn-sm rounded-pill px-3" (click)="downloadAttestation(s.id)">
            <i class="fa-solid fa-file-pdf me-1"></i> Télécharger mon Attestation
          </button>
        </div>

        <div class="row g-4 mb-4">
          <div class="col-md-4">
            <div class="p-3 bg-light rounded-3">
              <span class="text-uppercase text-muted d-block small mb-1">Direction d'Affectation</span>
              <strong class="text-dark">{{ s.direction }}</strong>
            </div>
          </div>
          <div class="col-md-4">
            <div class="p-3 bg-light rounded-3">
              <span class="text-uppercase text-muted d-block small mb-1">Date de Début Effective</span>
              <strong class="text-dark">{{ s.dateDebutEffective | date:'dd/MM/yyyy' }}</strong>
            </div>
          </div>
          <div class="col-md-4">
            <div class="p-3 bg-light rounded-3">
              <span class="text-uppercase text-muted d-block small mb-1">Encadrant Terrain</span>
              <strong class="text-dark">{{ s.encadrant || 'Non affecté' }}</strong>
            </div>
          </div>
        </div>

        <!-- Progression globale -->
        <div class="bg-light p-4 rounded-4 mb-2">
          <div class="d-flex justify-content-between align-items-center mb-2">
            <span class="fw-bold text-dark">Progression générale du stage</span>
            <span class="badge bg-primary text-white fs-6 px-3 rounded-pill">{{ s.progression }}%</span>
          </div>
          <div class="progress" style="height: 12px; border-radius: 6px;">
            <div class="progress-bar" role="progressbar" [style.width.%]="s.progression"></div>
          </div>
          <p class="small text-muted mt-2 mb-0">Basé sur la validation des tâches assignées par votre encadrant terrain.</p>
        </div>
      </div>

      <!-- ONGLET 2 : MISSIONS & TÂCHES -->
      <div *ngIf="activeTab() === 'tasks'" class="card p-4 rounded-4 border-0 shadow-sm">
        <h3 class="fw-bold text-dark mb-3">Mes Tâches & Missions</h3>
        <p class="text-muted mb-4">Mettez à jour le statut de vos tâches au fur et à mesure de votre avancement.</p>

        <div class="table-responsive" *ngIf="s.taches?.length > 0">
          <table class="table align-middle">
            <thead>
              <tr>
                <th>Mission / Description</th>
                <th>Statut</th>
                <th>Échéance</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let t of s.taches">
                <td>
                  <strong class="text-dark d-block">{{ t.titre }}</strong>
                  <span class="text-muted text-sm">{{ t.description }}</span>
                </td>
                <td>
                  <span class="badge" [ngClass]="{
                    'badge-success': t.statut==='Terminee',
                    'badge-info': t.statut==='EnCours',
                    'badge-warning': t.statut==='AFaire'
                  }">{{ t.statut }}</span>
                </td>
                <td>{{ t.datePrevue | date:'dd/MM/yyyy' }}</td>
                <td>
                  <button *ngIf="t.statut==='AFaire'" class="btn btn-outline btn-sm rounded-pill" (click)="startTask(t.id)">Démarrer</button>
                  <button *ngIf="t.statut==='EnCours'" class="btn btn-primary btn-sm rounded-pill" (click)="completeTask(t.id)">Terminer</button>
                  <span *ngIf="t.statut==='Terminee'" class="text-success small"><i class="fa-solid fa-check-circle me-1"></i> Validé</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div *ngIf="!s.taches?.length" class="text-center py-5 text-muted">
          <div class="mb-3"><i class="fa-solid fa-list-check fa-3x text-light"></i></div>
          <p>Aucune tâche ne vous a encore été assignée par votre encadrant terrain.</p>
        </div>
      </div>

      <!-- ONGLET 3 : RAPPORTS & IA GEMINI -->
      <div *ngIf="activeTab() === 'reports'" class="animate-fade-in">
        <div class="d-flex justify-content-between align-items-center mb-4">
          <div>
            <h3 class="fw-bold text-dark mb-1">Dépôts & Analyses IA</h3>
            <p class="text-muted mb-0">Déposez vos rapports de stage et accédez aux summaries générés par l'IA.</p>
          </div>
          <button class="btn btn-primary shadow-sm rounded-pill px-4" (click)="showReportModal = true">
            <i class="fa-solid fa-plus me-2"></i>Déposer un rapport
          </button>
        </div>

        <div class="row g-4" *ngIf="reports().length > 0">
          <div class="col-md-4" *ngFor="let r of reports()">
            <div class="card border-0 shadow-sm rounded-4 p-4 h-100 transition-hover">
              <div class="d-flex justify-content-between align-items-start mb-3">
                <div class="report-icon bg-light text-primary rounded-3 d-flex align-items-center justify-content-center" style="width: 50px; height: 50px;">
                  <i class="fa-solid fa-file-pdf fa-2x"></i>
                </div>
                <span class="badge" [ngClass]="{
                  'bg-success-light text-success': r.statut === 'Approuve',
                  'bg-warning-light text-warning': r.statut === 'EnAttente',
                  'bg-danger-light text-danger': r.statut === 'Rejete'
                }">{{ r.statut }}</span>
              </div>
              <h5 class="fw-bold mb-1">{{ r.titre }}</h5>
              <p class="text-muted small mb-3">{{ r.type }} • Envoyé le {{ r.dateDepot | date:'dd/MM/yyyy' }}</p>
              
              <div *ngIf="r.commentaire" class="alert alert-secondary text-sm py-2 px-3 rounded-3 mb-3">
                <strong>RH :</strong> {{ r.commentaire }}
              </div>

              <div class="d-flex gap-2 mt-auto">
                <a [href]="apiUrl + '/reports/download/' + r.id" class="btn btn-outline-primary btn-sm flex-grow-1 rounded-pill" target="_blank">
                  <i class="fa-solid fa-download me-2"></i>Télécharger
                </a>
                <button class="btn btn-primary btn-sm rounded-pill px-3" (click)="summarizeWithAI(r.id)" title="Générer un résumé avec Gemini">
                  ✨ IA
                </button>
              </div>
            </div>
          </div>
        </div>

        <div *ngIf="reports().length === 0" class="card p-5 text-center text-muted border-0 shadow-sm rounded-4">
          <div class="mb-3"><i class="fa-solid fa-file-circle-exclamation fa-3x text-light"></i></div>
          <p>Aucun rapport déposé pour le moment. Utilisez le bouton ci-dessus pour envoyer votre premier livrable.</p>
        </div>
      </div>

    </div>

    <!-- Modal Dépôt de Rapport -->
    <div *ngIf="showReportModal" class="modal-overlay">
      <div class="modal-card shadow-lg">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <h3 class="m-0 fw-bold">Déposer un rapport</h3>
          <button class="btn btn-text border-0 bg-transparent fs-4" (click)="showReportModal = false">✕</button>
        </div>
        
        <form (ngSubmit)="uploadReport()">
          <div class="mb-3">
            <label class="form-label">Titre du rapport</label>
            <input type="text" class="form-control" name="titre" [(ngModel)]="uploadData.titre" placeholder="Ex: Rapport de mi-parcours" required>
          </div>
          <div class="mb-3">
            <label class="form-label">Type</label>
            <select class="form-select form-control" name="type" [(ngModel)]="uploadData.type">
              <option value="MiParcours">Mi-Parcours</option>
              <option value="Final">Final</option>
            </select>
          </div>
          <div class="mb-3">
            <label class="form-label">Description (facultatif)</label>
            <textarea class="form-control" name="description" [(ngModel)]="uploadData.description" rows="3"></textarea>
          </div>
          <div class="mb-4">
            <label class="form-label">Fichier (PDF uniquement)</label>
            <input type="file" class="form-control" (change)="onFileSelected($event)" accept=".pdf" required>
          </div>
          <div class="text-end">
            <button type="button" class="btn btn-text me-2" (click)="showReportModal = false">Annuler</button>
            <button type="submit" class="btn btn-primary px-4 rounded-pill" [disabled]="!selectedFile || !uploadData.titre">Uploader</button>
          </div>
        </form>
      </div>
    </div>

    <!-- Modal IA Gemini -->
    <div *ngIf="isAiModalOpen()" class="modal-overlay">
      <div class="modal-backdrop fade show" style="position: fixed; top: 0; left: 0; width: 100vw; height: 100vh; background: rgba(0,0,0,0.5); z-index: -1;"></div>
      <div class="modal-card shadow-lg" style="max-width: 900px; width: 100%; padding: 2rem; background: white; border-radius: 12px; z-index: 1060; margin: auto;">
        <div class="d-flex justify-content-between align-items-center mb-4">
          <h3 class="m-0 fs-4 fw-bold text-dark"><i class="fa-solid fa-wand-magic-sparkles text-primary me-2"></i> Analyse IA Gemini</h3>
          <button class="btn btn-text fs-5 border-0 bg-transparent" (click)="closeAiModal()">✕</button>
        </div>

        <div *ngIf="isAnalyzing()" class="text-center py-5">
          <div class="spinner mb-3"></div>
          <p class="text-muted fs-5">L'IA analyse les pages de votre rapport en cours...</p>
          <p class="text-muted small">Cela peut prendre jusqu'à 30 secondes.</p>
        </div>
        
        <!-- Erreur IA -->
        <div *ngIf="!isAnalyzing() && aiError()" class="text-center py-4">
          <div class="mb-3"><i class="fa-solid fa-triangle-exclamation fa-3x text-warning"></i></div>
          <h5 class="fw-bold text-dark mb-2">Service temporairement indisponible</h5>
          <p class="text-muted mb-4">{{ aiError() }}</p>
          <div class="d-flex gap-2 justify-content-center">
            <button class="btn btn-primary rounded-pill px-4" (click)="retryAI()"><i class="fa-solid fa-rotate-right me-2"></i>Réessayer</button>
            <button class="btn btn-outline rounded-pill px-4" (click)="closeAiModal()">Fermer</button>
          </div>
        </div>
        
        <div *ngIf="!isAnalyzing() && aiSummary()">
          <div class="row mb-4 align-items-center">
            <div class="col-md-3 text-center d-flex flex-column justify-content-center">
              <h1 class="display-2 fw-bold mb-0" [ngClass]="{'text-success': aiSummary().score >= 70, 'text-warning': aiSummary().score >= 50 && aiSummary().score < 70, 'text-danger': aiSummary().score < 50}">
                {{ aiSummary().score }}%
              </h1>
              <div class="text-muted text-uppercase fw-bold tracking-wide small mt-2">Qualité estimée</div>
            </div>
            <div class="col-md-9">
              <div class="p-4 bg-light rounded-3 border shadow-sm h-100">
                <h5 class="mb-3 text-primary fw-bold"><i class="fa-solid fa-comment-dots me-2"></i> Évaluation technique de l'IA :</h5>
                <div style="max-height: 350px; overflow-y: auto;">
                  <p class="mb-0 lh-lg" style="font-size: 1.05rem; text-align: justify; white-space: pre-wrap;">{{ aiSummary().summary }}</p>
                </div>
              </div>
            </div>
          </div>
          <div class="text-end">
            <button class="btn btn-outline px-4 me-2 rounded-pill" (click)="downloadSummary()">
              <i class="fa-solid fa-download me-2"></i>Télécharger le résumé
            </button>
            <button type="button" class="btn btn-primary px-5 rounded-pill" (click)="closeAiModal()">Fermer</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .progress { height: 10px; background: var(--border); border-radius: 5px; overflow: hidden; }
    .progress-bar { height: 100%; background: var(--primary); transition: width 0.5s; }
    .cursor-pointer { cursor: pointer; }
    .report-icon { width: 50px; height: 50px; }
    .bg-success-light { background: #dcfce7; }
    .bg-warning-light { background: #fef9c3; }
    .bg-danger-light { background: #fee2e2; }
    .transition-hover { transition: transform 0.2s, box-shadow 0.2s; }
    .transition-hover:hover { transform: translateY(-5px); box-shadow: 0 10px 20px rgba(0,0,0,0.05) !important; }
    .spinner { width: 2.5rem; height: 2.5rem; border: 3px solid var(--border); border-radius: 50%; border-top-color: var(--primary); animation: spin 1s linear infinite; margin: 0 auto; }
    @keyframes spin { to { transform: rotate(360deg); } }
    .modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal-card { background: white; padding: 2rem; border-radius: 12px; width: 100%; max-width: 500px; box-shadow: 0 10px 25px rgba(0,0,0,0.2); }
    .form-group label { display: block; margin-bottom: 0.5rem; font-weight: 500; }
    .form-control, .form-select { width: 100%; padding: 0.6rem; border: 1px solid var(--border); border-radius: 6px; }
    .text-sm { font-size: 0.85rem; }
    .text-muted { color: var(--text-muted) !important; }
  `]
})
export class StudentInternshipComponent implements OnInit {
  private http = inject(HttpClient);
  
  data = signal<any>(null);
  isLoading = signal(true);
  activeTab = signal<'overview' | 'tasks' | 'reports'>('overview');

  // Reports list & actions
  reports = signal<any[]>([]);
  apiUrl = environment.apiUrl;
  
  // IA summaries modals
  isAiModalOpen = signal(false);
  isAnalyzing = signal(false);
  aiSummary = signal<any>(null);
  aiError = signal<string | null>(null);
  private aiLastReportId = '';

  // Upload reports variables
  showReportModal = false;
  selectedFile: File | null = null;
  uploadData = { titre: '', type: 'Final', description: '' };

  ngOnInit() {
    this.load();
  }

  load() {
    this.isLoading.set(true);
    this.http.get(`${environment.apiUrl}/internships/my`).subscribe({
      next: (d: any) => { 
        if (d && (d.id || d.Id)) {
          const normalized = {
            id: d.id || d.Id,
            sujet: d.sujet || d.Sujet,
            statut: d.statut || d.Statut,
            direction: d.direction || d.Direction,
            dateDebutEffective: d.dateDebutEffective || d.DateDebutEffective,
            offre: d.offre || d.Offre,
            progression: d.progression || d.Progression,
            tachesTotal: d.tachesTotal || d.TachesTotal,
            tachesTerminees: d.tachesTerminees || d.TachesTerminees,
            encadrant: d.encadrant || d.Encadrant,
            taches: (d.taches || d.Taches || []).map((t: any) => ({
              id: t.id || t.Id,
              titre: t.titre || t.Titre,
              description: t.description || t.Description,
              statut: t.statut || t.Statut,
              datePrevue: t.datePrevue || t.DatePrevue
            }))
          };
          this.data.set(normalized);
          this.loadReports(); // Charger les rapports uniquement si le stage est actif
        } else {
          this.data.set(null);
        }
        this.isLoading.set(false); 
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadReports() {
    this.http.get<any[]>(`${environment.apiUrl}/reports/my`).subscribe({
      next: (data) => {
        const normalized = data.map((r: any) => ({
          id: r.id || r.Id,
          titre: r.titre || r.Titre,
          type: r.type || r.Type,
          dateDepot: r.dateDepot || r.DateDepot,
          statut: r.statut || r.Statut,
          commentaire: r.commentaireReviseur || r.CommentaireReviseur
        }));
        this.reports.set(normalized);
      },
      error: () => this.reports.set([])
    });
  }

  startTask(id: string) {
    this.http.put(`${environment.apiUrl}/tasks/${id}/start`, {}).subscribe(() => this.load());
  }

  completeTask(id: string) {
    this.http.put(`${environment.apiUrl}/tasks/${id}/complete`, {}).subscribe(() => this.load());
  }

  onFileSelected(event: any) {
    if (event.target.files.length > 0) {
      this.selectedFile = event.target.files[0];
    }
  }

  uploadReport() {
    if (!this.selectedFile || !this.data()) return;

    const formData = new FormData();
    formData.append('File', this.selectedFile);
    formData.append('Titre', this.uploadData.titre);
    formData.append('Type', this.uploadData.type);
    formData.append('Description', this.uploadData.description);

    this.http.post(`${environment.apiUrl}/reports/upload/${this.data().id}`, formData).subscribe({
      next: () => {
        alert("Rapport déposé avec succès !");
        this.showReportModal = false;
        this.selectedFile = null;
        this.uploadData = { titre: '', type: 'Final', description: '' };
        this.loadReports();
      },
      error: (err) => {
        alert("Erreur lors de l'envoi du rapport.");
        console.error(err);
      }
    });
  }

  downloadAttestation(id: string) {
    this.http.get(`${environment.apiUrl}/internships/${id}/attestation`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Attestation_${id.substring(0,8)}.pdf`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: () => alert("Erreur lors du téléchargement de l'attestation.")
    });
  }

  // --- REPORT IA SUMMARIZATION (Gemini) ---
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
    const summary = this.aiSummary();
    if (!summary) return;
    const content = `Score d'évaluation : ${summary.score}%\n\nRésumé détaillé :\n${summary.summary}`;
    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `Evaluation_IA_Rapport.txt`;
    a.click();
    window.URL.revokeObjectURL(url);
  }
}
