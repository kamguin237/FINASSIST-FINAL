import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export interface HistoriqueUpdate {
  besoinId: number;
  action: string;
  description: string;
  dateAction: string;
}

export interface NotificationPush {
  message: string;
  type: string;
  dateEnvoi: string;
  lu: boolean;
}

export interface BesoinStatutUpdate {
  besoinId: number;
  nouveauStatut: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection?: signalR.HubConnection;
  private historiqueUpdated$ = new Subject<HistoriqueUpdate>();
  private nouvelleNotification$ = new Subject<NotificationPush>();
  private besoinStatutUpdated$ = new Subject<BesoinStatutUpdate>();

  constructor(private auth: AuthService) {}

  get historiqueUpdates() {
    return this.historiqueUpdated$.asObservable();
  }

  get nouvellesNotifications() {
    return this.nouvelleNotification$.asObservable();
  }

  get besoinStatutUpdates() {
    return this.besoinStatutUpdated$.asObservable();
  }

  async startConnection(token?: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return; // Déjà connecté (ex: démarré par le layout), on réutilise
    }

    // Si une connexion existe mais n'est pas connectée, la stopper proprement
    if (this.hubConnection) {
      try { await this.hubConnection.stop(); } catch { /* silencieux */ }
    }

    // Utiliser le token fourni, ou le récupérer depuis AuthService
    const resolvedToken = token ?? this.auth.getToken() ?? '';

    const hubUrl = `${environment.apiUrl}/hubs/besoins`;

    const builder = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        // Passer le token JWT via query string pour l'authentification WebSocket
        accessTokenFactory: () => resolvedToken,
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning);

    this.hubConnection = builder.build();

    // Écouter les mises à jour d'historique (besoin spécifique)
    this.hubConnection.on('HistoriqueUpdated', (data: HistoriqueUpdate) => {
      this.historiqueUpdated$.next(data);
    });

    // Écouter les nouvelles notifications en temps réel (utilisateur connecté)
    this.hubConnection.on('NouvelleNotification', (data: NotificationPush) => {
      this.nouvelleNotification$.next(data);
    });

    // Écouter les mises à jour de statut des besoins (liste globale)
    this.hubConnection.on('BesoinStatutUpdated', (data: BesoinStatutUpdate) => {
      this.besoinStatutUpdated$.next(data);
    });

    // Reconnexion automatique : rejoindre à nouveau les groupes
    this.hubConnection.onreconnected(() => {
      console.log('[SignalR] Reconnecté');
    });

    try {
      await this.hubConnection.start();
      console.log('[SignalR] Connexion établie');
    } catch (err) {
      console.error('[SignalR] Erreur de connexion:', err);
      // Retry après 5 secondes
      setTimeout(() => this.startConnection(token), 5000);
    }
  }

  async joinBesoinGroup(besoinId: number): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('JoinBesoinGroup', besoinId);
        console.log(`[SignalR] Rejoint le groupe besoin_${besoinId}`);
      } catch (err) {
        console.error('[SignalR] Erreur lors du join:', err);
      }
    }
  }

  async leaveBesoinGroup(besoinId: number): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('LeaveBesoinGroup', besoinId);
        console.log(`[SignalR] Quitté le groupe besoin_${besoinId}`);
      } catch (err) {
        console.error('[SignalR] Erreur lors du leave:', err);
      }
    }
  }

  async stopConnection(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      console.log('[SignalR] Connexion fermée');
    }
  }
}
