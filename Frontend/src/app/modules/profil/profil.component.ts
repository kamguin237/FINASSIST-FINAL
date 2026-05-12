import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/services/auth.service';
import { UsersService } from '../../core/services/users.service';
import { UtilisateurDTO } from '../../core/models/user.models';

function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  const nouveau = control.get('nouveauMotDePasse')?.value;
  const confirm = control.get('confirmerMotDePasse')?.value;
  return nouveau && confirm && nouveau !== confirm ? { mismatch: true } : null;
}

@Component({
  selector: 'app-profil',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './profil.component.html',
  styleUrl: './profil.component.scss'
})
export class ProfilComponent implements OnInit {
  profil: UtilisateurDTO | null = null;
  loading = true;
  pwdLoading = false;

  showAncien  = false;
  showNouveau = false;
  showConfirm = false;

  pwdForm = this.fb.group({
    ancienMotDePasse:    ['', Validators.required],
    nouveauMotDePasse:   ['', [Validators.required, Validators.minLength(8)]],
    confirmerMotDePasse: ['', Validators.required]
  }, { validators: passwordsMatch });

  constructor(
    private auth: AuthService,
    private usersService: UsersService,
    private fb: FormBuilder,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.usersService.getMe().subscribe({
      next: u => { this.profil = u; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  get initiales(): string {
    if (!this.profil) return '?';
    return `${this.profil.prenom?.[0] ?? ''}${this.profil.nom?.[0] ?? ''}`.toUpperCase();
  }

  get avatarColor(): string {
    const colors: Record<string, string> = {
      'Administrateur': '#7c3aed',
      'Direction':      '#0d9488',
      'Responsable':    '#2563eb',
      'Agent':          '#7c3aed',
    };
    return colors[this.profil?.role ?? ''] ?? '#7c3aed';
  }

  // ── Jauge de force du mot de passe ────────────────────────────────────────
  get strengthScore(): number {
    const pwd = this.pwdForm.get('nouveauMotDePasse')?.value ?? '';
    if (!pwd) return 0;
    let score = 0;
    if (pwd.length >= 8)  score++;
    if (pwd.length >= 12) score++;
    if (/[A-Z]/.test(pwd)) score++;
    if (/[0-9]/.test(pwd)) score++;
    if (/[^A-Za-z0-9]/.test(pwd)) score++;
    return score;
  }

  get strengthLabel(): string {
    const s = this.strengthScore;
    if (s <= 1) return 'profile.strengthWeak';
    if (s <= 3) return 'profile.strengthMedium';
    return 'profile.strengthStrong';
  }

  get strengthClass(): string {
    const s = this.strengthScore;
    if (s <= 1) return 'weak';
    if (s <= 3) return 'medium';
    return 'strong';
  }

  get strengthWidth(): string {
    return `${(this.strengthScore / 5) * 100}%`;
  }

  changerMotDePasse() {
    if (this.pwdForm.invalid) return;
    this.pwdLoading = true;
    const { ancienMotDePasse, nouveauMotDePasse } = this.pwdForm.value;
    this.usersService.changePassword({ ancienMotDePasse: ancienMotDePasse!, nouveauMotDePasse: nouveauMotDePasse! }).subscribe({
      next: res => {
        this.toastr.success(res.message ?? 'Mot de passe modifié.');
        this.pwdForm.reset();
        this.pwdLoading = false;
      },
      error: err => {
        this.toastr.error(err.error?.message ?? 'Erreur lors du changement de mot de passe.');
        this.pwdLoading = false;
      }
    });
  }
}
