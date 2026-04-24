import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { tap, catchError, of } from 'rxjs';

export interface UserSettings {
  notifApp: boolean;
  notifEmail: boolean;
  alertNouveauBesoin: boolean;
  alertValidation: boolean;
  alertEnAttente: boolean;
  langue: string;
  formatDate: string;
  fuseauHoraire: string;
  itemsParPage: number;
  pageAccueil: string;
  triDefaut: string;
  // Session
  deconnexionAuto: boolean;
  inactiviteMinutes: number;
  avertissementSecondes: number;
}

const DEFAULTS: UserSettings = {
  notifApp: true,
  notifEmail: false,
  alertNouveauBesoin: true,
  alertValidation: true,
  alertEnAttente: true,
  langue: 'fr',
  formatDate: 'dd/MM/yyyy',
  fuseauHoraire: 'Africa/Douala',
  itemsParPage: 20,
  pageAccueil: '/dashboard',
  triDefaut: 'dateDesc',
  deconnexionAuto: true,
  inactiviteMinutes: 30,
  avertissementSecondes: 30,
};

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly KEY = 'finassist_settings';
  private readonly url = `${environment.apiUrl}/settings`;

  settings = signal<UserSettings>({ ...DEFAULTS });

  constructor(private http: HttpClient) {
    // Charger depuis le cache local immédiatement, puis synchroniser avec le backend
    this.loadFromLocal();
    this.loadFromBackend();
  }

  private loadFromLocal() {
    try {
      const raw = localStorage.getItem(this.KEY);
      if (raw) this.settings.set({ ...DEFAULTS, ...JSON.parse(raw) });
    } catch { /* silencieux */ }
  }

  loadFromBackend() {
    return this.http.get<UserSettings>(this.url).pipe(
      tap(prefs => {
        const merged = { ...DEFAULTS, ...prefs };
        this.settings.set(merged);
        localStorage.setItem(this.KEY, JSON.stringify(merged));
      }),
      catchError(() => of(null))
    ).subscribe();
  }

  save(partial: Partial<UserSettings>) {
    const updated = { ...this.settings(), ...partial };
    this.settings.set(updated);
    localStorage.setItem(this.KEY, JSON.stringify(updated));

    // Persister au backend
    this.http.put<UserSettings>(this.url, updated).pipe(
      catchError(() => of(null))
    ).subscribe();
  }

  reset() {
    this.settings.set({ ...DEFAULTS });
    localStorage.removeItem(this.KEY);
    this.http.put<UserSettings>(this.url, DEFAULTS).pipe(
      catchError(() => of(null))
    ).subscribe();
  }
}
