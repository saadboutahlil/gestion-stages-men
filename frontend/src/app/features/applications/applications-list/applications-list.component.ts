import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/services/auth.service';
import { ApplicationService } from '../../../core/services/application.service';

import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-applications-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './applications-list.component.html',
  styleUrls: ['./applications-list.component.css']
})
export class ApplicationsListComponent implements OnInit {
  applications = signal<any[]>([]);
  isLoading = signal<boolean>(true);

  // Modal state for AI
  isAiModalOpen = signal(false);
  isAnalyzing = signal(false);
  aiResult = signal<any>(null);
  aiError = signal<string | null>(null);
  private aiLastAppId = '';

  // Modal state for Agreement Creation
  selectedAppId: string | null = null;
  agreementData = {
    numeroEtudiant: '', anneeEtude: '', parcours: '',
    dateDebut: '', dateFin: '',
    objectifsPedagogiques: '', cadreApprentissage: '',
    nombreVisites: 0, livrablesAttendus: '', criteresEvaluation: ''
  };

  constructor(
    private http: HttpClient, 
    public authService: AuthService,
    private applicationService: ApplicationService
  ) {}

  ngOnInit() {
    this.loadApplications();
  }

  loadApplications() {
    this.isLoading.set(true);
    let call: Observable<any[]>;
    const isSchoolMode = this.authService.hasRole('School');
    const isStudent = this.authService.hasRole('Student');

    if (isStudent) {
      call = this.applicationService.getMyApplications();
    } else if (isSchoolMode) {
      call = this.applicationService.getAcceptedForSchool();
    } else {
      call = this.applicationService.getReceivedApplications();
    }
    
    call.subscribe({
      next: (data) => {
        // Normalisation des données pour éviter les problèmes de casse PascalCase/camelCase
        const normalized = (data || []).map((a: any) => ({
          id: a.id || a.Id,
          offre: a.offre || a.Offre,
          etudiant: a.etudiant || a.Etudiant,
          filiere: a.filiere || a.Filiere,
          etablissement: a.etablissement || a.Etablissement,
          statut: a.statut || a.Statut,
          datePostulation: a.datePostulation || a.DatePostulation,
          message: a.message || a.Message,
          motifRefus: a.motifRefus || a.MotifRefus,
          direction: a.direction || a.Direction,
          isArchived: a.isArchived || a.IsArchived
        }));
        this.applications.set(normalized);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  acceptApplication(id: string) {
    if (!confirm('Accepter cette candidature ?')) return;
    this.applicationService.accept(id).subscribe(() => this.loadApplications());
  }

  rejectApplication(id: string) {
    const motif = prompt('Motif du refus :');
    if (motif === null) return;
    this.applicationService.reject(id, motif).subscribe(() => this.loadApplications());
  }

  openAgreementModal(app: any) {
    console.log('Opening modal for app:', app);
    this.selectedAppId = app.id;
    this.agreementData.dateDebut = app.datePostulation ? new Date(app.datePostulation).toISOString().split('T')[0] : '';
    this.agreementData.dateFin = '';
  }

  createAgreement() {
    if (!this.selectedAppId) return;
    this.http.post(`${environment.apiUrl}/agreements/create/${this.selectedAppId}`, this.agreementData).subscribe({
      next: () => {
        alert('Convention créée !');
        this.selectedAppId = null;
        this.loadApplications();
      },
      error: (e) => alert(e.error?.error || 'Erreur lors de la création.')
    });
  }

  // --- AI CV MATCHING ---
  analyzeWithAI(appId: string) {
    this.aiLastAppId = appId;
    this.isAiModalOpen.set(true);
    this.isAnalyzing.set(true);
    this.aiResult.set(null);
    this.aiError.set(null);

    this.http.post(`${environment.apiUrl}/ai/match-cv/${appId}`, {}).subscribe({
      next: (res: any) => {
        this.aiResult.set(res);
        this.isAnalyzing.set(false);
      },
      error: (err) => {
        console.error('AI Error:', err);
        let msg = err.error?.error || err.error?.message || 'Erreur du service IA.';
        if (err.error?.details) {
            msg += '\nDetails: ' + err.error.details;
        }
        this.aiError.set(msg);
        this.isAnalyzing.set(false);
      }
    });
  }

  retryAI() {
    if (this.aiLastAppId) this.analyzeWithAI(this.aiLastAppId);
  }

  closeAiModal() {
    this.isAiModalOpen.set(false);
    this.aiResult.set(null);
    this.aiError.set(null);
  }
}
