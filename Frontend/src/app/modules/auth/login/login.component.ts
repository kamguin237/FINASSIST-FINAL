import { Component } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { SettingsService } from '../../../core/services/settings.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  form = this.fb.group({
    email:      ['', [Validators.required, Validators.email]],
    motDePasse: ['', Validators.required]
  });
  error = '';
  loading = false;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private settings: SettingsService
  ) {}

  submit() {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';
    this.auth.login(this.form.value as any).subscribe({
      next: () => {
        // Charger les préférences puis rediriger vers la page d'accueil configurée
        this.settings.loadFromBackend();
        const pageAccueil = this.settings.settings().pageAccueil || '/dashboard';
        this.router.navigate([pageAccueil]);
      },
      error: (e) => { this.error = e.error?.message ?? 'Erreur de connexion'; this.loading = false; }
    });
  }
}
