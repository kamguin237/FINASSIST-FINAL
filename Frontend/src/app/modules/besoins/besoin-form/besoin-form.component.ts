import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { BesoinsService } from '../../../core/services/besoins.service';
import { CategoriesService } from '../../../core/services/categories.service';
import { CategorieDTO } from '../../../core/models/categorie.models';
import { NIVEAUX_IMPORTANCE } from '../../../core/models/besoin.models';
import { CustomSelectComponent } from '../../../shared/components/custom-select/custom-select.component';
import { NiveauOptionsPipe, CategorieOptionsPipe } from '../../../shared/pipes/select-options.pipe';

@Component({
  selector: 'app-besoin-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, CustomSelectComponent, NiveauOptionsPipe, CategorieOptionsPipe],
  templateUrl: './besoin-form.component.html',
  styleUrl: './besoin-form.component.scss'
})
export class BesoinFormComponent implements OnInit {
  form = this.fb.group({
    titre:            ['', Validators.required],
    description:      ['', Validators.required],
    niveauImportance: ['', Validators.required],
    categorieId:      [null as number | null, Validators.required]
  });

  categories: CategorieDTO[] = [];
  niveaux = NIVEAUX_IMPORTANCE;
  editId: number | null = null;
  loading = false;

  constructor(
    private fb: FormBuilder,
    private besoinsService: BesoinsService,
    private categoriesService: CategoriesService,
    private router: Router,
    private route: ActivatedRoute,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.categoriesService.getAll().subscribe(c => this.categories = c);
    this.editId = this.route.snapshot.params['id'] ? +this.route.snapshot.params['id'] : null;
    if (this.editId) {
      this.besoinsService.getById(this.editId).subscribe(b => this.form.patchValue(b as any));
    }
  }

  submit() {
    if (this.form.invalid) return;
    this.loading = true;
    const dto = this.form.value as any;
    const isEdit = !!this.editId;
    const req = isEdit ? this.besoinsService.update(this.editId!, dto) : this.besoinsService.create(dto);
    req.subscribe({
      next: () => {
        this.toastr.success(isEdit ? 'Besoin modifié.' : 'Besoin créé.');
        this.router.navigate(['/besoins']);
      },
      error: e => {
        this.toastr.error(e.error?.message ?? 'Erreur lors de l\'enregistrement.');
        this.loading = false;
      }
    });
  }
}
