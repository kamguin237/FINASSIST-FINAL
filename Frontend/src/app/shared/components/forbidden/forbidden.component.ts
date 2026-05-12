import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  template: `
    <div style="text-align:center;padding:4rem">
      <h1>403 — Accès refusé</h1>
      <p>Vous ne disposez pas des permissions nécessaires.</p>
      <a style="cursor:pointer;color:#a78bfa;text-decoration:underline" (click)="goHome()">Retour à l'accueil</a>
    </div>
  `
})
export class ForbiddenComponent {
  private router = inject(Router);
  private auth = inject(AuthService);

  goHome() {
    const perms = this.auth.permissions();
    // Naviguer vers la première page accessible
    if (perms.includes('DASHBOARD_CONSULTER')) {
      this.router.navigate(['/dashboard']);
    } else if (perms.includes('BESOIN_CONSULTER')) {
      this.router.navigate(['/besoins']);
    } else {
      // Déconnexion si aucune page accessible
      this.auth.logout();
    }
  }
}
