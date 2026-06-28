import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { LoginResponse, User } from '../models/auth.model';
import { catchError, map, Observable, of, tap } from 'rxjs';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private apiUrl = environment.apiUrl;

  // Signal for reactive user state
  currentUser = signal<User | null>(null);
  isLoading = signal<boolean>(true);

  constructor(private http: HttpClient, private router: Router) {
    this.loadUser();
  }

  login(credentials: any): Observable<boolean> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/auth/login`, credentials).pipe(
      tap(res => this.setToken(res.token)),
      tap(() => this.loadUser()), // Fetch profile after login
      map(() => true),
      catchError(() => of(false))
    );
  }

  registerStudent(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/auth/register-student`, data);
  }

  logout() {
    localStorage.removeItem(this.TOKEN_KEY);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  setToken(token: string) {
    localStorage.setItem(this.TOKEN_KEY, token);
  }

  loadUser() {
    const token = this.getToken();
    if (!token) {
      this.currentUser.set(null);
      this.isLoading.set(false);
      return;
    }

    this.http.get<User>(`${this.apiUrl}/auth/me`).subscribe({
      next: (user) => {
        this.currentUser.set(user);
        this.isLoading.set(false);
      },
      error: () => {
        this.logout();
        this.isLoading.set(false);
      }
    });
  }

  hasRole(role: string): boolean {
    const user = this.currentUser();
    return user ? user.role === role : false;
  }
}
