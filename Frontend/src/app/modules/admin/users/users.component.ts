import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { TranslateModule } from '@ngx-translate/core';
import { UsersService } from '../../../core/services/users.service';
import { RolesService } from '../../../core/services/roles.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { AuthService } from '../../../core/services/auth.service';
import { ConfirmService } from '../../../core/services/confirm.service';
import { UtilisateurDTO } from '../../../core/models/user.models';
import { RoleDTO, PermissionDTO } from '../../../core/models/role.models';
import { CustomSelectComponent } from '../../../shared/components/custom-select/custom-select.component';
import { RoleOptionsPipe } from '../../../shared/pipes/select-options.pipe';

// Validateur domaine email
function finstarEmailValidator(control: AbstractControl): ValidationErrors | null {
  const value: string = control.value ?? '';
  return value.toLowerCase().endsWith('@finstar-cm.com') ? null : { finstarDomain: true };
}

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomSelectComponent, RoleOptionsPipe, TranslateModule],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export class UsersComponent implements OnInit {
  users: UtilisateurDTO[] = [];
  roles: RoleDTO[] = [];
  allPermissions: PermissionDTO[] = [];

  // Modale création/modification
  showModal = false;
  editId: number | null = null;
  saving = false;
  erreur = '';

  form = this.fb.group({
    nom:    ['', Validators.required],
    prenom: ['', Validators.required],
    email:  ['', [Validators.required, Validators.email, finstarEmailValidator]],
    roleId: [null as number | null, Validators.required]
  });

  // Flag : l'admin a modifié manuellement l'email → stopper la génération auto
  private emailManuallyEdited = false;

  private slugify(str: string): string {
    return str.trim()
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '') // supprimer accents
      .replace(/\s+/g, '-')            // espaces → tirets
      .replace(/[^a-z0-9-]/g, '-')     // caractères spéciaux → tirets
      .replace(/-+/g, '-')             // tirets multiples → un seul
      .replace(/^-|-$/g, '');          // tirets en début/fin
  }

  private genererEmail(): string {
    const prenom = this.form.get('prenom')?.value ?? '';
    const nom    = this.form.get('nom')?.value ?? '';
    if (!prenom.trim() && !nom.trim()) return '';
    return `${this.slugify(prenom)}.${this.slugify(nom)}@finstar-cm.com`;
  }

  onEmailInput() {
    const val = this.form.get('email')?.value ?? '';
    // Si l'admin efface complètement → reprendre la génération auto
    if (val === '') {
      this.emailManuallyEdited = false;
      this.form.get('email')?.setValue(this.genererEmail(), { emitEvent: false });
    } else {
      // L'admin a tapé quelque chose de différent de la valeur générée
      const generated = this.genererEmail();
      if (val !== generated) this.emailManuallyEdited = true;
    }
  }

  onNomOrPrenomChange() {
    if (!this.emailManuallyEdited) {
      this.form.get('email')?.setValue(this.genererEmail(), { emitEvent: false });
    }
  }

  // Modale permissions
  showPermModal = false;
  permUser: UtilisateurDTO | null = null;
  permDirectes: Set<number> = new Set();
  permRole: Set<number> = new Set();
  savingPerms = false;

  constructor(
    public auth: AuthService,
    private usersService: UsersService,
    private rolesService: RolesService,
    private permissionsService: PermissionsService,
    private fb: FormBuilder,
    private toastr: ToastrService,
    private confirm: ConfirmService
  ) {}

  ngOnInit() {
    this.usersService.getAll().subscribe(u => this.users = u);
    this.rolesService.getAll().subscribe(r => this.roles = r);
    this.permissionsService.getAll().subscribe(p => this.allPermissions = p);
  }

  // ── Groupement par module ──────────────────────────────────────────────────
  get permissionsByModule(): { module: string; permissions: PermissionDTO[] }[] {
    const map = new Map<string, PermissionDTO[]>();
    for (const p of this.allPermissions) {
      const mod = p.module ?? 'Autres';
      if (!map.has(mod)) map.set(mod, []);
      map.get(mod)!.push(p);
    }
    return Array.from(map.entries()).map(([module, permissions]) => ({ module, permissions }));
  }

  // ── Modale permissions ─────────────────────────────────────────────────────
  openPermissions(u: UtilisateurDTO) {
    this.permUser = u;
    this.permDirectes = new Set();
    this.permRole = new Set();
    this.showPermModal = true;

    this.usersService.getPermissions(u.id).subscribe(data => {
      this.permDirectes = new Set(
        this.allPermissions.filter(p => data.permissionsDirectes.includes(p.code)).map(p => p.id)
      );
      this.permRole = new Set(
        this.allPermissions.filter(p => data.permissionsRole.includes(p.code)).map(p => p.id)
      );
    });
  }

  fermerPermModal() { this.showPermModal = false; this.permUser = null; }

  isDirecte(permId: number): boolean { return this.permDirectes.has(permId); }
  isFromRole(permId: number): boolean { return this.permRole.has(permId); }

  toggleDirecte(permId: number, event: Event) {
    const checked = (event.target as HTMLInputElement).checked;
    if (checked) this.permDirectes.add(permId);
    else this.permDirectes.delete(permId);
  }

  isModuleAllChecked(permissions: PermissionDTO[]): boolean {
    return permissions
      .filter(p => !this.isFromRole(p.id))
      .every(p => this.permDirectes.has(p.id));
  }

  toggleModule(permissions: PermissionDTO[]) {
    const editables = permissions.filter(p => !this.isFromRole(p.id));
    if (this.isModuleAllChecked(permissions))
      editables.forEach(p => this.permDirectes.delete(p.id));
    else
      editables.forEach(p => this.permDirectes.add(p.id));
  }

  sauvegarderPermissions() {
    if (!this.permUser) return;
    this.savingPerms = true;
    const ids = Array.from(this.permDirectes);
    this.usersService.setPermissions(this.permUser.id, { permissionIds: ids }).subscribe({
      next: () => {
        this.toastr.success('Permissions mises à jour.');
        this.fermerPermModal();
        this.savingPerms = false;
      },
      error: e => {
        this.toastr.error(e.error?.message ?? 'Erreur lors de la mise à jour.');
        this.savingPerms = false;
      }
    });
  }

  // ── Modale création/modification ───────────────────────────────────────────
  openCreate() {
    this.editId = null; this.erreur = ''; this.form.reset();
    this.emailManuallyEdited = false;
    this.showModal = true;
  }

  openEdit(u: UtilisateurDTO) {
    this.editId = u.id; this.erreur = '';
    this.form.patchValue({ nom: u.nom, prenom: u.prenom, email: u.email });
    this.showModal = true;
  }

  fermerModal() { this.showModal = false; this.saving = false; this.erreur = ''; this.form.reset(); }

  submit() {
    if (this.form.invalid) return;
    this.saving = true; this.erreur = '';
    const dto = this.form.value as any;
    const isEdit = !!this.editId;
    const req = isEdit ? this.usersService.update(this.editId!, dto) : this.usersService.create(dto);
    req.subscribe({
      next: (res: any) => {
        if (!isEdit && res?.emailWarning) {
          this.toastr.warning(res.emailWarning, 'Avertissement email', { timeOut: 8000 });
        } else {
          this.toastr.success(isEdit ? 'Utilisateur modifié.' : 'Utilisateur créé. Un email avec le mot de passe a été envoyé.');
        }
        this.fermerModal();
        this.usersService.getAll().subscribe(u => this.users = u);
      },
      error: e => { this.erreur = e.error?.message ?? 'Une erreur est survenue.'; this.toastr.error(this.erreur); this.saving = false; }
    });
  }

  async deactivate(id: number) {
    const ok = await this.confirm.confirm({ titre: 'Désactiver l\'utilisateur', message: 'Êtes-vous sûr de vouloir désactiver ce compte ?', labelConfirm: 'Désactiver', danger: true });
    if (!ok) return;
    this.usersService.deactivate(id).subscribe({
      next: () => { this.toastr.success('Compte désactivé.'); this.usersService.getAll().subscribe(u => this.users = u); },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de la désactivation.')
    });
  }

  async activate(id: number) {
    const ok = await this.confirm.confirm({ titre: 'Réactiver l\'utilisateur', message: 'Êtes-vous sûr de vouloir réactiver ce compte ?', labelConfirm: 'Réactiver', danger: false });
    if (!ok) return;
    this.usersService.activate(id).subscribe({
      next: () => { this.toastr.success('Compte réactivé.'); this.usersService.getAll().subscribe(u => this.users = u); },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de la réactivation.')
    });
  }

  async deletePermanent(id: number) {
    const ok = await this.confirm.confirm({ titre: 'Suppression définitive', message: '⚠ Cette action est irréversible. Supprimer définitivement cet utilisateur ?', labelConfirm: 'Supprimer', danger: true });
    if (!ok) return;
    this.usersService.deletePermanent(id).subscribe({
      next: () => { this.toastr.success('Utilisateur supprimé définitivement.'); this.usersService.getAll().subscribe(u => this.users = u); },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de la suppression.')
    });
  }
}
