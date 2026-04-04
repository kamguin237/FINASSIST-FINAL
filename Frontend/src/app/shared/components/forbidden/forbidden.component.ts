import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div style="text-align:center;padding:4rem">
      <h1>403 — Accès refusé</h1>
      <p>Vous ne disposez pas des permissions nécessaires.</p>
      <a routerLink="/">Retour à l'accueil</a>
    </div>
  `
})
export class ForbiddenComponent {}
