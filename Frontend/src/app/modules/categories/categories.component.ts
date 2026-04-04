import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { CategoriesService } from '../../core/services/categories.service';
import { WorkflowService } from '../../core/services/workflow.service';
import { AuthService } from '../../core/services/auth.service';
import { CategorieDTO } from '../../core/models/categorie.models';
import { WorkflowCircuitDTO } from '../../core/models/workflow.models';
import { CustomSelectComponent } from '../../shared/components/custom-select/custom-select.component';
import { CircuitOptionsPipe } from '../../shared/pipes/select-options.pipe';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomSelectComponent, CircuitOptionsPipe],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss'
})
export class CategoriesComponent implements OnInit {
  categories: CategorieDTO[] = [];
  circuits: WorkflowCircuitDTO[] = [];
  loading = true;
  showForm = false;
  editId: number | null = null;

  form = this.fb.group({
    nom:               ['', Validators.required],
    description:       [''],
    workflowCircuitId: [null as number | null, Validators.required]
  });

  constructor(
    public auth: AuthService,
    private categoriesService: CategoriesService,
    private workflowService: WorkflowService,
    private fb: FormBuilder,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.load();
    this.workflowService.getCircuits().subscribe(c => this.circuits = c);
  }

  load() {
    this.categoriesService.getAll().subscribe(c => { this.categories = c; this.loading = false; });
  }

  openCreate() { this.editId = null; this.form.reset(); this.showForm = true; }

  openEdit(c: CategorieDTO) {
    this.editId = c.id;
    this.form.patchValue({ nom: c.nom, description: c.description, workflowCircuitId: c.workflowCircuitId ?? null });
    this.showForm = true;
  }

  submit() {
    if (this.form.invalid) return;
    const dto = this.form.value as any;
    const isEdit = !!this.editId;
    const req = isEdit ? this.categoriesService.update(this.editId!, dto) : this.categoriesService.create(dto);
    req.subscribe({
      next: () => {
        this.toastr.success(isEdit ? 'Catégorie modifiée.' : 'Catégorie créée.');
        this.showForm = false;
        this.load();
      },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de l\'enregistrement.')
    });
  }

  delete(id: number) {
    if (!confirm('Supprimer cette catégorie ?')) return;
    this.categoriesService.delete(id).subscribe({
      next: () => { this.toastr.success('Catégorie supprimée.'); this.load(); },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de la suppression.')
    });
  }
}
