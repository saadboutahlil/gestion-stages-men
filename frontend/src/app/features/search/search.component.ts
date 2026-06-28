import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.css']
})
export class SearchComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);

  query = '';
  activeTab = 'internships';
  loading = false;
  
  results: any = {
    internships: [],
    agreements: [],
    users: [],
    offers: []
  };

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.query = params['q'] || '';
      if (this.query) {
        this.performSearch();
      }
    });
  }

  performSearch() {
    this.loading = true;
    this.http.get(`${environment.apiUrl}/Search?q=${encodeURIComponent(this.query)}`).subscribe({
      next: (res: any) => {
        this.results = res;
        this.loading = false;
        
        // Auto-select first tab with results
        if (this.results.internships.length > 0) this.activeTab = 'internships';
        else if (this.results.agreements.length > 0) this.activeTab = 'agreements';
        else if (this.results.users.length > 0) this.activeTab = 'users';
        else if (this.results.offers.length > 0) this.activeTab = 'offers';
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  setTab(tab: string) {
    this.activeTab = tab;
  }
}
