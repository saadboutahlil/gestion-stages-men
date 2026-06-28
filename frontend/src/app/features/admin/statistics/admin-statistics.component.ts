import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';

@Component({
  selector: 'app-admin-statistics',
  standalone: true,
  imports: [CommonModule, BaseChartDirective],
  template: `
    <div class="container-fluid">
      <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h1 class="mb-1">Statistiques Globales</h1>
          <p class="text-muted">Aperçu des indicateurs clés de la plateforme.</p>
        </div>
        <div>
          <a href="assets/Rapport_BI.pdf" target="_blank" class="btn btn-primary shadow-sm">
            <i class="fa-solid fa-file-pdf me-2"></i> Télécharger le Rapport Analytique Complet
          </a>
        </div>
      </div>

      <div *ngIf="isLoading()" class="text-center py-5">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Chargement...</span>
        </div>
      </div>

      <div *ngIf="!isLoading()" class="row g-4 mb-4">
        <div class="col-md-3" *ngFor="let stat of statsList()">
          <div class="card h-100 p-3 shadow-sm border-0">
            <div class="d-flex align-items-center">
              <div class="stat-icon me-3" [style.background-color]="stat.color + '20'" [style.color]="stat.color">
                <i [class]="stat.icon"></i>
              </div>
              <div>
                <div class="text-muted small">{{ stat.label }}</div>
                <div class="h3 mb-0 fw-bold">{{ stat.value }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="row g-4" *ngIf="!isLoading()">
        <!-- Bar Chart -->
        <div class="col-md-8">
          <div class="card p-4 border-0 shadow-sm h-100">
            <h4 class="mb-4">Stages par année</h4>
            <div style="display: block;">
              <canvas baseChart
                [data]="barChartData"
                [options]="barChartOptions"
                [type]="barChartType">
              </canvas>
            </div>
          </div>
        </div>

        <div class="col-md-4">
          <div class="d-flex flex-column gap-4 h-100">
            <!-- Doughnut 1 -->
            <div class="card p-4 border-0 shadow-sm flex-fill">
              <h5 class="mb-3">Conventions (Statut)</h5>
              <div style="display: block;">
                <canvas baseChart
                  [data]="doughChartAgreements"
                  [options]="doughChartOptions"
                  [type]="doughChartType">
                </canvas>
              </div>
            </div>
            <!-- Doughnut 2 -->
            <div class="card p-4 border-0 shadow-sm flex-fill">
              <h5 class="mb-3">Rapports par type</h5>
              <div style="display: block;">
                <canvas baseChart
                  [data]="doughChartReports"
                  [options]="doughChartOptions"
                  [type]="doughChartType">
                </canvas>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Section Alertes Sentiment -->
      <div class="mt-4" *ngIf="!isLoading()">
        <div class="card p-4 border-0 shadow-sm">
          <div class="d-flex justify-content-between align-items-center mb-3">
            <h4 class="mb-0"><i class="fa-solid fa-triangle-exclamation text-danger me-2"></i>Alertes Sentiment IA</h4>
            <span class="badge bg-danger rounded-pill">{{ sentimentAlerts().length }} alerte{{ sentimentAlerts().length !== 1 ? 's' : '' }}</span>
          </div>

          <div *ngIf="sentimentAlerts().length === 0" class="text-center py-4 text-muted">
            <i class="fa-solid fa-face-smile fa-2x text-success mb-2 d-block"></i>
            Aucune alerte de sentiment détectée. Tout semble normal.
          </div>

          <div class="alert-list">
            <div *ngFor="let alert of sentimentAlerts()" class="alert-item d-flex gap-3 p-3 mb-2 rounded-3">
              <div class="alert-icon d-flex align-items-center justify-content-center rounded-circle" style="width:42px;height:42px;min-width:42px;background:#fef2f2;color:#ef4444;">
                <i class="fa-solid fa-bell"></i>
              </div>
              <div class="flex-grow-1">
                <div class="d-flex justify-content-between align-items-start">
                  <span class="fw-bold text-dark">{{ alert.userName || 'Inconnu' }}</span>
                  <span class="text-muted small">{{ alert.timestamp | date:'dd/MM/yyyy HH:mm' }}</span>
                </div>
                <p class="text-muted small mb-1 mt-1" *ngIf="parseAlertDetails(alert.details) as d">
                  {{ d.explication }}
                </p>
                <div class="d-flex gap-2 mt-1">
                  <span class="badge bg-danger-subtle text-danger">⚠️ Conflit potentiel</span>
                  <span class="badge" [ngClass]="parseAlertDetails(alert.details)?.source === 'Gemini IA' ? 'bg-primary' : 'bg-secondary'">
                    {{ parseAlertDetails(alert.details)?.source || 'Analyse locale' }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .stat-icon { width: 48px; height: 48px; border-radius: 12px; display: flex; align-items: center; justify-content: center; font-size: 1.25rem; }
    .card { border-radius: 16px; transition: transform 0.2s; }
    .card:hover { transform: translateY(-3px); }
    .alert-item { background: #fff5f5; border: 1px solid #fecaca; }
    .bg-danger-subtle { background: #fef2f2 !important; }
  `]
})
export class AdminStatisticsComponent implements OnInit {
  stats = signal<any>({});
  isLoading = signal(true);
  sentimentAlerts = signal<any[]>([]);

  statsList = signal([
    { label: 'Total Étudiants', value: '0', icon: 'fa-solid fa-user-graduate', color: '#0d6efd' },
    { label: 'Offres Totales', value: '0', icon: 'fa-solid fa-briefcase', color: '#198754' },
    { label: 'Stages en cours', value: '0', icon: 'fa-solid fa-graduation-cap', color: '#ffc107' },
    { label: 'Total Conventions', value: '0', icon: 'fa-solid fa-file-contract', color: '#dc3545' }
  ]);

  public barChartOptions: ChartConfiguration['options'] = { responsive: true, plugins: { legend: { display: false } } };
  public barChartType: ChartType = 'bar';
  public barChartData: ChartData<'bar'> = { labels: [], datasets: [ { data: [], label: 'Stages', backgroundColor: '#0d6efd', borderRadius: 6 } ] };

  public doughChartOptions: ChartConfiguration['options'] = { responsive: true, maintainAspectRatio: false };
  public doughChartType: ChartType = 'doughnut';
  public doughChartAgreements: ChartData<'doughnut'> = { labels: [], datasets: [{ data: [] }] };
  public doughChartReports: ChartData<'doughnut'> = { labels: [], datasets: [{ data: [] }] };

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.load();
    this.loadSentimentAlerts();
  }

  loadSentimentAlerts() {
    this.http.get<any[]>(`${environment.apiUrl}/admin/logs`).subscribe({
      next: (logs) => {
        this.sentimentAlerts.set(
          (logs || []).filter(l => l.action === 'SENTIMENT_ALERT').slice(0, 20)
        );
      },
      error: () => this.sentimentAlerts.set([])
    });
  }

  parseAlertDetails(details: string): any {
    try { return JSON.parse(details); } catch { return null; }
  }

  load() {
    this.http.get<any>(`${environment.apiUrl}/admin/stats`).subscribe({
      next: (data) => {
        this.stats.set(data);
        this.statsList.update(list => [
          { ...list[0], value: data.totalStudents || '0' },
          { ...list[1], value: data.totalOffers || '0' },
          { ...list[2], value: data.activeInternships || '0' },
          { ...list[3], value: data.totalAgreements || '0' }
        ]);

        // Mapping Bar Chart
        if (data.internshipsByYear) {
          this.barChartData = {
            labels: data.internshipsByYear.map((i: any) => i.year),
            datasets: [{ 
              data: data.internshipsByYear.map((i: any) => i.count),
              label: 'Nombre de stages',
              backgroundColor: '#0d6efd',
              borderRadius: 6
            }]
          };
        }

        // Mapping Doughnut Agreements
        if (data.agreementsStatus) {
          this.doughChartAgreements = {
            labels: data.agreementsStatus.map((a: any) => a.status),
            datasets: [{
              data: data.agreementsStatus.map((a: any) => a.count),
              backgroundColor: ['#198754', '#ffc107', '#dc3545', '#0dcaf0']
            }]
          };
        }

        // Mapping Doughnut Reports
        if (data.reportsByType) {
          this.doughChartReports = {
            labels: data.reportsByType.map((r: any) => r.type),
            datasets: [{
              data: data.reportsByType.map((r: any) => r.count),
              backgroundColor: ['#6f42c1', '#20c997', '#fd7e14']
            }]
          };
        }

        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }
}
