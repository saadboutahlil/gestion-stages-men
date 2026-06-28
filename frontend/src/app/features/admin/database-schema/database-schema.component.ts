import { Component, OnInit, ElementRef, ViewChild } from '@angular/core';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { environment } from '../../../../environments/environment';
import mermaid from 'mermaid';

@Component({
  selector: 'app-database-schema',
  standalone: true,
  imports: [CommonModule, HttpClientModule],
  template: `
    <div class="schema-container">
      <div class="header">
        <h1>Schéma de la Base de Données</h1>
        <div class="actions">
          <button (click)="downloadExcel()" class="btn-excel">
            <i class="fas fa-file-excel"></i> Télécharger Données de Test (Excel)
          </button>
        </div>
      </div>

      <div class="card shadow">
        <div class="card-body">
          <div *ngIf="loading" class="status-msg">
            <i class="fas fa-spinner fa-spin"></i> Analyse du schéma en cours...
          </div>
          <div *ngIf="error" class="status-msg error">
            <i class="fas fa-exclamation-triangle"></i> {{ error }}
            <br>
            <button (click)="fetchSchema()" class="btn-retry">Réessayer</button>
          </div>
          <div #mermaidDiv class="mermaid-diagram" [hidden]="loading || error">
            <!-- Le diagramme sera injecté ici -->
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .schema-container { padding: 2rem; }
    .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 2rem; }
    .btn-excel { 
      background: #1d6f42; color: white; border: none; padding: 0.75rem 1.5rem; 
      border-radius: 8px; cursor: pointer; display: flex; align-items: center; gap: 0.5rem;
      transition: background 0.3s;
    }
    .btn-excel:hover { background: #145a32; }
    .btn-retry { margin-top: 1rem; padding: 0.5rem 1rem; border-radius: 4px; cursor: pointer; }
    .mermaid-diagram { 
      background: white; min-height: 600px; display: flex; justify-content: center; overflow: auto;
      padding: 1rem; width: 100%;
    }
    .status-msg { padding: 3rem; text-align: center; font-size: 1.2rem; color: #666; }
    .status-msg.error { color: #d32f2f; }
    .card { background: white; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.08); min-height: 400px; }
    .card-body { padding: 1rem; }
  `]
})
export class DatabaseSchemaComponent implements OnInit {
  @ViewChild('mermaidDiv') mermaidDiv!: ElementRef;
  loading = true;
  error: string | null = null;

  constructor(private http: HttpClient) {
    mermaid.initialize({
      startOnLoad: false,
      theme: 'default',
      securityLevel: 'loose',
      er: { useMaxWidth: true }
    });
  }

  ngOnInit(): void {
    this.fetchSchema();
  }

  fetchSchema(): void {
    this.loading = true;
    this.error = null;
    const infoUrl = `${environment.apiUrl}/DatabaseSchema/info`;
    this.http.get<any>(infoUrl).subscribe({
      next: (data) => {
        if (data.error) {
          this.error = `Erreur: ${data.message}`;
          this.loading = false;
          return;
        }
        this.renderDiagram(data);
      },
      error: (err) => {
        console.error('Erreur de connexion API', err);
        this.error = "Erreur de connexion. Vérifiez la console (F12) ou réessayez.";
        this.loading = false;
      }
    });
  }

  renderDiagram(data: any): void {
    try {
      let definition = 'erDiagram\n';

      data.tables.forEach((t: any) => {
        const tableName = t.name.replace(/\s+/g, '_');
        definition += `    ${tableName} {\n`;
        t.columns.forEach((c: any) => {
          const type = c.type.split('`')[0].split('<')[0].replace(/Nullable/g, '').replace(/Guid/g, 'uuid').replace(/String/g, 'string').replace(/Int32/g, 'int').toLowerCase();
          const pk = c.isPk ? 'PK' : '';
          const fk = c.isFk ? 'FK' : '';
          const desc = c.description ? ` "${c.description}"` : '';
          definition += `        ${type} ${c.name} ${pk} ${fk}${desc}\n`;
        });
        definition += `    }\n`;
      });

      data.relationships.forEach((r: any) => {
        const from = r.from.replace(/\s+/g, '_');
        const to = r.to.replace(/\s+/g, '_');
        definition += `    ${to} ||--o{ ${from} : "${r.col}"\n`;
      });

      mermaid.render('graphDiv', definition).then((result: { svg: string }) => {
        if (this.mermaidDiv) {
          this.mermaidDiv.nativeElement.innerHTML = result.svg;
        }
        this.loading = false;
      }).catch(err => {
        console.error('Mermaid Render Error', err);
        this.error = "Erreur lors du rendu du diagramme.";
        this.loading = false;
      });
    } catch (e) {
      console.error('Processing Error', e);
      this.error = "Erreur de traitement des données.";
      this.loading = false;
    }
  }

  downloadExcel(): void {
    const excelUrl = `${environment.apiUrl}/DatabaseSchema/excel`;
    this.http.get(excelUrl, { responseType: 'blob' }).subscribe(blob => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'donnees-test-gestion-stages.xlsx';
      a.click();
      window.URL.revokeObjectURL(url);
    });
  }
}
