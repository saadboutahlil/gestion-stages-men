import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, NgIf, NgFor, NgClass, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-agreements-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  template: `
    <div class="mb-4 d-flex justify-content-between align-items-center flex-wrap gap-3">
      <div>
        <h1 class="mb-1 text-dark fw-bold">Conventions de Stage</h1>
        <p class="text-muted">Gestion administrative et signature des conventions tripartites.</p>
      </div>
      <div>
        <button *ngIf="mode === 'rh' || mode === 'admin'" class="btn btn-outline-success text-nowrap" (click)="exportToExcel()">
          <i class="fa-solid fa-file-excel me-2"></i>Exporter Excel
        </button>
      </div>
    </div>

    <!-- TABS POUR LE ROLE ECOLE -->
    @if (mode === 'school') {
      <div class="mb-4">
        <ul class="nav nav-pills nav-fill bg-white shadow-sm rounded-4 p-1">
          <li class="nav-item">
            <a class="nav-link cursor-pointer rounded-4" [class.active]="schoolTab() === 'agreements'" (click)="setSchoolTab('agreements')">
              <i class="fa-solid fa-file-contract me-2"></i>Suivi & Signatures
            </a>
          </li>
          <li class="nav-item">
            <a class="nav-link cursor-pointer rounded-4" [class.active]="schoolTab() === 'initiate'" (click)="setSchoolTab('initiate')">
              <i class="fa-solid fa-plus-circle me-2"></i>À initialiser (Candidatures acceptées)
            </a>
          </li>
        </ul>
      </div>
    }

    @if (isLoading()) {
      <div class="text-center py-5"><div class="spinner"></div></div>
    }

    @if (!isLoading()) {
      
      <!-- ONGLET SUIVI & SIGNATURES (CONVENTIONS EXISTANTES) -->
      @if (mode !== 'school' || schoolTab() === 'agreements') {
        <div class="card border-0 shadow-sm rounded-4">
          <div class="table-responsive">
            <table class="table align-middle">
              <thead>
                <tr>
                  <th>Étudiant</th>
                  <th>Offre</th>
                  <th>Période</th>
                  <th>Statut</th>
                  <th>Signatures</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (a of agreements(); track a.id) {
                  <tr>
                    <td><strong>{{ a.etudiant }}</strong></td>
                    <td>{{ a.offre }}</td>
                    <td>{{ a.dateDebut | date:'dd/MM/yyyy' }} → {{ a.dateFin | date:'dd/MM/yyyy' }}</td>
                    <td>
                      <span class="badge" [ngClass]="{
                        'badge-info': a.statut && a.statut.includes('Attente'),
                        'badge-success': a.statut === 'Signee' || a.statut === 'Active',
                        'badge-warning': a.statut === 'Brouillon'
                      }">{{ a.statut }}</span>
                    </td>
                    <td class="signatures">
                      <span [class.signed]="a.signatureEtudiantAt" title="Étudiant">👤</span>
                      <span [class.signed]="a.signatureRHAt" title="Ministère">🏛️</span>
                      <span [class.signed]="a.signatureEcoleAt" title="École">🎓</span>
                    </td>
                    <td>
                      <div class="d-flex gap-2">
                        @if (a.statut === 'AttenteRemplissageRH' && mode === 'rh' && !a.isArchived) {
                          <button class="btn btn-warning btn-sm rounded-pill px-3" (click)="openFillModal(a)">Remplir</button>
                        }
                        @if (a.statut === 'AttenteSignatureRH' && mode === 'rh' && !a.isArchived) {
                          <button class="btn btn-primary btn-sm rounded-pill px-3" (click)="sign(a.id, 'rh')">Signer RH</button>
                        }
                        @if (a.statut === 'AttenteSignatureEcole' && mode === 'school' && !a.isArchived) {
                          <button class="btn btn-primary btn-sm rounded-pill px-3" (click)="sign(a.id, 'school')">Signer École</button>
                        }
                        @if (a.statut === 'AttenteSignatureEtudiant' && mode === 'student') {
                          <button class="btn btn-primary btn-sm rounded-pill px-3" (click)="sign(a.id, 'student')">Signer</button>
                        }
                        @if (a.statut === 'Signee' || a.statut === 'Active') {
                          <button class="btn btn-outline btn-sm rounded-pill px-3" (click)="downloadPdf(a.id)">📄 PDF</button>
                        }
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
            @if (agreements().length === 0) {
              <p class="text-center text-muted py-5 my-0">Aucune convention trouvée.</p>
            }
          </div>
        </div>
      }

      <!-- ONGLET A INITIALISER (ROLE ECOLE UNIQUEMENT) -->
      @if (mode === 'school' && schoolTab() === 'initiate') {
        <div class="card border-0 shadow-sm rounded-4">
          <div class="table-responsive">
            <table class="table align-middle">
              <thead>
                <tr>
                  <th>Étudiant</th>
                  <th>Offre de stage</th>
                  <th>Direction</th>
                  <th>Acceptée le</th>
                  <th class="text-end">Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (app of acceptedApps(); track app.id) {
                  <tr>
                    <td>
                      <strong>{{ app.etudiant }}</strong>
                      <div class="text-muted text-sm">{{ app.filiere }}</div>
                    </td>
                    <td>{{ app.offre }}</td>
                    <td>{{ app.direction }}</td>
                    <td>{{ app.datePostulation | date:'dd/MM/yyyy' }}</td>
                    <td class="text-end">
                      <button class="btn btn-primary btn-sm rounded-pill px-3" (click)="openAgreementModal(app)">
                        <i class="fa-solid fa-plus me-1"></i>Créer convention
                      </button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
            @if (acceptedApps().length === 0) {
              <p class="text-center text-muted py-5 my-0">Aucune candidature acceptée en attente de génération de convention.</p>
            }
          </div>
        </div>
      }

    }

    <!-- Modal Remplissage RH -->
    @if (fillingAgreement) {
      <div class="modal-overlay">
        <div class="modal-card wide">
          <div class="d-flex justify-content-between align-items-center mb-3">
            <h3 class="fw-bold m-0 text-dark">Compléter la convention (RH)</h3>
            <button class="btn btn-text border-0 bg-transparent fs-4" (click)="fillingAgreement = null">✕</button>
          </div>
          <form (ngSubmit)="fillRH()" #rhF="ngForm" class="grid-form">
            <div class="form-group full">
              <label class="form-label">Missions concrètes du stagiaire</label>
              <textarea [(ngModel)]="fillData.missionsConcretes" name="missionsConcretes" class="form-control" rows="3" required></textarea>
            </div>
            <div class="form-group">
              <label class="form-label">Tuteur de stage (Nom complet)</label>
              <input type="text" [(ngModel)]="fillData.nomTuteur" name="nomTuteur" class="form-control" required>
            </div>
            <div class="form-group">
              <label class="form-label">Fonction du tuteur</label>
              <input type="text" [(ngModel)]="fillData.fonctionTuteur" name="fonctionTuteur" class="form-control" required>
            </div>
            <div class="form-group">
              <label class="form-label">Email Tuteur</label>
              <input type="email" [(ngModel)]="fillData.emailTuteur" name="emailTuteur" class="form-control" required>
            </div>
            <div class="form-group">
              <label class="form-label">Téléphone Tuteur</label>
              <input type="text" [(ngModel)]="fillData.telephoneTuteur" name="telephoneTuteur" class="form-control">
            </div>
            <div class="form-group">
              <label class="form-label">Gratification mensuelle (DH)</label>
              <input type="number" [(ngModel)]="fillData.gratificationMensuelle" name="gratificationMensuelle" class="form-control">
            </div>
            <div class="form-group">
              <label class="form-label">Horaires de travail</label>
              <input type="text" [(ngModel)]="fillData.horairesTravail" name="horairesTravail" class="form-control" placeholder="ex: 09h00 - 16h30">
            </div>
            <div class="form-group full">
              <div class="checkbox-group">
                <input type="checkbox" [(ngModel)]="fillData.teletravailPossible" name="teletravailPossible" id="tele">
                <label for="tele" class="m-0 cursor-pointer">Télétravail autorisé</label>
              </div>
            </div>
            <div class="form-group full">
              <label class="form-label">Grille d'évaluation (Critères RH)</label>
              <textarea [(ngModel)]="fillData.grilleEvaluation" name="grille" class="form-control" rows="2" placeholder="Détaillez les critères d'évaluation..."></textarea>
            </div>
            <div class="form-actions mt-3 text-end full">
              <button type="button" class="btn btn-text me-2" (click)="fillingAgreement = null">Annuler</button>
              <button type="submit" class="btn btn-primary px-4 rounded-pill" [disabled]="!rhF.valid">Valider et envoyer</button>
            </div>
          </form>
        </div>
      </div>
    }

    <!-- Modal Création Convention (Role École) -->
    @if (selectedAppId) {
      <div class="modal-overlay">
        <div class="modal-card">
          <div class="d-flex justify-content-between align-items-center mb-3">
            <h3 class="fw-bold m-0 text-dark">Nouvelle Convention</h3>
            <button class="btn btn-text border-0 bg-transparent fs-4" (click)="selectedAppId = null">✕</button>
          </div>
          <form (ngSubmit)="createAgreement()" #f="ngForm" class="grid-form">
            <div class="form-group">
              <label class="form-label">N° Étudiant</label>
              <input type="text" [(ngModel)]="agreementData.numeroEtudiant" name="num" class="form-control" required>
            </div>
            <div class="form-group">
              <label class="form-label">Année d'étude</label>
              <input type="text" [(ngModel)]="agreementData.anneeEtude" name="annee" class="form-control" placeholder="Ex: 3ème année / Master 1" required>
            </div>
            <div class="form-group full">
              <label class="form-label">Filière / Parcours académique</label>
              <input type="text" [(ngModel)]="agreementData.parcours" name="parc" class="form-control" placeholder="Ex: Génie Logiciel" required>
            </div>
            <div class="form-group">
              <label class="form-label">Date de début</label>
              <input type="date" [(ngModel)]="agreementData.dateDebut" name="start" class="form-control" required>
            </div>
            <div class="form-group">
              <label class="form-label">Date de fin</label>
              <input type="date" [(ngModel)]="agreementData.dateFin" name="end" class="form-control" required>
            </div>
            <div class="form-group full">
              <label class="form-label">Objectifs pédagogiques</label>
              <textarea [(ngModel)]="agreementData.objectifsPedagogiques" name="obj" class="form-control" rows="2" placeholder="Quels sont les objectifs du stage ?"></textarea>
            </div>
            <div class="form-group full">
              <label class="form-label">Cadre d'apprentissage</label>
              <textarea [(ngModel)]="agreementData.cadreApprentissage" name="cadre" class="form-control" rows="2" placeholder="Dans quel cadre académique s'inscrit ce stage ?"></textarea>
            </div>
            <div class="form-actions mt-3 text-end full">
              <button type="button" class="btn btn-text me-2" (click)="selectedAppId = null">Annuler</button>
              <button type="submit" class="btn btn-primary px-4 rounded-pill" [disabled]="!f.valid">Générer la convention</button>
            </div>
          </form>
        </div>
      </div>
    }
  `,
  styles: [`
    .signatures span { opacity: 0.3; font-size: 1.25rem; margin-right: 0.25rem; }
    .signatures span.signed { opacity: 1; }
    .btn-sm { padding: 0.25rem 0.5rem; font-size: 0.8rem; }
    .spinner { width: 2.5rem; height: 2.5rem; border: 3px solid var(--border); border-radius: 50%; border-top-color: var(--primary); animation: spin 1s linear infinite; margin: 0 auto; }
    @keyframes spin { to { transform: rotate(360deg); } }
    .modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal-card { background: white; padding: 2rem; border-radius: 12px; width: 100%; max-width: 600px; box-shadow: 0 10px 25px rgba(0,0,0,0.2); }
    .modal-card.wide { max-width: 800px; }
    .grid-form { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .form-group.full { grid-column: span 2; }
    .form-group label { display: block; font-weight: 500; margin-bottom: 0.25rem; font-size: 0.9rem; }
    .form-group input, .form-group textarea { width: 100%; padding: 0.5rem; border: 1px solid var(--border); border-radius: 6px; }
    .checkbox-group { display: flex; align-items: center; gap: 0.5rem; margin-top: 0.5rem; }
    .cursor-pointer { cursor: pointer; }
  `]
})
export class AgreementsListComponent implements OnInit {
  agreements = signal<any[]>([]);
  isLoading = signal(true);
  mode = '';  // 'rh', 'school', 'student'
  
  // Specific to School role
  schoolTab = signal<'initiate' | 'agreements'>('agreements');
  acceptedApps = signal<any[]>([]);

  // Form logic for draft agreement creation
  selectedAppId: string | null = null;
  agreementData = {
    numeroEtudiant: '', anneeEtude: '', parcours: '',
    dateDebut: '', dateFin: '',
    objectifsPedagogiques: '', cadreApprentissage: '',
    nombreVisites: 0, livrablesAttendus: '', criteresEvaluation: ''
  };

  // Filling state (RH)
  fillingAgreement: any = null;
  fillData = {
    missionsConcretes: '', nomTuteur: '', fonctionTuteur: '',
    emailTuteur: '', telephoneTuteur: '', gratificationMensuelle: 0,
    horairesTravail: '', teletravailPossible: false, moyensFournis: '',
    grilleEvaluation: ''
  };

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) { }

  ngOnInit() {
    if (this.authService.hasRole('School')) {
      this.mode = 'school';
      this.setSchoolTab('agreements');
    } else {
      if (this.authService.hasRole('Student')) this.mode = 'student';
      else if (this.authService.hasRole('MinistereRH') || this.authService.hasRole('Admin')) this.mode = 'rh';
      this.load();
    }
  }

  setSchoolTab(tab: 'initiate' | 'agreements') {
    this.schoolTab.set(tab);
    if (tab === 'initiate') {
      this.loadAcceptedApps();
    } else {
      this.load();
    }
  }

  load() {
    this.isLoading.set(true);
    let endpoint = '/agreements';
    if (this.mode === 'school') endpoint = '/agreements/pending-school';
    else if (this.mode === 'student') endpoint = '/agreements/my';

    this.http.get<any[]>(`${environment.apiUrl}${endpoint}`).subscribe({
      next: (data) => {
        const normalized = (data || []).map(a => ({
          id: a.id || a.Id,
          statut: a.statut || a.Statut,
          etudiant: a.etudiant || a.Etudiant,
          offre: a.offre || a.Offre,
          direction: a.direction || a.Direction,
          dateDebut: a.dateDebut || a.DateDebut,
          dateFin: a.dateFin || a.DateFin,
          gratificationMensuelle: a.gratificationMensuelle || a.GratificationMensuelle,
          signatureEtudiantAt: a.signatureEtudiantAt || a.SignatureEtudiantAt,
          signatureRHAt: a.signatureRHAt || a.SignatureRHAt,
          signatureEcoleAt: a.signatureEcoleAt || a.SignatureEcoleAt,
          missionsConcretes: a.missionsConcretes || a.MissionsConcretes,
          nomTuteur: a.nomTuteur || a.NomTuteur,
          fonctionTuteur: a.fonctionTuteur || a.FonctionTuteur,
          emailTuteur: a.emailTuteur || a.EmailTuteur,
          telephoneTuteur: a.telephoneTuteur || a.TelephoneTuteur,
          horairesTravail: a.horairesTravail || a.HorairesTravail,
          teletravailPossible: a.teletravailPossible || a.TeletravailPossible,
          moyensFournis: a.moyensFournis || a.MoyensFournis,
          grilleEvaluation: a.grilleEvaluation || a.GrilleEvaluation,
          isArchived: a.isArchived || a.IsArchived
        }));
        this.agreements.set(normalized);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadAcceptedApps() {
    this.isLoading.set(true);
    this.http.get<any[]>(`${environment.apiUrl}/applications/accepted-for-school`).subscribe({
      next: (data) => {
        const normalized = (data || []).map((a: any) => ({
          id: a.id || a.Id,
          offre: a.offre || a.Offre,
          etudiant: a.etudiant || a.Etudiant,
          filiere: a.filiere || a.Filiere,
          etablissement: a.etablissement || a.Etablissement,
          statut: a.statut || a.Statut,
          datePostulation: a.datePostulation || a.DatePostulation,
          direction: a.direction || a.Direction
        }));
        this.acceptedApps.set(normalized);
        this.isLoading.set(false);
      },
      error: () => {
        this.acceptedApps.set([]);
        this.isLoading.set(false);
      }
    });
  }

  openFillModal(a: any) {
    this.fillingAgreement = a;
    this.fillData = {
      missionsConcretes: a.missionsConcretes || '',
      nomTuteur: a.nomTuteur || '',
      fonctionTuteur: a.fonctionTuteur || '',
      emailTuteur: a.emailTuteur || '',
      telephoneTuteur: a.telephoneTuteur || '',
      gratificationMensuelle: a.gratificationMensuelle || 0,
      horairesTravail: a.horairesTravail || '',
      teletravailPossible: a.teletravailPossible || false,
      moyensFournis: a.moyensFournis || '',
      grilleEvaluation: a.grilleEvaluation || ''
    };
  }

  fillRH() {
    if (!this.fillingAgreement) return;
    this.http.put(`${environment.apiUrl}/agreements/${this.fillingAgreement.id}/fill-rh`, this.fillData).subscribe({
      next: () => {
        alert('Convention complétée !');
        this.fillingAgreement = null;
        this.load();
      },
      error: (e) => alert(e.error?.error || 'Erreur lors du remplissage.')
    });
  }

  sign(id: string, who: string) {
    this.http.put(`${environment.apiUrl}/agreements/${id}/sign/${who}`, {}).subscribe({
      next: () => { alert('Convention signée !'); this.load(); },
      error: (e) => alert(e.error?.error || 'Erreur lors de la signature.')
    });
  }

  downloadPdf(id: string) {
    this.http.get(`${environment.apiUrl}/agreements/${id}/pdf`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Convention-${id.substring(0, 8)}.pdf`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (e) => alert('Erreur lors du téléchargement du PDF.')
    });
  }

  openAgreementModal(app: any) {
    this.selectedAppId = app.id;
    this.agreementData = {
      numeroEtudiant: '',
      anneeEtude: '',
      parcours: app.filiere || '',
      dateDebut: app.datePostulation ? new Date(app.datePostulation).toISOString().split('T')[0] : '',
      dateFin: '',
      objectifsPedagogiques: '',
      cadreApprentissage: '',
      nombreVisites: 0,
      livrablesAttendus: '',
      criteresEvaluation: ''
    };
  }

  createAgreement() {
    if (!this.selectedAppId) return;
    this.http.post(`${environment.apiUrl}/agreements/create/${this.selectedAppId}`, this.agreementData).subscribe({
      next: () => {
        alert('Convention créée avec succès !');
        this.selectedAppId = null;
        this.setSchoolTab('agreements');
      },
      error: (e) => alert(e.error?.error || 'Erreur lors de la création.')
    });
  }

  exportToExcel() {
    this.http.get(`${environment.apiUrl}/export/agreements`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `Conventions_${new Date().toISOString().slice(0, 10)}.xlsx`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
      },
      error: (err) => alert('Erreur lors du téléchargement du fichier Excel.')
    });
  }
}
