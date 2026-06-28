import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="auth-container">
      <div class="auth-card animate-fade-in">
        <div class="auth-header">
          <div class="logo">
            <i class="fa-solid fa-graduation-cap"></i>
          </div>
          <h1>Rejoignez-nous</h1>
          <p>Créez votre compte pour accéder à la plateforme</p>
        </div>

        <form (ngSubmit)="onSubmit()" #regForm="ngForm">
          <!-- Champs Communs -->
          <div class="form-grid">
            <div class="form-group full">
              <label>Rôle</label>
              <select [(ngModel)]="model.role" name="role" class="form-control" required>
                <option value="Student">Étudiant</option>
                <option value="MinistereRH">RH Ministère</option>
                <option value="Encadrant">Encadrant</option>
                <option value="School">Établissement / École</option>
              </select>
            </div>

            <div class="form-group">
              <label>{{ model.role === 'School' ? "Nom de l'établissement" : 'Nom complet' }}</label>
              <input type="text" [(ngModel)]="model.fullName" name="fullName" class="form-control" placeholder="Ex: Jean Dupont" required>
            </div>

            <div class="form-group">
              <label>Email professionnel</label>
              <input type="email" [(ngModel)]="model.email" name="email" class="form-control" placeholder="exemple@domaine.com" required>
            </div>

            <div class="form-group full">
              <label>Mot de passe</label>
              <input type="password" [(ngModel)]="model.password" name="password" class="form-control" placeholder="••••••••" required>
            </div>

            <!-- ── CHAMPS SPÉCIFIQUES STUDENT ── -->
            <ng-container *ngIf="model.role === 'Student'">
              <div class="form-group">
                <label>CNE / Code Massar</label>
                <input type="text" [(ngModel)]="model.cne" name="cne" class="form-control" placeholder="N° Étudiant" required>
              </div>
              <div class="form-group">
                <label>Établissement</label>
                <input type="text" [(ngModel)]="model.etablissement" name="etablissement" class="form-control" placeholder="Ex: ENSIAS" required>
              </div>
              <div class="form-group">
                <label>Filière</label>
                <input type="text" [(ngModel)]="model.filiere" name="filiere" class="form-control" placeholder="Ex: Génie Logiciel" required>
              </div>
              <div class="form-group">
                <label>Promotion</label>
                <input type="text" [(ngModel)]="model.promotion" name="promotion" class="form-control" placeholder="Ex: 2024-2025" required>
              </div>
            </ng-container>

            <!-- ── CHAMPS SPÉCIFIQUES RH ── -->
            <ng-container *ngIf="model.role === 'MinistereRH'">
              <div class="form-group full">
                <label>Direction (Optionnel)</label>
                <input type="text" [(ngModel)]="model.direction" name="direction" class="form-control" placeholder="Ex: DSI, DRH...">
              </div>
            </ng-container>

            <!-- ── CHAMPS SPÉCIFIQUES ENCADRANT ── -->
            <ng-container *ngIf="model.role === 'Encadrant'">
              <div class="form-group">
                <label>Fonction</label>
                <input type="text" [(ngModel)]="model.fonction" name="fonction" class="form-control" placeholder="Ex: Chef de projet" required>
              </div>
              <div class="form-group">
                <label>Téléphone (Optionnel)</label>
                <input type="tel" [(ngModel)]="model.telephone" name="telephone" class="form-control" placeholder="06XXXXXXXX">
              </div>
            </ng-container>

            <!-- ── CHAMPS SPÉCIFIQUES SCHOOL ── -->
            <ng-container *ngIf="model.role === 'School'">
              <div class="form-group full">
                <label>Adresse de l'établissement</label>
                <input type="text" [(ngModel)]="model.adresse" name="adresse" class="form-control" placeholder="Adresse complète" required>
              </div>
              <div class="form-group">
                <label>Téléphone contact</label>
                <input type="tel" [(ngModel)]="model.telEtablissement" name="telEtablissement" class="form-control" placeholder="05XXXXXXXX" required>
              </div>
            </ng-container>
          </div>

          <div *ngIf="errorMessage()" class="alert alert-danger mt-3">
            {{ errorMessage() }}
          </div>

          <button type="submit" class="btn btn-primary btn-block mt-4" [disabled]="isLoading() || !regForm.valid">
            {{ isLoading() ? 'Création en cours...' : "S'inscrire" }}
          </button>
        </form>

        <div class="auth-footer mt-4">
          <p>Déjà un compte ? <a routerLink="/login">Connectez-vous</a></p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
      padding: 2rem;
    }
    .auth-card {
      background: rgba(255, 255, 255, 0.9);
      backdrop-filter: blur(10px);
      padding: 2.5rem;
      border-radius: 20px;
      box-shadow: 0 15px 35px rgba(0,0,0,0.1);
      width: 100%;
      max-width: 600px;
    }
    .auth-header { text-align: center; margin-bottom: 2rem; }
    .logo {
      width: 60px;
      height: 60px;
      background: var(--primary);
      color: white;
      border-radius: 15px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.5rem;
      margin: 0 auto 1rem;
      box-shadow: 0 8px 16px rgba(0,0,0,0.1);
    }
    h1 { font-size: 1.8rem; font-weight: 700; color: #2d3436; margin-bottom: 0.5rem; }
    .text-muted { color: #636e72; }

    .form-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1.2rem;
    }
    .form-group.full { grid-column: span 2; }
    .form-group label {
      display: block;
      margin-bottom: 0.5rem;
      font-weight: 500;
      color: #2d3436;
      font-size: 0.9rem;
    }
    .form-control {
      width: 100%;
      padding: 0.8rem 1rem;
      border: 2px solid #edf2f7;
      border-radius: 10px;
      transition: all 0.3s;
      font-size: 1rem;
    }
    .form-control:focus {
      border-color: var(--primary);
      outline: none;
      box-shadow: 0 0 0 4px rgba(var(--primary-rgb), 0.1);
    }
    .btn-block { width: 100%; padding: 1rem; font-weight: 600; font-size: 1.1rem; border-radius: 12px; }
    
    .auth-footer { text-align: center; border-top: 1px solid #edf2f7; padding-top: 1.5rem; }
    .auth-footer a { color: var(--primary); font-weight: 600; text-decoration: none; }

    .animate-fade-in { animation: fadeIn 0.6s ease-out; }
    @keyframes fadeIn { from { opacity: 0; transform: translateY(20px); } to { opacity: 1; transform: translateY(0); } }

    @media (max-width: 480px) {
      .form-grid { grid-template-columns: 1fr; }
      .form-group.full { grid-column: span 1; }
    }
  `]
})
export class RegisterComponent {
  model: any = {
    role: 'Student',
    fullName: '',
    email: '',
    password: '',
    cne: '',
    filiere: '',
    promotion: '',
    etablissement: '',
    direction: '',
    fonction: '',
    telephone: '',
    adresse: '',
    telEtablissement: ''
  };

  isLoading = signal(false);
  errorMessage = signal('');

  constructor(private http: HttpClient, private router: Router) {}

  onSubmit() {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.http.post(`${environment.apiUrl}/auth/register`, this.model).subscribe({
      next: () => {
        alert('Compte créé avec succès !');
        this.router.navigate(['/login']);
      },
      error: (err) => {
        this.isLoading.set(false);
        const error = err.error?.error || (err.error?.errors ? err.error.errors[0] : "Erreur lors de l'inscription");
        this.errorMessage.set(error);
      }
    });
  }
}
