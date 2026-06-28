import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { Router } from '@angular/router';

@Component({
  selector: 'app-offer-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="mb-4">
      <h1 class="mb-1">Nouvelle offre de stage</h1>
      <p class="text-muted">Créez une nouvelle opportunité de stage au sein du Ministère.</p>
    </div>

    <div class="card max-w-2xl">
      <div class="card-body">
        <form (ngSubmit)="submit()">
          <div class="form-group mb-3">
            <label class="form-label">Titre de l'offre</label>
            <input type="text" class="form-control" [(ngModel)]="offer.titre" name="titre" required placeholder="Ex: Développeur Full Stack">
          </div>
          
          <div class="form-group mb-3">
            <label class="form-label">Description</label>
            <textarea class="form-control" rows="4" [(ngModel)]="offer.description" name="description" required placeholder="Décrivez les missions..."></textarea>
          </div>
          
          <div class="row">
            <div class="col-md-6 form-group mb-3">
              <label class="form-label">Date de début</label>
              <input type="date" class="form-control" [(ngModel)]="offer.dateDebut" name="dateDebut" required>
            </div>
            <div class="col-md-6 form-group mb-3">
              <label class="form-label">Date de fin</label>
              <input type="date" class="form-control" [(ngModel)]="offer.dateFin" name="dateFin" required>
            </div>
          </div>

          <div class="row">
            <div class="col-md-6 form-group mb-3">
              <label class="form-label">Lieu</label>
              <input type="text" class="form-control" [(ngModel)]="offer.lieu" name="lieu" required placeholder="Ex: Rabat, Siège central">
            </div>
            <div class="col-md-6 form-group mb-4">
              <label class="form-label">Gratification (MAD)</label>
              <input type="number" class="form-control" [(ngModel)]="offer.gratificationMensuelle" name="gratification" placeholder="Optionnel">
            </div>
          </div>
          
          <div class="d-flex justify-content-end gap-2">
            <button type="button" class="btn btn-outline" (click)="cancel()">Annuler</button>
            <button type="submit" class="btn btn-primary" [disabled]="isSubmitting">
              <span *ngIf="isSubmitting">Enregistrement...</span>
              <span *ngIf="!isSubmitting">Créer l'offre</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .max-w-2xl { max-width: 800px; }
  `]
})
export class OfferFormComponent {
  offer = {
    titre: '',
    description: '',
    dateDebut: '',
    dateFin: '',
    lieu: 'Rabat',
    gratificationMensuelle: null as number | null,
    nombrePostes: 1
  };
  isSubmitting = false;

  constructor(private http: HttpClient, private router: Router) {}

  submit() {
    if (!this.offer.titre || !this.offer.description || !this.offer.dateDebut || !this.offer.dateFin || !this.offer.lieu) {
      alert('Veuillez remplir tous les champs obligatoires.');
      return;
    }

    this.isSubmitting = true;
    this.http.post(`${environment.apiUrl}/offers`, this.offer).subscribe({
      next: () => {
        alert('Offre créée avec succès !');
        this.router.navigate(['/ministere/offers']);
      },
      error: (err) => {
        alert(err.error?.error || 'Erreur lors de la création de l\'offre.');
        this.isSubmitting = false;
      }
    });
  }

  cancel() {
    this.router.navigate(['/ministere/offers']);
  }
}
