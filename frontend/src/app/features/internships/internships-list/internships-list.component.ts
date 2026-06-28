import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-internships-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="mb-4 d-flex justify-content-between align-items-center flex-wrap gap-3">
      <div>
        <h1 class="mb-1">{{ viewTitle() }}</h1>
        <p class="text-muted">{{ viewSubtitle() }}</p>
      </div>
      <div>
        <button *ngIf="authService.hasRole('MinistereRH') || authService.hasRole('Admin')" class="btn btn-outline-success text-nowrap" (click)="exportToExcel()">
          <i class="fa-solid fa-file-excel me-2"></i>Exporter Excel
        </button>
      </div>
    </div>

    <div *ngIf="isLoading()" class="text-center py-5"><div class="spinner"></div></div>

    <div *ngIf="!isLoading() && authService.hasRole('Encadrant')" class="row mb-4 animate-fade-in">
      <div class="col-md-4">
        <div class="card bg-primary text-white h-100 p-3 rounded-4 border-0 shadow-sm">
          <div class="d-flex justify-content-between align-items-center">
            <div>
              <div class="opacity-75 mb-1">Stagiaires Actifs</div>
              <h2 class="mb-0 fw-bold">{{ stats().activeInterns }}</h2>
            </div>
            <div class="fs-1 opacity-50"><i class="fa-solid fa-users"></i></div>
          </div>
        </div>
      </div>
      <div class="col-md-4">
        <div class="card bg-success text-white h-100 p-3 rounded-4 border-0 shadow-sm">
          <div class="d-flex justify-content-between align-items-center">
            <div>
              <div class="opacity-75 mb-1">Tâches Accomplies</div>
              <h2 class="mb-0 fw-bold">{{ stats().tasksDone }} / {{ stats().tasksTotal }}</h2>
            </div>
            <div class="fs-1 opacity-50"><i class="fa-solid fa-check-double"></i></div>
          </div>
        </div>
      </div>
      <div class="col-md-4">
        <div class="card bg-warning text-dark h-100 p-3 rounded-4 border-0 shadow-sm">
          <div class="d-flex justify-content-between align-items-center">
            <div>
              <div class="opacity-75 mb-1">Évaluations à faire</div>
              <h2 class="mb-0 fw-bold">{{ stats().evalsPending }}</h2>
            </div>
            <div class="fs-1 opacity-50"><i class="fa-solid fa-star"></i></div>
          </div>
        </div>
      </div>
    </div>

    <div *ngIf="!isLoading() && authService.hasRole('Encadrant')" class="mb-4">
      <ul class="nav nav-pills nav-fill bg-white shadow-sm rounded-4 p-1">
        <li class="nav-item">
          <a class="nav-link cursor-pointer rounded-4" [class.active]="viewMode() === 'all'" (click)="setTab('all')">Tous mes stagiaires</a>
        </li>
        <li class="nav-item">
          <a class="nav-link cursor-pointer rounded-4" [class.active]="viewMode() === 'tasks'" (click)="setTab('tasks')">Gestion des Tâches</a>
        </li>
        <li class="nav-item">
          <a class="nav-link cursor-pointer rounded-4" [class.active]="viewMode() === 'evals'" (click)="setTab('evals')">Évaluations & Bilans</a>
        </li>
        <li class="nav-item">
          <a class="nav-link cursor-pointer rounded-4" [class.active]="viewMode() === 'reports'" (click)="setTab('reports')">Rapports</a>
        </li>
      </ul>
    </div>

    <!-- TABLEAU SUIVI GLOBAL / MISSIONS / EVALS -->
    <div *ngIf="!isLoading() && viewMode() !== 'reports'" class="card">
      <div class="table-responsive">
        <table class="table">
          <thead>
            <tr>
              <th>Stagiaire / Offre</th>
              <th *ngIf="viewMode() === 'all'">Encadrant</th>
              <th>Progression</th>
              <th>Statut</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let i of internships()">
              <td>
                <strong>{{ i.etudiant }}</strong>
                <div class="text-muted text-sm">{{ i.sujet }}</div>
              </td>
              <td *ngIf="viewMode() === 'all'">
                <span *ngIf="i.encadrant" class="badge badge-info">{{ i.encadrant }}</span>
                <button *ngIf="!i.encadrant && authService.hasRole('MinistereRH') && !i.isArchived" 
                        class="btn btn-outline btn-sm" (click)="openAssignModal(i)">Affecter</button>
                <span *ngIf="!i.encadrant && (!authService.hasRole('MinistereRH') || i.isArchived)" class="text-muted text-sm">Non assigné</span>
              </td>
              <td>
                <div class="d-flex align-items-center gap-2">
                  <div class="progress flex-1"><div class="progress-bar" [style.width.%]="i.progression"></div></div>
                  <span class="text-sm">{{ i.progression }}%</span>
                </div>
              </td>
              <td>
                <span class="badge" [ngClass]="{'badge-success': i.statut==='EnCours', 'badge-warning': i.statut==='Termine'}">{{ i.statut }}</span>
              </td>
              <td>
                <div class="d-flex gap-2">
                  <!-- Boutons Tâches -->
                  <button *ngIf="authService.hasRole('Encadrant') && i.statut === 'EnCours' && (viewMode() === 'all' || viewMode() === 'tasks')" 
                          class="btn btn-primary btn-sm" (click)="openTaskModal(i)">+ Tâche</button>
                  
                  <!-- Boutons Évaluations -->
                  <button *ngIf="authService.hasRole('Encadrant') && i.statut === 'EnCours' && (viewMode() === 'all' || viewMode() === 'evals')" 
                          class="btn btn-warning btn-sm" 
                          [disabled]="!i.auraRapportMiParcoursValide && !i.auraRapportFinalValide"
                          [title]="(!i.auraRapportMiParcoursValide && !i.auraRapportFinalValide) ? 'Attente validation rapport' : 'Evaluer le stagiaire'"
                          (click)="openEvalModal(i)">Évaluer</button>
                  
                  <button *ngIf="authService.hasRole('Encadrant') && i.statut === 'EnCours' && (viewMode() === 'all' || viewMode() === 'evals')" 
                          class="btn btn-outline-danger btn-sm" 
                          [disabled]="!i.auraRapportFinalValide"
                          [title]="!i.auraRapportFinalValide ? 'Rapport final requis pour cloturer' : 'Terminer le stage'"
                          (click)="terminate(i.id)">Clôturer</button>

                  <!-- Bouton Attestation -->
                  <button *ngIf="i.statut === 'Termine'" 
                          class="btn btn-outline-info btn-sm" 
                          (click)="openAttestationModal(i.id)">
                    <i class="fa-solid fa-file-pdf"></i> Attestation
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- TABLEAU DES RAPPORTS DE STAGIAIRES (LECTURE SEULE POUR ENCADRANT) -->
    <div *ngIf="!isLoading() && viewMode() === 'reports'" class="card">
      <div class="table-responsive">
        <table class="table">
          <thead>
            <tr>
              <th>Stagiaire</th>
              <th>Sujet de stage</th>
              <th>Titre du rapport</th>
              <th>Type</th>
              <th>Date Dépôt</th>
              <th>Statut</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let r of reportsList()">
              <td><strong>{{ r.etudiant }}</strong></td>
              <td>{{ r.sujet }}</td>
              <td><strong>{{ r.titre }}</strong></td>
              <td>
                <span class="badge bg-light text-dark border">{{ r.type }}</span>
              </td>
              <td>{{ r.dateDepot | date:'dd/MM/yyyy' }}</td>
              <td>
                <span class="badge" [ngClass]="{
                  'badge-success': r.statut === 'Approuve',
                  'badge-info': r.statut === 'EnAttente',
                  'badge-danger': r.statut === 'Rejete'
                }">{{ r.statut }}</span>
              </td>
              <td>
                <div class="d-flex gap-2">
                  <a [href]="apiUrl + '/reports/download/' + r.id" class="btn btn-outline btn-sm rounded-pill px-3" target="_blank">
                    <i class="fa-solid fa-download me-1"></i>Télécharger
                  </a>
                  <button class="btn btn-primary btn-sm rounded-pill px-3" (click)="summarizeWithAI(r.id)">
                    ✨ IA
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
        <div *ngIf="reportsList().length === 0" class="text-center py-5 text-muted">
          <div class="mb-3"><i class="fa-solid fa-file-circle-question fa-3x text-light"></i></div>
          <p>Aucun rapport n'a été déposé par vos stagiaires pour le moment.</p>
        </div>
      </div>
    </div>

    <!-- Modal Affectation Encadrant -->
    <div *ngIf="showAssignModal" class="modal-overlay">
      <div class="modal-card">
        <h3>Affecter un encadrant</h3>
        <div class="form-group mt-3">
          <label>Sélectionnez l'encadrant terrain</label>
          <select [(ngModel)]="selectedSupervisorId" class="form-control">
            <option *ngFor="let s of supervisors" [value]="s.id">{{ s.nomComplet }} ({{ s.service }})</option>
          </select>
        </div>
        <div class="form-actions mt-4">
          <button class="btn btn-text" (click)="showAssignModal = false">Annuler</button>
          <button class="btn btn-primary" (click)="assignSupervisor()">Confirmer l'affectation</button>
        </div>
      </div>
    </div>

    <!-- Modal Ajout Tâche -->
    <div *ngIf="showTaskModal" class="modal-overlay">
      <div class="modal-card">
        <h3>Nouvelle tâche pour {{ selectedInternship?.etudiant }}</h3>
        <div class="grid-form mt-3">
          <div class="form-group">
            <label>Titre de la tâche</label>
            <input type="text" [(ngModel)]="taskData.titre" class="form-control">
          </div>
          <div class="form-group">
            <label>Description</label>
            <textarea [(ngModel)]="taskData.description" class="form-control"></textarea>
          </div>
          <div class="form-group">
            <label>Échéance prévue</label>
            <input type="date" [(ngModel)]="taskData.datePrevue" class="form-control">
          </div>
        </div>
        <div class="form-actions mt-4">
          <button class="btn btn-text" (click)="showTaskModal = false">Annuler</button>
          <button class="btn btn-primary" (click)="addTask()">Ajouter la tâche</button>
        </div>
      </div>
    </div>

    <!-- Modal Évaluation -->
    <div *ngIf="showEvalModal" class="modal-overlay">
      <div class="modal-card wide">
        <h3>Évaluation de stage</h3>
        <div class="grid-form mt-3">
          <div class="form-group">
            <label>Type</label>
            <select [(ngModel)]="evalData.type" class="form-control">
              <option value="MiParcours">Mi-parcours</option>
              <option value="Finale">Finale (Requiert rapport final)</option>
            </select>
          </div>
          <div class="notes-grid">
            <div class="form-group"><label>Technique /20</label><input type="number" [(ngModel)]="evalData.noteTechnique" min="0" max="20" class="form-control"></div>
            <div class="form-group"><label>Soft Skills /20</label><input type="number" [(ngModel)]="evalData.noteComportement" min="0" max="20" class="form-control"></div>
            <div class="form-group"><label>Autonomie /20</label><input type="number" [(ngModel)]="evalData.noteAutonomie" min="0" max="20" class="form-control"></div>
            <div class="form-group"><label>Note Globale /20</label><input type="number" [(ngModel)]="evalData.noteGlobale" min="0" max="20" class="form-control"></div>
          </div>
          <!-- Évaluation Modal Content -->
          <div class="form-group full" *ngIf="!sentimentResult()">
            <label>Commentaires / Recommandations</label>
            <textarea [(ngModel)]="evalData.recommandations" class="form-control"></textarea>
          </div>

          <!-- Résultat Sentiment (affiché après analyse) -->
          <div *ngIf="sentimentResult()" class="sentiment-card" [ngClass]="{
            'sentiment-positif': sentimentResult().sentiment === 'positif',
            'sentiment-neutre':  sentimentResult().sentiment === 'neutre',
            'sentiment-negatif': sentimentResult().sentiment === 'négatif'
          }">
            <div class="d-flex align-items-center gap-3 mb-2">
              <span class="sentiment-badge">
                {{ sentimentResult().sentiment === 'positif' ? '😊' : sentimentResult().sentiment === 'négatif' ? '😟' : '😐' }}
                Sentiment {{ sentimentResult().sentiment }}
              </span>
              <span *ngIf="sentimentResult().alerte" class="badge bg-danger">
                ⚠️ Alerte — signalée à l'Admin
              </span>
            </div>
            <p class="text-sm mb-0" style="line-height:1.5">{{ sentimentResult().explication }}</p>
          </div>
        </div>
        <div class="form-actions mt-4">
          <ng-container *ngIf="!sentimentResult()">
            <button class="btn btn-text" (click)="showEvalModal = false; sentimentResult.set(null)">Annuler</button>
            <button class="btn btn-primary" (click)="submitEval()" [disabled]="isSentimentLoading()">
              <span *ngIf="isSentimentLoading()" class="spinner-sm me-2"></span>
              <ng-container *ngIf="isSentimentLoading()">Analyse en cours...</ng-container>
              <ng-container *ngIf="!isSentimentLoading()">Enregistrer l'évaluation</ng-container>
            </button>
          </ng-container>
          <ng-container *ngIf="sentimentResult()">
            <button class="btn btn-primary" (click)="showEvalModal = false; sentimentResult.set(null)">Fermer</button>
          </ng-container>
        </div>
      </div>
    </div>
    <!-- Modal Attestation (Choix du signataire) -->
    <div *ngIf="showAttestationModal" class="modal-overlay">
      <div class="modal-card">
        <h3>Générer l'attestation</h3>
        <div class="grid-form mt-3">
          <div class="form-group">
            <label>Nom du signataire</label>
            <input type="text" [(ngModel)]="attestationData.nom" class="form-control" placeholder="ex: Mohammed Tazi">
          </div>
          <div class="form-group">
            <label>Fonction du signataire</label>
            <input type="text" [(ngModel)]="attestationData.fonction" class="form-control" placeholder="ex: Directeur des Ressources Humaines">
          </div>
        </div>
        <div class="form-actions mt-4">
          <button class="btn btn-text" (click)="showAttestationModal = false">Annuler</button>
          <button class="btn btn-primary" (click)="downloadAttestationWithSignatory()">Télécharger le PDF</button>
        </div>
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
    .progress { height: 8px; background: var(--border); border-radius: 4px; overflow: hidden; min-width: 80px; }
    .progress-bar { height: 100%; background: var(--primary); }
    .cursor-pointer { cursor: pointer; }
    .flex-1 { flex: 1; }
    .text-sm { font-size: 0.8rem; }
    .spinner { width: 2.5rem; height: 2.5rem; border: 3px solid var(--border); border-radius: 50%; border-top-color: var(--primary); animation: spin 1s linear infinite; margin: 0 auto; }
    @keyframes spin { to { transform: rotate(360deg); } }
    .modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal-card { background: white; padding: 2rem; border-radius: 12px; width: 100%; max-width: 500px; box-shadow: 0 10px 25px rgba(0,0,0,0.2); }
    .modal-card.wide { max-width: 800px; }
    .grid-form { display: grid; gap: 1rem; }
    .notes-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 1rem; }
    .form-group label { display: block; margin-bottom: 0.5rem; font-weight: 500; font-size: 0.9rem; }
    .form-control, input, select, textarea { width: 100%; padding: 0.6rem; border: 1px solid var(--border); border-radius: 6px; }
    .form-actions { display: flex; justify-content: flex-end; gap: 1rem; }
    .spinner-sm { display: inline-block; width: 1rem; height: 1rem; border: 2px solid #fff6; border-top-color: #fff; border-radius: 50%; animation: spin 0.7s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }
    .sentiment-card { padding: 1rem; border-radius: 12px; border: 1.5px solid; margin-bottom: 0.5rem; }
    .sentiment-positif { background: #f0fdf4; border-color: #22c55e; color: #15803d; }
    .sentiment-neutre  { background: #fffbeb; border-color: #f59e0b; color: #92400e; }
    .sentiment-negatif { background: #fef2f2; border-color: #ef4444; color: #b91c1c; }
    .sentiment-badge { font-weight: 700; font-size: 0.95rem; text-transform: capitalize; }
    .text-sm { font-size: 0.85rem; }
  `]
})
export class InternshipsListComponent implements OnInit {
  internships = signal<any[]>([]);
  isLoading = signal(true);
  viewMode = signal<'all' | 'tasks' | 'evals' | 'reports'>('all');
  apiUrl = environment.apiUrl;
  isAiModalOpen = signal(false);
  isAnalyzing = signal(false);
  aiSummary = signal<any>(null);
  aiError = signal<string | null>(null);
  private aiLastReportId = '';

  // Sentiment analysis
  sentimentResult = signal<any>(null);
  isSentimentLoading = signal(false);
  
  viewTitle = signal('Suivi des Stages');
  viewSubtitle = signal('Gestion opérationnelle et suivi de progression.');

  // Modals
  supervisors: any[] = [];
  showAssignModal = false;
  showTaskModal = false;
  showEvalModal = false;
  showAttestationModal = false;
  selectedInternship: any = null;
  selectedSupervisorId = '';
  selectedAttestationId = '';

  taskData = { titre: '', description: '', datePrevue: '' };
  evalData = { 
    type: 'MiParcours', 
    noteTechnique: 0, 
    noteComportement: 0, 
    noteAutonomie: 0, 
    noteGlobale: 0, 
    recommandations: '' 
  };
  attestationData = {
    nom: 'Le Directeur des Ressources Humaines',
    fonction: 'Directeur des Ressources Humaines'
  };

  private route = inject(ActivatedRoute);

  constructor(private http: HttpClient, public authService: AuthService) {}

  ngOnInit() {
    this.load();
  }

  setTab(mode: 'all' | 'tasks' | 'evals' | 'reports') {
    this.viewMode.set(mode);
    if (mode === 'tasks') {
      this.viewTitle.set('Gestion des Missions');
      this.viewSubtitle.set('Attribuez des tâches et suivez l\'avancement technique.');
    } else if (mode === 'evals') {
      this.viewTitle.set('Bilans & Évaluations');
      this.viewSubtitle.set('Consultez les rapports et validez les compétences.');
    } else if (mode === 'reports') {
      this.viewTitle.set('Rapports des Stagiaires');
      this.viewSubtitle.set('Consultez et téléchargez les rapports déposés par vos stagiaires.');
    } else {
      this.viewTitle.set('Suivi Global des Stagiaires');
      this.viewSubtitle.set('Vue d\'ensemble de vos encadrements en cours.');
    }
  }

  stats() {
    const list = this.internships();
    const activeInterns = list.filter(i => i.statut === 'EnCours').length;
    const tasksTotal = list.reduce((acc, i) => acc + (i.tachesTotal || 0), 0);
    const tasksDone = list.reduce((acc, i) => acc + (i.tachesTerminees || 0), 0);
    // Un stage a besoin d'évaluation s'il est en cours et a un rapport validé. On simplifie en comptant les rapports validés
    const evalsPending = list.filter(i => i.statut === 'EnCours' && (i.auraRapportMiParcoursValide || i.auraRapportFinalValide)).length;
    return { activeInterns, tasksTotal, tasksDone, evalsPending };
  }

  load() {
    let url = `${environment.apiUrl}/internships/supervisor`;
    if (this.authService.hasRole('MinistereRH') || this.authService.hasRole('Admin')) {
      url = `${environment.apiUrl}/internships`;
    } else if (this.authService.hasRole('School')) {
      url = `${environment.apiUrl}/internships/school`;
    }

    this.http.get<any[]>(url).subscribe({
      next: (d) => { this.internships.set(d); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  openAssignModal(i: any) {
    this.selectedInternship = i;
    this.http.get<any[]>(`${environment.apiUrl}/internships/supervisors`).subscribe(data => {
      this.supervisors = data;
      this.showAssignModal = true;
    });
  }

  assignSupervisor() {
    this.http.put(`${environment.apiUrl}/internships/${this.selectedInternship.id}/assign-supervisor`, JSON.stringify(this.selectedSupervisorId), {
      headers: { 'Content-Type': 'application/json' }
    }).subscribe(() => {
      this.showAssignModal = false;
      this.load();
    });
  }

  openTaskModal(i: any) {
    this.selectedInternship = i;
    this.showTaskModal = true;
  }

  addTask() {
    const payload = { 
      ...this.taskData, 
      InternshipId: this.selectedInternship.id,
      datePrevue: this.taskData.datePrevue ? this.taskData.datePrevue : null
    };

    this.http.post(`${environment.apiUrl}/tasks`, payload).subscribe({
      next: () => {
        this.showTaskModal = false;
        this.taskData = { titre: '', description: '', datePrevue: '' };
        this.load();
      },
      error: (err) => {
        alert(err.error?.error || err.error?.title || "Erreur lors de l'ajout de la tâche");
        console.error('Add Task Error:', err);
      }
    });
  }

  openEvalModal(i: any) {
    this.selectedInternship = i;
    this.showEvalModal = true;
  }

  submitEval() {
    this.http.post(`${environment.apiUrl}/evaluations`, { ...this.evalData, internshipId: this.selectedInternship.id }).subscribe({
      next: () => {
        this.showEvalModal = false;
        this.load();
        // Trigger sentiment analysis if there's a comment
        if (this.evalData.recommandations?.trim()) {
          this.isSentimentLoading.set(true);
          this.sentimentResult.set(null);
          this.showEvalModal = true; // Re-open to show badge
          
          const aiPayload = { 
            text: this.evalData.recommandations,
            studentName: this.selectedInternship.etudiant,
            supervisorName: this.selectedInternship.encadrant
          };

          this.http.post<any>(`${environment.apiUrl}/ai/analyze-sentiment`, aiPayload).subscribe({
            next: (res) => {
              this.sentimentResult.set(res);
              this.isSentimentLoading.set(false);
            },
            error: () => {
              this.sentimentResult.set({ sentiment: 'neutre', alerte: false, explication: 'Analyse de sentiment indisponible.' });
              this.isSentimentLoading.set(false);
            }
          });
        }
      },
      error: (err) => alert(err.error?.error || 'Erreur lors de l\'évaluation')
    });
  }

  terminate(id: string) {
    if (confirm('Voulez-vous vraiment clôturer ce stage ?')) {
      this.http.put(`${environment.apiUrl}/internships/${id}/complete`, {}).subscribe({
        next: () => this.load(),
        error: (err) => alert(err.error?.error || 'Erreur lors de la clôture du stage')
      });
    }
  }

  openAttestationModal(id: string) {
    this.selectedAttestationId = id;
    this.showAttestationModal = true;
  }

  downloadAttestationWithSignatory() {
    const params = new URLSearchParams({
      nom: this.attestationData.nom,
      fonction: this.attestationData.fonction
    }).toString();

    this.http.get(`${environment.apiUrl}/internships/${this.selectedAttestationId}/attestation?${params}`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Attestation_${this.selectedAttestationId.substring(0,8)}.pdf`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        this.showAttestationModal = false;
      },
      error: (err) => alert('Erreur lors du téléchargement de l\'attestation.')
    });
  }

  reportsList() {
    const list = this.internships();
    const allReports: any[] = [];
    list.forEach(i => {
      const rapports = i.rapports || i.Rapports;
      if (rapports) {
        rapports.forEach((r: any) => {
          allReports.push({
            id: r.id || r.Id,
            type: r.type || r.Type,
            titre: r.titre || r.Titre,
            statut: r.statut || r.Statut,
            dateDepot: r.dateDepot || r.DateDepot,
            etudiant: i.etudiant || i.Etudiant,
            sujet: i.sujet || i.Sujet
          });
        });
      }
    });
    return allReports.sort((a, b) => new Date(b.dateDepot).getTime() - new Date(a.dateDepot).getTime());
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

  exportToExcel() {
    this.http.get(`${environment.apiUrl}/export/internships`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `Stages_${new Date().toISOString().slice(0, 10)}.xlsx`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
      },
      error: (err) => alert('Erreur lors du téléchargement du fichier Excel.')
    });
  }
}
