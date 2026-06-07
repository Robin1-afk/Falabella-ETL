import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';

export interface LoginResponse {
  accessToken:  string;
  refreshToken: string;
  userId:       number;
  name:         string;
  email:        string;
  roleId:       number;
  roleName:     string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY      = 'access_token';
  private readonly REFRESH_KEY    = 'refresh_token';
  private readonly USER_NAME_KEY  = 'user_name';
  private readonly USER_EMAIL_KEY = 'user_email';

  readonly isAuthenticated = signal(this.hasToken());

  constructor(private http: HttpClient, private router: Router) {}

  login(email: string, password: string) {
    return this.http
      .post<LoginResponse>('/api/auth/login', { email, password })
      .pipe(
        tap(res => {
          localStorage.setItem(this.TOKEN_KEY,      res.accessToken);
          localStorage.setItem(this.REFRESH_KEY,    res.refreshToken);
          localStorage.setItem(this.USER_NAME_KEY,  res.name  ?? '');
          localStorage.setItem(this.USER_EMAIL_KEY, res.email ?? '');
          this.isAuthenticated.set(true);
        })
      );
  }

  logout(): void {
    const refreshToken = localStorage.getItem(this.REFRESH_KEY);
    if (refreshToken) {
      this.http.post('/api/auth/logout', { refreshToken }).subscribe();
    }
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_KEY);
    localStorage.removeItem(this.USER_NAME_KEY);
    localStorage.removeItem(this.USER_EMAIL_KEY);
    this.isAuthenticated.set(false);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getUserName(): string {
    return localStorage.getItem(this.USER_NAME_KEY) ?? 'Usuario';
  }

  getUserInitial(): string {
    return this.getUserName().charAt(0).toUpperCase();
  }

  getUserEmail(): string {
    return localStorage.getItem(this.USER_EMAIL_KEY) ?? '';
  }

  private hasToken(): boolean {
    return !!localStorage.getItem(this.TOKEN_KEY);
  }
}
