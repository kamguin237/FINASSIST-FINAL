import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { PermissionsService } from '../../../core/services/permissions.service';
import { AuthService } from '../../../core/services/auth.service';
import { PermissionDTO } from '../../../core/models/role.models';

@Component({
  selector: 'app-permissions',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './permissions.component.html',
  styleUrl: './permissions.component.scss'
})
export class PermissionsComponent implements OnInit {
  permissions: PermissionDTO[] = [];
  editPerm: PermissionDTO | null = null;

  form = this.fb.group({ code: [''], description: [''], fonctionnalite: [''], module: [''] });

  constructor(public auth: AuthService, private permissionsService: PermissionsService, private fb: FormBuilder) {}

  ngOnInit() {
    this.permissionsService.getAll().subscribe(p => this.permissions = p);
  }

  openEdit(p: PermissionDTO) {
    this.editPerm = p;
    this.form.patchValue(p);
  }

  submit() {
    this.permissionsService.update(this.editPerm!.id, this.form.value as any).subscribe(updated => {
      const idx = this.permissions.findIndex(p => p.id === updated.id);
      if (idx >= 0) this.permissions[idx] = updated;
      this.editPerm = null;
    });
  }
}
