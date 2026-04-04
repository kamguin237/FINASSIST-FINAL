import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormArray, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { WorkflowService } from '../../core/services/workflow.service';
import { RolesService } from '../../core/services/roles.service';
import { AuthService } from '../../core/services/auth.service';
import { WorkflowCircuitDTO } from '../../core/models/workflow.models';
import { RoleDTO } from '../../core/models/role.models';
import { CustomSelectComponent, SelectOption } from '../../shared/components/custom-select/custom-select.component';

@Component({
  selector: 'app-workflow',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomSelectComponent],
  templateUrl: './workflow.component.html',
  styleUrl: './workflow.component.scss',
  encapsulation: ViewEncapsulation.None
})
export class WorkflowComponent implements OnInit {
  circuits: WorkflowCircuitDTO[] = [];
  roles: RoleDTO[] = [];
  roleOptions: SelectOption[] = [];
  selected: WorkflowCircuitDTO | null = null;
  loading = true;
  showForm = false;
  editId: number | null = null;

  form = this.fb.group({ nom: ['', Validators.required], description: [''], etapes: this.fb.array([]) });
  get etapes() { return this.form.get('etapes') as FormArray; }

  constructor(
    public auth: AuthService,
    private workflowService: WorkflowService,
    private rolesService: RolesService,
    private fb: FormBuilder,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.load();
    this.rolesService.getAll().subscribe(r => {
      this.roles = r;
      this.roleOptions = r.map(role => ({ value: role.code, label: role.code }));
    });
  }

  load() {
    this.workflowService.getCircuits().subscribe(c => { this.circuits = c; this.loading = false; });
  }

  openCreate() { this.editId = null; this.form.reset(); this.etapes.clear(); this.addEtape(); this.showForm = true; }

  openEdit(c: WorkflowCircuitDTO) {
    this.editId = c.id;
    this.form.patchValue({ nom: c.nom, description: c.description ?? '' });
    this.etapes.clear();
    c.etapes.forEach(e => this.etapes.push(this.fb.group({
      ordre: [e.ordre, Validators.required], roleRequis: [e.roleRequis, Validators.required],
      approbationRequise: [e.approbationRequise], signatureRequise: [e.signatureRequise],
      delaiMaxJours: [e.delaiMaxJours, Validators.required], estDerniereEtape: [e.estDerniereEtape]
    })));
    this.showForm = true;
    this.selected = null;
  }

  addEtape() {
    this.etapes.push(this.fb.group({
      ordre: [this.etapes.length + 1, Validators.required], roleRequis: ['', Validators.required],
      approbationRequise: [true], signatureRequise: [false],
      delaiMaxJours: [null, Validators.required], estDerniereEtape: [false]
    }));
  }

  removeEtape(i: number) { this.etapes.removeAt(i); }

  submit() {
    if (this.form.invalid) return;
    const dto = this.form.value as any;
    const isEdit = !!this.editId;
    const req = isEdit ? this.workflowService.updateCircuit(this.editId!, dto) : this.workflowService.createCircuit(dto);
    req.subscribe({
      next: () => {
        this.toastr.success(isEdit ? 'Circuit modifié.' : 'Circuit créé.');
        this.showForm = false;
        this.load();
      },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de l\'enregistrement.')
    });
  }

  delete(id: number) {
    if (!confirm('Supprimer ce circuit ?')) return;
    this.workflowService.deleteCircuit(id).subscribe({
      next: () => { this.toastr.success('Circuit supprimé.'); this.load(); },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de la suppression.')
    });
  }

  select(c: WorkflowCircuitDTO) { this.selected = this.selected?.id === c.id ? null : c; }
}
