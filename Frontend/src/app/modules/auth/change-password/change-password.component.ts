import { Component } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './change-password.component.html',
  styleUrl: './change-password.component.scss'
})
export class ChangePasswordComponent {
  form = this.fb.group({
    ancienMotDePasse: ['', Validators.required],
    nouveauMotDePasse: ['', [Validators.required, Validators.minLength(8)]],
    confirmation: ['', Validators.required]
  }, { validators: this.passwordsMatch });

  error = '';
  loading = false;
  showAncien = false;
  showNouveau = false;
  showConfirm = false;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private auth: AuthService,
    private router: Router
  ) {}

  private passwordsMatch(group: AbstractControl) {
    const pwd = group.get('nouveauMotDePasse')?.value;
    const confirm = group.get('confirmation')?.value;
    return pwd === confirm ? null : { mismatch: true };
  }

  submit() {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';
    const { ancienMotDePasse, nouveauMotDePasse } = this.form.value;
    this.http.post(`${environment.apiUrl}/auth/change-password`, { ancienMotDePasse, nouveauMotDePasse }).subscribe({
      next: () => {
        this.auth.clearMustChangePassword();
        this.router.navigate(['/dashboard']);
      },
      error: e => { this.error = e.error?.message ?? 'Erreur lors du changement.'; this.loading = false; }
    });
  }
}
