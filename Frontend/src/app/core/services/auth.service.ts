import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { LoginRequest, LoginResponse, UtilisateurInfo } from '../models/auth.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'finassist_token';
  private readonly USER_KEY  = 'finassist_user';
  private readonly MDP_KEY   = 'finassist_must_change_pwd';

  currentUser = signal<UtilisateurInfo | null>(this.loadUser());
  permissions = signal<string[]>(this.loadPermissions());

  constructor(private http: HttpClient, private router: Router) {}

  login(credentials: LoginRequest) {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, credentials).pipe(
      tap(res => {
        localStorage.setItem(this.TOKEN_KEY, res.accessToken);
        localStorage.setItem(this.USER_KEY, JSON.stringify(res.utilisateur));
        localStorage.setItem(this.MDP_KEY, String(res.doitChangerMotDePasse));
        this.currentUser.set(res.utilisateur);
        this.permissions.set(this.parsePermissions(res.accessToken));
      })
    );
  }

  mustChangePassword(): boolean {
    return localStorage.getItem(this.MDP_KEY) === 'true';
  }

  clearMustChangePassword() {
    localStorage.removeItem(this.MDP_KEY);
  }

  logout() {
    this.http.post(`${environment.apiUrl}/auth/logout`, {}).subscribe();
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    localStorage.removeItem(this.MDP_KEY);
    this.currentUser.set(null);
    this.permissions.set([]);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }

  hasPermission(code: string): boolean {
    return this.permissions().includes(code);
  }

  private parsePermissions(token: string): string[] {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const perms: string = payload['permissions'] ?? '';
      return perms ? perms.split(',').map((p: string) => p.trim()) : [];
    } catch {
      return [];
    }
  }

  private loadUser(): UtilisateurInfo | null {
    try {
      const raw = localStorage.getItem(this.USER_KEY);
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }

  private loadPermissions(): string[] {
    const token = localStorage.getItem(this.TOKEN_KEY);
    return token ? this.parsePermissions(token) : [];
  }
}
