import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApplicationService {
  private apiUrl = `${environment.apiUrl}/applications`;

  constructor(private http: HttpClient) {}

  postuler(offerId: string, message: string, cv: File, lettre: File): Observable<any> {
    const formData = new FormData();
    formData.append('OfferId', offerId);
    if (message) {
      formData.append('Message', message);
    }
    formData.append('cv', cv);
    formData.append('lettre', lettre);

    return this.http.post(this.apiUrl, formData);
  }

  getReceivedApplications(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/received`);
  }

  getAcceptedForSchool(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/accepted-for-school`);
  }

  getMyApplications(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/my`);
  }

  accept(id: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}/accept`, {});
  }

  reject(id: string, commentaire: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}/reject`, { commentaire });
  }
}
