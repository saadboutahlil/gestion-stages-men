import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-3">
      <div>
        <h1 class="mb-1">Gestion des Utilisateurs</h1>
        <p class="text-muted">Créer, modifier ou désactiver des comptes.</p>
      </div>
      <div class="d-flex gap-2 align-items-center">
        <select class="form-control" [(ngModel)]="roleFilter" (change)="applyFilter()">
          <option value="">Tous les rôles</option>
          <option value="Student">Étudiant</option>
          <option value="MinistereRH">Ministère RH</option>
          <option value="Encadrant">Encadrant</option>
          <option value="School">École</option>
          <option value="Admin">Admin</option>
        </select>
        <button class="btn btn-outline-success text-nowrap" (click)="exportToExcel()"><i class="fa-solid fa-file-excel me-2"></i>Exporter</button>
        <button class="btn btn-primary text-nowrap" (click)="showCreateForm = !showCreateForm">+ Nouveau</button>
      </div>
    </div>

    <!-- Formulaire création -->
    <div class="card mb-4" *ngIf="showCreateForm">
      <div class="card-header"><h3 class="card-title">Créer un utilisateur</h3></div>
      <div class="card-body">
        <div class="form-row">
          <div class="form-group"><label class="form-label">Nom complet</label><input class="form-control" [(ngModel)]="newUser.fullName"></div>
          <div class="form-group"><label class="form-label">Email</label><input class="form-control" [(ngModel)]="newUser.email" type="email"></div>
          <div class="form-group"><label class="form-label">Mot de passe</label><input class="form-control" [(ngModel)]="newUser.password" type="password"></div>
          <div class="form-group">
            <label class="form-label">Rôle</label>
            <select class="form-control" [(ngModel)]="newUser.role">
              <option value="Student">Étudiant</option>
              <option value="MinistereRH">Ministère RH</option>
              <option value="Encadrant">Encadrant</option>
              <option value="School">École</option>
              <option value="Admin">Admin</option>
            </select>
          </div>
        </div>
        <button class="btn btn-primary mt-3" (click)="createUser()">Créer</button>
      </div>
    </div>

    <div *ngIf="isLoading()" class="text-center py-5"><div class="spinner"></div></div>

    <div *ngIf="!isLoading()" class="card">
      <div class="table-responsive">
        <table class="table">
          <thead><tr><th>Nom</th><th>Email</th><th>Rôle</th><th>Statut</th><th>Créé le</th><th>Actions</th></tr></thead>
          <tbody>
            <tr *ngFor="let u of filteredUsers()">
              <td><strong>{{ u.fullName }}</strong></td>
              <td>{{ u.email }}</td>
              <td><span class="badge badge-primary">{{ u.role }}</span></td>
              <td><span class="badge" [ngClass]="{'badge-success': u.isActive, 'badge-danger': !u.isActive}">{{ u.isActive ? 'Actif' : 'Inactif' }}</span></td>
              <td>{{ u.createdAt | date:'dd/MM/yyyy' }}</td>
              <td>
                <button class="btn btn-outline btn-sm me-1" (click)="openEditModal(u)" title="Modifier"><i class="fa-solid fa-pen"></i></button>
                <button class="btn btn-outline-warning btn-sm me-1" (click)="resetPassword(u)" title="Réinitialiser MDP"><i class="fa-solid fa-key"></i></button>
                <button class="btn btn-sm" [ngClass]="u.isActive ? 'btn-outline-danger' : 'btn-outline-success'" (click)="toggleActive(u)">
                  <i class="fa-solid" [ngClass]="u.isActive ? 'fa-user-slash' : 'fa-user-check'"></i> 
                  {{ u.isActive ? 'Désactiver' : 'Activer' }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Modal Edition -->
    <div *ngIf="showEditModal" class="modal-overlay">
      <div class="modal-card">
        <h3>Modifier l'utilisateur</h3>
        <div class="form-group mt-3">
          <label>Nom complet</label>
          <input class="form-control" [(ngModel)]="editUser.fullName">
        </div>
        <div class="form-group mt-3">
          <label>Rôle</label>
          <select class="form-control" [(ngModel)]="editUser.role">
            <option value="Student">Étudiant</option>
            <option value="MinistereRH">Ministère RH</option>
            <option value="Encadrant">Encadrant</option>
            <option value="School">École</option>
            <option value="Admin">Admin</option>
          </select>
        </div>
        <div class="form-group mt-3 form-check">
          <input type="checkbox" class="form-check-input" id="isActiveCheck" [(ngModel)]="editUser.isActive">
          <label class="form-check-label" for="isActiveCheck">Compte Actif</label>
        </div>
        <div class="form-actions mt-4">
          <button class="btn btn-text" (click)="showEditModal = false">Annuler</button>
          <button class="btn btn-primary" (click)="saveEdit()">Enregistrer</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .form-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem; }
    .btn-sm { padding: 0.25rem 0.5rem; font-size: 0.875rem; }
    .spinner { width: 2.5rem; height: 2.5rem; border: 3px solid var(--border); border-radius: 50%; border-top-color: var(--primary); animation: spin 1s linear infinite; margin: 0 auto; }
    @keyframes spin { to { transform: rotate(360deg); } }
    .modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal-card { background: white; padding: 2rem; border-radius: 12px; width: 100%; max-width: 500px; box-shadow: 0 10px 25px rgba(0,0,0,0.2); }
    .form-actions { display: flex; justify-content: flex-end; gap: 1rem; }
  `]
})
export class AdminUsersComponent implements OnInit {
  users = signal<any[]>([]);
  filteredUsers = signal<any[]>([]);
  isLoading = signal(true);
  
  showCreateForm = false;
  newUser = { fullName: '', email: '', password: '', role: 'Student' };

  showEditModal = false;
  editUser: any = {};
  roleFilter = '';

  constructor(private http: HttpClient) {}

  ngOnInit() { this.load(); }

  load() {
    this.http.get<any>(`${environment.apiUrl}/admin/users?pageSize=1000`).subscribe({
      next: (d) => { 
        this.users.set(d.items || d); 
        this.applyFilter();
        this.isLoading.set(false); 
      },
      error: () => this.isLoading.set(false)
    });
  }

  applyFilter() {
    if (!this.roleFilter) {
      this.filteredUsers.set(this.users());
    } else {
      this.filteredUsers.set(this.users().filter(u => u.role === this.roleFilter));
    }
  }

  createUser() {
    this.http.post(`${environment.apiUrl}/admin/users`, this.newUser).subscribe({
      next: () => { 
        alert('Utilisateur créé avec succès !'); 
        this.showCreateForm = false; 
        this.newUser = { fullName: '', email: '', password: '', role: 'Student' }; 
        this.load(); 
      },
      error: (e) => alert(e.error?.errors?.join('\\n') || 'Erreur lors de la création')
    });
  }

  openEditModal(u: any) {
    this.editUser = { ...u };
    this.showEditModal = true;
  }

  saveEdit() {
    this.http.put(`${environment.apiUrl}/admin/users/${this.editUser.id}`, { 
      fullName: this.editUser.fullName, 
      role: this.editUser.role, 
      isActive: this.editUser.isActive 
    }).subscribe(() => {
      this.showEditModal = false;
      this.load();
    });
  }

  toggleActive(u: any) {
    this.http.put(`${environment.apiUrl}/admin/users/${u.id}`, { fullName: u.fullName, role: u.role, isActive: !u.isActive }).subscribe({
      next: () => this.load(),
      error: (err) => {
        console.error('Erreur toggleActive:', err);
        alert('Erreur lors de la modification du statut : ' + (err.error?.message || err.message));
      }
    });
  }

  resetPassword(u: any) {
    if (confirm(`Êtes-vous sûr de vouloir réinitialiser le mot de passe de ${u.email} ?`)) {
      this.http.post<any>(`${environment.apiUrl}/admin/users/${u.id}/reset-password`, {}).subscribe({
        next: (res) => alert(`Mot de passe réinitialisé avec succès.\nNouveau mot de passe temporaire : ${res.password}\nVeuillez le communiquer à l'utilisateur.`),
        error: (err) => {
          console.error('Erreur resetPassword:', err);
          alert('Erreur lors de la réinitialisation du mot de passe : ' + (err.error?.message || err.message));
        }
      });
    }
  }

  exportToExcel() {
    this.http.get(`${environment.apiUrl}/export/users`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `Utilisateurs_${new Date().toISOString().slice(0, 10)}.xlsx`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
      },
      error: (err) => alert('Erreur lors du téléchargement du fichier Excel.')
    });
  }
}
