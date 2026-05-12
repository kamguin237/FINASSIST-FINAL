import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormArray, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { TranslateModule } from '@ngx-translate/core';
import { WorkflowService } from '../../core/services/workflow.service';
import { RolesService } from '../../core/services/roles.service';
import { AuthService } from '../../core/services/auth.service';
import { ConfirmService } from '../../core/services/confirm.service';
import { WorkflowCircuitDTO } from '../../core/models/workflow.models';
import { RoleDTO } from '../../core/models/role.models';
import { CustomSelectComponent, SelectOption } from '../../shared/components/custom-select/custom-select.component';
import { TimePickerComponent } from '../../shared/components/time-picker/time-picker.component';

@Component({
  selector: 'app-workflow',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomSelectComponent, TranslateModule, TimePickerComponent],
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
    private toastr: ToastrService,
    private confirm: ConfirmService
  ) {}

  ngOnInit() {
    this.load();
    this.rolesService.getAll().subscribe(r => {
      this.roles = r;
      this.roleOptions = r
        .filter(role => role.code !== 'Administrateur')
        .map(role => ({ value: role.code, label: role.code }));
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
      ordre: [e.ordre, Validators.required],
      roleRequis: [e.roleRequis, Validators.required],
      approbationRequise: [e.approbationRequise],
      signatureRequise: [e.signatureRequise],
      delaiMaxJours: [this.minutesToHHmm(e.delaiMaxJours), Validators.required],
      estDerniereEtape: [e.estDerniereEtape]
    })));
    this.showForm = true;
    this.selected = null;
  }

  minutesToHHmm(minutes: number): string {
    const h = Math.floor(minutes / 60).toString().padStart(2, '0');
    const m = (minutes % 60).toString().padStart(2, '0');
    return `${h}:${m}`;
  }

  private hhmmToMinutes(hhmm: string): number {
    const [h, m] = (hhmm ?? '00:00').split(':').map(Number);
    const total = (h || 0) * 60 + (m || 0);
    return total > 0 ? total : 1; // minimum 1 minute
  }

  get derniereEtapeDefinie(): boolean {
    return this.etapes.controls.some(e => e.get('estDerniereEtape')?.value === true);
  }

  get tousDelaisValides(): boolean {
    return this.etapes.controls.every(e => this.hhmmToMinutes(e.get('delaiMaxJours')?.value) > 0);
  }

  addEtape() {
    this.etapes.push(this.fb.group({
      ordre: [this.etapes.length + 1, Validators.required],
      roleRequis: ['', Validators.required],
      approbationRequise: [true],
      signatureRequise: [false],
      delaiMaxJours: ['', Validators.required],
      estDerniereEtape: [false]
    }));
  }

  removeEtape(i: number) { this.etapes.removeAt(i); }

  submit() {
    if (this.form.invalid || !this.derniereEtapeDefinie) return;
    const raw = this.form.value as any;
    const dto = {
      ...raw,
      etapes: raw.etapes.map((e: any) => ({
        ...e,
        delaiMaxJours: this.hhmmToMinutes(e.delaiMaxJours)
      }))
    };
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

  async delete(id: number) {
    const ok = await this.confirm.confirm({ titre: 'Supprimer le circuit', message: 'Êtes-vous sûr de vouloir supprimer ce circuit de validation ?', labelConfirm: 'Supprimer', danger: true });
    if (!ok) return;
    this.workflowService.deleteCircuit(id).subscribe({
      next: () => { this.toastr.success('Circuit supprimé.'); this.load(); },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de la suppression.')
    });
  }

  select(c: WorkflowCircuitDTO) { this.selected = this.selected?.id === c.id ? null : c; }
}
