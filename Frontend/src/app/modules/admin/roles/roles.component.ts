import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { RolesService } from '../../../core/services/roles.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { AuthService } from '../../../core/services/auth.service';
import { ConfirmService } from '../../../core/services/confirm.service';
import { RoleDTO, PermissionDTO } from '../../../core/models/role.models';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './roles.component.html',
  styleUrl: './roles.component.scss'
})
export class RolesComponent implements OnInit {
  roles: RoleDTO[] = [];
  allPermissions: PermissionDTO[] = [];
  showModal = false;
  editId: number | null = null;
  saving = false;
  erreur = '';

  // Modale permissions
  showPermModal = false;
  permRole: RoleDTO | null = null;
  rolePermissions: Set<number> = new Set();
  savingPerms = false;

  form = this.fb.group({ code: ['', Validators.required], description: [''] });

  constructor(
    public auth: AuthService,
    private rolesService: RolesService,
    private permissionsService: PermissionsService,
    private fb: FormBuilder,
    private toastr: ToastrService,
    private confirm: ConfirmService,
    private translate: TranslateService
  ) {}

  ngOnInit() {
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
  openPermissions(r: RoleDTO) {
    this.permRole = r;
    this.rolePermissions = new Set();
    this.showPermModal = true;
    this.rolesService.getPermissions(r.id).subscribe(perms => {
      this.rolePermissions = new Set(perms.map(p => p.id));
    });
  }

  fermerPermModal() { this.showPermModal = false; this.permRole = null; }

  hasPerm(permId: number): boolean { return this.rolePermissions.has(permId); }

  togglePerm(permId: number, event: Event) {
    const checked = (event.target as HTMLInputElement).checked;
    if (checked) this.rolePermissions.add(permId);
    else this.rolePermissions.delete(permId);
  }

  isModuleAllChecked(permissions: PermissionDTO[]): boolean {
    return permissions.every(p => this.rolePermissions.has(p.id));
  }

  toggleModule(permissions: PermissionDTO[]) {
    if (this.isModuleAllChecked(permissions))
      permissions.forEach(p => this.rolePermissions.delete(p.id));
    else
      permissions.forEach(p => this.rolePermissions.add(p.id));
  }

  sauvegarderPermissions() {
    if (!this.permRole) return;
    this.savingPerms = true;
    const ids = Array.from(this.rolePermissions);
    this.rolesService.setPermissions(this.permRole.id, { permissionIds: ids }).subscribe({
      next: () => {
        this.toastr.success('Permissions du rôle mises à jour.');
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
  openCreate() { this.editId = null; this.erreur = ''; this.form.reset(); this.showModal = true; }

  openEdit(r: RoleDTO) {
    this.editId = r.id;
    this.erreur = '';
    this.form.patchValue({ code: r.code, description: r.description ?? '' });
    this.showModal = true;
  }

  fermerModal() { this.showModal = false; this.saving = false; this.erreur = ''; this.form.reset(); }

  submit() {
    if (this.form.invalid) return;
    this.saving = true;
    this.erreur = '';
    const dto = this.form.value as any;
    const isEdit = !!this.editId;
    const req = isEdit ? this.rolesService.update(this.editId!, dto) : this.rolesService.create(dto);
    req.subscribe({
      next: () => {
        this.toastr.success(isEdit ? 'Rôle modifié avec succès.' : 'Rôle créé avec succès.');
        this.fermerModal();
        this.rolesService.getAll().subscribe(r => this.roles = r);
      },
      error: e => {
        this.erreur = e.error?.message ?? 'Une erreur est survenue.';
        this.toastr.error(this.erreur);
        this.saving = false;
      }
    });
  }

  async delete(id: number) {
    const ok = await this.confirm.confirm({ titre: this.translate.instant('roles.deleteTitle'), message: this.translate.instant('roles.deleteConfirm'), labelConfirm: this.translate.instant('common.delete'), danger: true });
    if (!ok) return;
    this.rolesService.delete(id).subscribe({
      next: () => { this.toastr.success('Rôle supprimé.'); this.rolesService.getAll().subscribe(r => this.roles = r); },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de la suppression.')
    });
  }
}
