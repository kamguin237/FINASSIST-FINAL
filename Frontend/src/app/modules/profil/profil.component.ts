import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
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
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './profil.component.html',
  styleUrl: './profil.component.scss'
})
export class ProfilComponent implements OnInit {
  profil: UtilisateurDTO | null = null;
  loading = true;
  pwdLoading = false;

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
