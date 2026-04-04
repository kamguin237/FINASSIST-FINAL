import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { UsersService } from '../../../core/services/users.service';
import { RolesService } from '../../../core/services/roles.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { AuthService } from '../../../core/services/auth.service';
import { UtilisateurDTO } from '../../../core/models/user.models';
import { RoleDTO, PermissionDTO } from '../../../core/models/role.models';
import { CustomSelectComponent } from '../../../shared/components/custom-select/custom-select.component';
import { RoleOptionsPipe } from '../../../shared/pipes/select-options.pipe';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomSelectComponent, RoleOptionsPipe],
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
    nom:        ['', Validators.required],
    prenom:     ['', Validators.required],
    email:      ['', [Validators.required, Validators.email]],
    motDePasse: [''],
    roleId:     [null as number | null, Validators.required]
  });

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
    private toastr: ToastrService
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
    this.form.get('motDePasse')?.setValidators(Validators.required);
    this.form.get('motDePasse')?.updateValueAndValidity();
    this.showModal = true;
  }

  openEdit(u: UtilisateurDTO) {
    this.editId = u.id; this.erreur = '';
    this.form.get('motDePasse')?.clearValidators();
    this.form.get('motDePasse')?.updateValueAndValidity();
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
      next: () => {
        this.toastr.success(isEdit ? 'Utilisateur modifié.' : 'Utilisateur créé.');
        this.fermerModal();
        this.usersService.getAll().subscribe(u => this.users = u);
      },
      error: e => { this.erreur = e.error?.message ?? 'Une erreur est survenue.'; this.toastr.error(this.erreur); this.saving = false; }
    });
  }

  deactivate(id: number) {
    if (!confirm('Désactiver cet utilisateur ?')) return;
    this.usersService.deactivate(id).subscribe({
      next: () => { this.toastr.success('Compte désactivé.'); this.usersService.getAll().subscribe(u => this.users = u); },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de la désactivation.')
    });
  }

  activate(id: number) {
    if (!confirm('Réactiver cet utilisateur ?')) return;
    this.usersService.activate(id).subscribe({
      next: () => { this.toastr.success('Compte réactivé.'); this.usersService.getAll().subscribe(u => this.users = u); },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de la réactivation.')
    });
  }

  deletePermanent(id: number) {
    if (!confirm('⚠ Supprimer définitivement cet utilisateur ? Cette action est irréversible.')) return;
    this.usersService.deletePermanent(id).subscribe({
      next: () => { this.toastr.success('Utilisateur supprimé définitivement.'); this.usersService.getAll().subscribe(u => this.users = u); },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de la suppression.')
    });
  }
}
