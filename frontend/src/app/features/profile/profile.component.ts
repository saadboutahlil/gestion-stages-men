import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="profile-page animate-fade-in">
      <!-- Header Hero -->
      <div class="profile-hero rounded-5 mb-5 p-5 text-white shadow-lg overflow-hidden position-relative">
        <div class="row align-items-center position-relative z-index-1">
          <div class="col-auto">
            <div class="avatar-huge shadow-lg">{{ (authService.currentUser()?.fullName || '?')[0] }}</div>
          </div>
          <div class="col">
            <h1 class="display-5 fw-bold mb-1">{{ authService.currentUser()?.fullName }}</h1>
            <div class="d-flex gap-3 align-items-center opacity-75">
              <span><i class="fa-solid fa-envelope me-2"></i>{{ authService.currentUser()?.email }}</span>
              <span class="badge bg-white-20 rounded-pill px-3 py-2 border border-white-10">{{ authService.currentUser()?.role }}</span>
            </div>
          </div>
        </div>
        <div class="hero-decoration"></div>
      </div>

      <div class="row g-4">
        <!-- Infos Personnelles -->
        <div class="col-lg-7">
          <div class="glass-card p-5 rounded-5 shadow-sm h-100">
            <div class="d-flex align-items-center gap-3 mb-5">
              <div class="icon-box bg-blue-light"><i class="fa-solid fa-id-card"></i></div>
              <h4 class="fw-bold mb-0">Informations du compte</h4>
            </div>

            <div class="row g-4">
              <div class="col-12">
                <label class="form-label text-muted fw-semibold small text-uppercase">Nom Complet</label>
                <div class="input-group-custom">
                  <i class="fa-solid fa-user"></i>
                  <input type="text" [(ngModel)]="profileData.fullName" class="form-control-custom" placeholder="Ex: Jean Dupont">
                </div>
              </div>
              <div class="col-12">
                <label class="form-label text-muted fw-semibold small text-uppercase">Adresse Email</label>
                <div class="input-group-custom">
                  <i class="fa-solid fa-at"></i>
                  <input type="email" [(ngModel)]="profileData.email" class="form-control-custom" placeholder="Ex: jean.dupont@email.com">
                </div>
                <div class="form-text mt-2"><i class="fa-solid fa-circle-info me-1"></i> La modification de l'email changera aussi votre identifiant de connexion.</div>
              </div>
            </div>

            <button class="btn btn-primary-premium mt-5 w-100" (click)="updateProfile()" [disabled]="isLoading()">
              <span *ngIf="!isLoading()"><i class="fa-solid fa-save me-2"></i> Enregistrer les modifications</span>
              <span *ngIf="isLoading()"><i class="fa-solid fa-circle-notch fa-spin me-2"></i> Mise à jour...</span>
            </button>
          </div>
        </div>

        <!-- Sécurité -->
        <div class="col-lg-5">
          <div class="glass-card p-5 rounded-5 shadow-sm h-100">
            <div class="d-flex align-items-center gap-3 mb-5">
              <div class="icon-box bg-red-light text-danger"><i class="fa-solid fa-shield-halved"></i></div>
              <h4 class="fw-bold mb-0">Sécurité</h4>
            </div>

            <div class="alert alert-warning-soft rounded-4 p-4 mb-4">
              <p class="mb-0 small"><i class="fa-solid fa-triangle-exclamation me-2"></i> Laissez le champ ci-dessous vide si vous ne souhaitez pas modifier votre mot de passe actuel.</p>
            </div>

            <div class="col-12 mb-3">
              <label class="form-label text-muted fw-semibold small text-uppercase">Ancien mot de passe</label>
              <div class="input-group-custom">
                <i class="fa-solid fa-lock-open"></i>
                <input type="password" [(ngModel)]="profileData.oldPassword" class="form-control-custom" placeholder="Requis pour changer">
              </div>
            </div>
            
            <div class="col-12">
              <label class="form-label text-muted fw-semibold small text-uppercase">Nouveau mot de passe</label>
              <div class="input-group-custom">
                <i class="fa-solid fa-lock"></i>
                <input type="password" [(ngModel)]="profileData.newPassword" class="form-control-custom" placeholder="••••••••">
              </div>
            </div>
          </div>
        </div>
      </div>

    </div>
  `,
  styles: [`
    .profile-page { padding: 30px; --primary: #3b82f6; --primary-dark: #1e3a8a; max-width: 1200px; margin: 0 auto; }

    /* Hero Section */
    .profile-hero {
      background: linear-gradient(135deg, #1e3a8a 0%, #3b82f6 100%);
      padding: 70px !important; /* Plus de padding */
      border-radius: 40px !important;
    }

    .avatar-huge {
      width: 120px;
      height: 120px;
      background: rgba(255, 255, 255, 0.2);
      backdrop-filter: blur(15px);
      border: 6px solid rgba(255, 255, 255, 0.3);
      border-radius: 35px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 4rem;
      font-weight: 800;
      color: white;
      margin-right: 20px;
    }

    .profile-hero h1 { font-size: 3.5rem; }
    .profile-hero span { font-size: 1.2rem; }

    /* Glass Cards */
    .glass-card {
      background: white;
      border: 1px solid #f0f0f0;
      padding: 50px !important; /* Plus d'espace interne */
      border-radius: 35px !important;
      transition: all 0.3s ease;
    }

    .icon-box {
      width: 60px;
      height: 60px;
      border-radius: 18px;
      font-size: 1.5rem;
    }

    h4 { font-size: 1.75rem; }

    /* Custom Inputs */
    .form-label {
      font-size: 1rem;
      letter-spacing: 0.05em;
      margin-bottom: 12px;
      display: block;
    }

    .form-control-custom {
      padding: 18px 18px 18px 60px; /* Plus grand */
      font-size: 1.15rem;
      border-radius: 20px;
      background: #f8fafc;
    }

    .input-group-custom i {
      left: 22px;
      font-size: 1.25rem;
    }

    /* Buttons */
    .btn-primary-premium {
      padding: 20px;
      font-size: 1.25rem;
      border-radius: 22px;
    }

    .alert-warning-soft { background: #fffbeb; border: 1px solid #fef3c7; color: #92400e; }

    /* Animation */
    .animate-fade-in { animation: fadeIn 0.6s ease-out; }
    @keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
  `]
})
export class ProfileComponent implements OnInit {
  authService = inject(AuthService);
  http = inject(HttpClient);
  
  profileData = { fullName: '', email: '', oldPassword: '', newPassword: '' };
  isLoading = signal(false);

  ngOnInit() {
    const user = this.authService.currentUser();
    if (user) {
      this.profileData.fullName = user.fullName;
      this.profileData.email = user.email;
    }
  }

  updateProfile() {
    if (!this.profileData.fullName) return alert('Le nom est requis.');
    
    this.isLoading.set(true);
    this.http.put(`${environment.apiUrl}/auth/profile`, this.profileData).subscribe({
      next: (res: any) => {
        alert('Profil mis à jour avec succès !');
        this.isLoading.set(false);
        this.profileData.oldPassword = '';
        this.profileData.newPassword = ''; // Reset password fields
        
        // Update local user state if needed (or force re-login for email change)
        if (this.profileData.email !== this.authService.currentUser()?.email) {
            alert('Vous avez modifié votre email. Veuillez vous reconnecter avec vos nouveaux identifiants.');
            this.authService.logout();
        }
      },
      error: (err) => {
        alert('Erreur lors de la mise à jour : ' + (err.error?.message || 'Erreur serveur'));
        this.isLoading.set(false);
      }
    });
  }
}
