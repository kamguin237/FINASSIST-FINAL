import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { BesoinsService } from '../../../core/services/besoins.service';
import { CategoriesService } from '../../../core/services/categories.service';
import { AuthService } from '../../../core/services/auth.service';
import { BesoinDTO, NIVEAUX_IMPORTANCE } from '../../../core/models/besoin.models';
import { CategorieDTO, CategorieDetailDTO } from '../../../core/models/categorie.models';
import { CustomSelectComponent } from '../../../shared/components/custom-select/custom-select.component';
import { NiveauOptionsPipe, CategorieOptionsPipe } from '../../../shared/pipes/select-options.pipe';

@Component({
  selector: 'app-besoins-list',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, CustomSelectComponent, NiveauOptionsPipe, CategorieOptionsPipe],
  templateUrl: './besoins-list.component.html',
  styleUrl: './besoins-list.component.scss'
})
export class BesoinsListComponent implements OnInit {
  besoins: BesoinDTO[] = [];
  categories: CategorieDTO[] = [];
  loading = true;
  showModal = false;
  showEditModal = false;
  saving = false;
  editId: number | null = null;
  niveaux = NIVEAUX_IMPORTANCE;
  filtreStatut: string = '';

  statutOptions = [
    { value: 'BROUILLON',     label: 'Brouillon' },
    { value: 'ENREGISTRE',    label: 'Enregistré' },
    { value: 'EN_ATTENTE',    label: 'En attente' },
    { value: 'APPROUVE',      label: 'Approuvé' },
    { value: 'SIGNE',         label: 'Signé' },
    { value: 'REJETE',        label: 'Rejeté' },
    { value: 'TERMINE',       label: 'Terminé' },
  ];

  get besoinsFiltres(): BesoinDTO[] {
    if (!this.filtreStatut) return this.besoins;
    return this.besoins.filter(b => {
      switch (this.filtreStatut) {
        case 'EN_ATTENTE': return b.statut.startsWith('EN_ATTENTE_ROLE') || b.statut === 'EN_ATTENTE';
        case 'APPROUVE':   return b.statut.startsWith('APPROUVE_ROLE');
        case 'SIGNE':      return b.statut.startsWith('SIGNE_ROLE');
        case 'REJETE':     return b.statut.startsWith('REJETE_ROLE');
        default:           return b.statut === this.filtreStatut;
      }
    });
  }

  onFiltreStatutChange(val: string | null) {
    this.filtreStatut = val ?? '';
  }

  reinitialiserFiltre() {
    this.filtreStatut = '';
  }

  form = this.fb.group({
    titre:            ['', Validators.required],
    description:      ['', Validators.required],
    niveauImportance: ['', Validators.required],
    categorieId:      [null as number | null, Validators.required]
  });

  editForm = this.fb.group({
    titre:            ['', Validators.required],
    description:      ['', Validators.required],
    niveauImportance: ['', Validators.required],
    categorieId:      [null as number | null, Validators.required]
  });

  constructor(
    public auth: AuthService,
    private besoinsService: BesoinsService,
    private categoriesService: CategoriesService,
    private fb: FormBuilder,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.load();
    this.categoriesService.getAll().subscribe(c => this.categories = c);
  }

  load() {
    this.besoinsService.getAll().subscribe({
      next: b => { this.besoins = b; this.loading = false; },
      error: () => this.loading = false
    });
  }

  openCreate() {
    this.form.reset();
    this.showModal = true;
  }

  fermerModal() {
    this.showModal = false;
    this.saving = false;
    this.form.reset();
  }

  openEdit(b: BesoinDTO) {
    this.editId = b.id;
    this.editForm.patchValue({
      titre: b.titre,
      description: b.description,
      niveauImportance: b.niveauImportance,
      categorieId: b.categorieId
    });
    this.showEditModal = true;
  }

  fermerEditModal() {
    this.showEditModal = false;
    this.saving = false;
    this.editId = null;
    this.editForm.reset();
  }

  submitEdit() {
    if (this.editForm.invalid || !this.editId) return;
    this.saving = true;
    this.besoinsService.update(this.editId, this.editForm.value as any).subscribe({
      next: updated => {
        this.toastr.success('Besoin modifié avec succès.');
        this.besoins = this.besoins.map(b => b.id === updated.id ? updated : b);
        this.fermerEditModal();
      },
      error: e => {
        this.toastr.error(e.error?.message ?? 'Erreur lors de la modification.');
        this.saving = false;
      }
    });
  }

  submit() {
    if (this.form.invalid) return;
    this.saving = true;
    this.besoinsService.create(this.form.value as any).subscribe({
      next: created => {
        this.toastr.success('Besoin créé avec succès.');
        this.fermerModal();
        this.besoins = [created, ...this.besoins];
      },
      error: e => {
        this.toastr.error(e.error?.message ?? 'Erreur lors de la création.');
        this.saving = false;
      }
    });
  }

  // ── Modale circuit ───────────────────────────────────────────────────────
  showCircuitModal = false;
  circuitLoading = false;
  circuitBesoin: BesoinDTO | null = null;
  circuitDetail: CategorieDetailDTO | null = null;

  ouvrirCircuit(b: BesoinDTO) {
    this.circuitBesoin = b;
    this.circuitDetail = null;
    this.showCircuitModal = true;
    this.circuitLoading = true;
    this.categoriesService.getById(b.categorieId).subscribe({
      next: d => { this.circuitDetail = d; this.circuitLoading = false; },
      error: () => { this.circuitLoading = false; this.toastr.error('Impossible de charger le circuit.'); }
    });
  }

  fermerCircuit() { this.showCircuitModal = false; this.circuitBesoin = null; this.circuitDetail = null; }

  etapeStatut(ordre: number, besoinStatut: string): 'active' | 'done' | 'pending' {
    const match = besoinStatut.match(/(\d+)$/);
    const courant = match ? +match[1] : 0;
    if (besoinStatut === 'TERMINE') return 'done';
    if (ordre < courant) return 'done';
    if (ordre === courant) return 'active';
    return 'pending';
  }

  delete(id: number) {
    if (!confirm('Supprimer ce besoin ?')) return;
    this.besoinsService.delete(id).subscribe({
      next: () => {
        this.toastr.success('Besoin supprimé.');
        this.besoins = this.besoins.filter(b => b.id !== id);
      },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de la suppression.')
    });
  }

  peutSupprimer(b: BesoinDTO): boolean {
    return b.statut === 'BROUILLON' ||
           b.statut === 'ENREGISTRE' ||
           b.statut.startsWith('REJETE_ROLE') ||
           b.estTermine;
  }
}
