import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

function detectOS(): string {
  const ua = navigator.userAgent;

  // Détection précise depuis le User-Agent (fonctionne pour toutes versions sauf Win11)
  if (ua.includes('Windows')) {
    if (ua.includes('Windows NT 10.0')) return 'Windows 10/11';
    if (ua.includes('Windows NT 6.3'))  return 'Windows 8.1';
    if (ua.includes('Windows NT 6.2'))  return 'Windows 8';
    if (ua.includes('Windows NT 6.1'))  return 'Windows 7';
    if (ua.includes('Windows NT 6.0'))  return 'Windows Vista';
    if (ua.includes('Windows NT 5.1') || ua.includes('Windows NT 5.2')) return 'Windows XP';
    return 'Windows';
  }
  if (ua.includes('Mac OS X')) return 'macOS';
  if (ua.includes('Android'))  return 'Android';
  if (ua.includes('iPhone') || ua.includes('iPad')) return 'iOS';
  if (ua.includes('Linux'))    return 'Linux';
  return 'Inconnu';
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.getToken();

  const headers: Record<string, string> = {};
  if (token) headers['Authorization'] = `Bearer ${token}`;
  headers['X-Client-OS'] = detectOS();
  // Bypass ngrok interstitial page pour les appels API
  headers['ngrok-skip-browser-warning'] = 'true';

  req = req.clone({ setHeaders: headers });
  return next(req);
};
