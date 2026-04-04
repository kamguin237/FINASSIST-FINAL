import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { RolesService } from '../../../core/services/roles.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { AuthService } from '../../../core/services/auth.service';
import { RoleDTO, PermissionDTO } from '../../../core/models/role.models';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './roles.component.html',
  styleUrl: './roles.component.scss'
})
export class RolesComponent implements OnInit {
  roles: RoleDTO[] = [];
  allPermissions: PermissionDTO[] = [];
  rolePermissions: number[] = [];
  selectedRole: RoleDTO | null = null;
  showModal = false;
  editId: number | null = null;
  saving = false;
  erreur = '';

  form = this.fb.group({ code: ['', Validators.required], description: [''] });

  constructor(
    public auth: AuthService,
    private rolesService: RolesService,
    private permissionsService: PermissionsService,
    private fb: FormBuilder,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.rolesService.getAll().subscribe(r => this.roles = r);
    this.permissionsService.getAll().subscribe(p => this.allPermissions = p);
  }

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

  delete(id: number) {
    if (!confirm('Supprimer ce rôle ?')) return;
    this.rolesService.delete(id).subscribe({
      next: () => {
        this.toastr.success('Rôle supprimé.');
        this.rolesService.getAll().subscribe(r => this.roles = r);
      },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de la suppression.')
    });
  }

  togglePermissions(r: RoleDTO) {
    if (this.selectedRole?.id === r.id) { this.selectedRole = null; return; }
    this.selectedRole = r;
    this.rolesService.getPermissions(r.id).subscribe(p => this.rolePermissions = p.map(x => x.id));
  }

  hasPermission(permId: number) { return this.rolePermissions.includes(permId); }

  togglePerm(roleId: number, permId: number, event: Event) {
    const checked = (event.target as HTMLInputElement).checked;
    if (checked) {
      this.rolesService.addPermissions(roleId, { permissionIds: [permId] }).subscribe({
        next: () => { this.rolePermissions.push(permId); this.toastr.success('Permission attribuée.'); },
        error: () => this.toastr.error('Erreur lors de l\'attribution.')
      });
    } else {
      this.rolesService.removePermission(roleId, permId).subscribe({
        next: () => { this.rolePermissions = this.rolePermissions.filter(id => id !== permId); this.toastr.info('Permission retirée.'); },
        error: () => this.toastr.error('Erreur lors du retrait.')
      });
    }
  }
}
