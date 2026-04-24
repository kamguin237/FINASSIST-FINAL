import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';
import { SettingsService } from '../../core/services/settings.service';
import { MaSignatureService } from '../../core/services/ma-signature.service';
import { UsersService } from '../../core/services/users.service';
import { SignatureUtilisateurDTO } from '../../core/models/signature-utilisateur.models';
import { UtilisateurDTO } from '../../core/models/user.models';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent implements OnInit {
  profil: UtilisateurDTO | null = null;
  signature: SignatureUtilisateurDTO | null = null;

  langues   = [{ value: 'fr', label: '🇫🇷 Français' }, { value: 'en', label: '🇬🇧 English' }];
  formats   = [{ value: 'dd/MM/yyyy', label: 'dd/MM/yyyy' }, { value: 'MM/dd/yyyy', label: 'MM/dd/yyyy' }, { value: 'yyyy-MM-dd', label: 'yyyy-MM-dd' }];
  fuseaux   = [
    { value: 'Africa/Douala',   label: 'Africa/Douala (UTC+1)' },
    { value: 'Africa/Lagos',    label: 'Africa/Lagos (UTC+1)' },
    { value: 'Africa/Abidjan',  label: 'Africa/Abidjan (UTC+0)' },
    { value: 'Africa/Nairobi',  label: 'Africa/Nairobi (UTC+3)' },
    { value: 'Europe/Paris',    label: 'Europe/Paris (UTC+1/+2)' },
    { value: 'Europe/London',   label: 'Europe/London (UTC+0/+1)' },
    { value: 'UTC',             label: 'UTC' },
  ];
  pages     = [{ value: '/dashboard', label: 'Dashboard' }, { value: '/besoins', label: 'Besoins' }, { value: '/notifications', label: 'Notifications' }];
  tris      = [{ value: 'dateDesc', label: 'Date (récent → ancien)' }, { value: 'dateAsc', label: 'Date (ancien → récent)' }, { value: 'titre', label: 'Titre A→Z' }];

  get s() { return this.settingsService.settings(); }

  get initiales(): string {
    if (!this.profil) return '?';
    return `${this.profil.prenom?.[0] ?? ''}${this.profil.nom?.[0] ?? ''}`.toUpperCase();
  }

  constructor(
    public auth: AuthService,
    public theme: ThemeService,
    public settingsService: SettingsService,
    private maSignatureService: MaSignatureService,
    private usersService: UsersService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.usersService.getMe().subscribe({ next: u => this.profil = u, error: () => {} });
    this.maSignatureService.get().subscribe({ next: s => this.signature = s, error: () => {} });
  }

  toggle(key: keyof typeof this.s) {
    this.settingsService.save({ [key]: !this.s[key] } as any);
  }

  set(key: keyof typeof this.s, value: any) {
    this.settingsService.save({ [key]: value } as any);
    this.toastr.success('Préférence enregistrée.', '', { timeOut: 1500 });
  }

  setInactivite(val: number) {
    const v = Math.min(480, Math.max(1, +val || 30));
    this.settingsService.save({ inactiviteMinutes: v });
  }

  setAvertissement(val: number) {
    const v = Math.min(300, Math.max(10, +val || 30));
    this.settingsService.save({ avertissementSecondes: v });
  }

  sauvegarderSession() {
    this.settingsService.save({
      deconnexionAuto: this.s.deconnexionAuto,
      inactiviteMinutes: this.s.inactiviteMinutes,
      avertissementSecondes: this.s.avertissementSecondes
    });
    this.toastr.success('Paramètres de session enregistrés.');
  }

  resetAll() {
    this.settingsService.reset();
    this.toastr.info('Paramètres réinitialisés.');
  }
}
