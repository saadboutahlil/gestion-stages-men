import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-admin-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="mb-4">
      <h1 class="h2 fw-bold text-dark mb-1">Paramètres Système</h1>
      <p class="text-muted">Configurez les constantes et les seuils de la plateforme.</p>
    </div>

    <div class="card border-0 shadow-sm rounded-4 p-4">
      <div class="row g-4">
        <div class="col-md-6" *ngFor="let s of settings()">
          <div class="p-3 bg-light rounded-3">
            <label class="form-label fw-bold text-muted small text-uppercase">{{ s.cle }}</label>
            <div class="input-group">
              <input type="text" [(ngModel)]="s.valeur" class="form-control border-0">
              <button class="btn btn-primary" (click)="save(s)">Mettre à jour</button>
            </div>
            <div class="form-text mt-1">{{ s.description }}</div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class AdminSettingsComponent implements OnInit {
  http = inject(HttpClient);
  settings = signal<any[]>([]);

  ngOnInit() {
    this.http.get<any[]>(`${environment.apiUrl}/admin/settings`).subscribe(data => this.settings.set(data));
  }

  save(setting: any) {
    this.http.put(`${environment.apiUrl}/admin/settings/${setting.cle}`, JSON.stringify(setting.valeur), {
      headers: { 'Content-Type': 'application/json' }
    }).subscribe(() => alert('Paramètre mis à jour.'));
  }
}
