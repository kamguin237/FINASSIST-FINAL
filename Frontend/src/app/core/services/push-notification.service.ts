import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class PushNotificationService {
  private url = `${environment.apiUrl}/push`;
  private swReg: ServiceWorkerRegistration | null = null;

  constructor(private http: HttpClient) {}

  async init() {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) return;

    try {
      this.swReg = await navigator.serviceWorker.register('/sw.js');
      console.log('[Push] Service Worker enregistré');
    } catch (e) {
      console.warn('[Push] Échec enregistrement SW:', e);
    }
  }

  async subscribe(): Promise<boolean> {
    if (!this.swReg) await this.init();
    if (!this.swReg) return false;

    const permission = await Notification.requestPermission();
    if (permission !== 'granted') return false;

    try {
      // Récupérer la clé publique VAPID
      const { publicKey } = await this.http.get<{ publicKey: string }>(`${this.url}/vapid-public-key`).toPromise() as any;

      const sub = await this.swReg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: this.urlBase64ToUint8Array(publicKey)
      });

      const json = sub.toJSON();
      await this.http.post(`${this.url}/subscribe`, {
        endpoint: json.endpoint,
        p256dh:   (json.keys as any)?.p256dh,
        auth:     (json.keys as any)?.auth
      }).toPromise();

      console.log('[Push] Abonnement enregistré');
      return true;
    } catch (e) {
      console.warn('[Push] Échec abonnement:', e);
      return false;
    }
  }

  async unsubscribe() {
    if (!this.swReg) return;
    const sub = await this.swReg.pushManager.getSubscription();
    if (!sub) return;
    await this.http.delete(`${this.url}/unsubscribe`, { body: { endpoint: sub.endpoint } }).toPromise();
    await sub.unsubscribe();
  }

  async isSubscribed(): Promise<boolean> {
    if (!this.swReg) return false;
    const sub = await this.swReg.pushManager.getSubscription();
    return !!sub;
  }

  private urlBase64ToUint8Array(base64String: string): Uint8Array {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const raw = window.atob(base64);
    return Uint8Array.from([...raw].map(c => c.charCodeAt(0)));
  }
}
