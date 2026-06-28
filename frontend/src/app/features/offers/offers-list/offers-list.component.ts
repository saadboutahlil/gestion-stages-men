import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/services/auth.service';
import { ApplicationService } from '../../../core/services/application.service';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-offers-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './offers-list.component.html',
  styleUrls: ['./offers-list.component.css']
})
export class OffersListComponent implements OnInit {
  offers = signal<any[]>([]);
  isLoading = signal<boolean>(true);
  
  // Application modal state
  selectedOfferId: string | null = null;
  applicationMessage = '';
  isApplying = false;
  cvFile: File | null = null;
  lettreFile: File | null = null;

  constructor(
    private http: HttpClient, 
    public authService: AuthService,
    private applicationService: ApplicationService
  ) {}

  ngOnInit() {
    this.loadOffers();
  }

  loadOffers() {
    this.isLoading.set(true);
    this.http.get<any[]>(`${environment.apiUrl}/offers`).subscribe({
      next: (data) => {
        this.offers.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  openApplyModal(offerId: string) {
    this.selectedOfferId = offerId;
    this.applicationMessage = '';
    this.cvFile = null;
    this.lettreFile = null;
  }

  closeApplyModal() {
    this.selectedOfferId = null;
  }

  onFileChange(event: any, type: string) {
    const file = event.target.files[0];
    if (type === 'cv') this.cvFile = file;
    else if (type === 'lettre') this.lettreFile = file;
  }

  submitApplication() {
    if (!this.selectedOfferId) return;
    if (!this.cvFile || !this.lettreFile) {
      alert("Veuillez sélectionner votre CV et votre lettre de motivation.");
      return;
    }
    
    this.isApplying = true;
    
    this.applicationService.postuler(this.selectedOfferId, this.applicationMessage, this.cvFile, this.lettreFile).subscribe({
      next: () => {
        alert('Candidature envoyée avec succès !');
        this.isApplying = false;
        this.closeApplyModal();
        this.loadOffers(); // Refresh list
      },
      error: (err) => {
        alert(err.error?.error || 'Erreur lors de la candidature.');
        this.isApplying = false;
      }
    });
  }

  closeOffer(offerId: string) {
    if(!confirm('Voulez-vous vraiment fermer cette offre ?')) return;

    this.http.put(`${environment.apiUrl}/offers/${offerId}/close`, {}).subscribe({
      next: () => this.loadOffers(),
      error: () => alert('Erreur lors de la fermeture.')
    });
  }
}
