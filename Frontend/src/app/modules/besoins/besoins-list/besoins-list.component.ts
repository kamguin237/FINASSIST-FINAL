import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { TranslateModule } from '@ngx-translate/core';
import { Subscription, forkJoin, of } from 'rxjs';
import { BesoinsService } from '../../../core/services/besoins.service';
import { CategoriesService } from '../../../core/services/categories.service';
import { AuthService } from '../../../core/services/auth.service';
import { SettingsService } from '../../../core/services/settings.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { BesoinDTO, NIVEAUX_IMPORTANCE } from '../../../core/models/besoin.models';
import { CategorieDTO, CategorieDetailDTO } from '../../../core/models/categorie.models';
import { CustomSelectComponent } from '../../../shared/components/custom-select/custom-select.component';
import { NiveauOptionsPipe, CategorieOptionsPipe } from '../../../shared/pipes/select-options.pipe';
import { ConfirmService } from '../../../core/services/confirm.service';

@Component({
  selector: 'app-besoins-list',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, CustomSelectComponent, NiveauOptionsPipe, CategorieOptionsPipe, TranslateModule],
  templateUrl: './besoins-list.component.html',
  styleUrl: './besoins-list.component.scss'
})
export class BesoinsListComponent implements OnInit, OnDestroy {
  besoins: BesoinDTO[] = [];
  categories: CategorieDTO[] = [];
  loading = true;
  showModal = false;
  showEditModal = false;
  saving = false;
  editId: number | null = null;
  niveaux = NIVEAUX_IMPORTANCE;
  filtreStatut: string = '';
  fichierSelectionne: File | null = null;
  fichierEditSelectionne: File | null = null;
  editDocuments: { id: number; nom: string; type: string }[] = [];

  private signalRSub?: Subscription;

  // Options générées dynamiquement depuis les statuts réels des besoins chargés
  get statutOptions(): { value: string; label: string }[] {
    const statutsUniques = [...new Set(this.besoins.map(b => b.statut))].sort();
    return statutsUniques.map(s => ({ value: s, label: this.labelStatut(s) }));
  }

  labelStatut(statut: string): string {
    if (statut === 'BROUILLON')  return 'Brouillon';
    if (statut === 'ENREGISTRE') return 'Enregistré';
    if (statut === 'SOUMISE')    return 'Soumis';
    if (statut === 'TRANSMIS')   return 'Transmis';
    if (statut === 'TERMINE')    return 'Terminé';
    if (statut.startsWith('EN_ATTENTE_'))  return `En attente — ${this.formatRole(statut.replace('EN_ATTENTE_', ''))}`;
    if (statut.startsWith('APPROUVE_PAR_')) return `Approuvé par ${this.formatRole(statut.replace('APPROUVE_PAR_', ''))}`;
    if (statut.startsWith('REJETE_PAR_'))   return `Rejeté par ${this.formatRole(statut.replace('REJETE_PAR_', ''))}`;
    if (statut.startsWith('SIGNE_PAR_'))    return `Signé par ${this.formatRole(statut.replace('SIGNE_PAR_', ''))}`;
    return statut;
  }

  badgeClass(statut: string): string {
    if (!statut) return 'badge statut-brouillon';
    if (statut === 'BROUILLON')             return 'badge statut-brouillon';
    if (statut === 'ENREGISTRE')            return 'badge statut-enregistre';
    if (statut === 'TRANSMIS')              return 'badge statut-transmis';
    if (statut === 'TERMINE')               return 'badge statut-termine';
    if (statut.startsWith('EN_ATTENTE_'))   return 'badge statut-attente';
    if (statut.startsWith('APPROUVE_PAR_')) return 'badge statut-approuve';
    if (statut.startsWith('REJETE_PAR_'))   return 'badge statut-rejete';
    if (statut.startsWith('SIGNE_PAR_'))    return 'badge statut-signe';
    return 'badge';
  }

  private formatRole(roleCode: string): string {
    const map: Record<string, string> = {
      'RESPONSABLE':       'le Responsable',
      'DIRECTION':         'la Direction',
      'DIRECTIONGENERALE': 'la Direction Générale',
      'ADMINISTRATEUR':    "l'Administrateur",
      'AGENT':             "l'Agent",
    };
    return map[roleCode] ?? roleCode.charAt(0) + roleCode.slice(1).toLowerCase();
  }

  get besoinsFiltres(): BesoinDTO[] {
    if (!this.filtreStatut) return this.besoins;
    return this.besoins.filter(b => b.statut === this.filtreStatut);
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
    private toastr: ToastrService,
    private confirm: ConfirmService,
    private settings: SettingsService,
    private signalR: SignalRService
  ) {}

  ngOnInit() {
    this.load();
    // Charger uniquement les catégories dont le circuit ne contient pas le rôle de l'utilisateur
    this.categoriesService.getDisponibles().subscribe(c => this.categories = c);

    // S'abonner aux mises à jour de statut en temps réel via SignalR
    this.signalRSub = this.signalR.besoinStatutUpdates.subscribe(update => {
      const besoin = this.besoins.find(b => b.id === update.besoinId);
      if (besoin) {
        besoin.statut = update.nouveauStatut;
      }
    });
  }

  ngOnDestroy() {
    this.signalRSub?.unsubscribe();
  }

  load() {
    this.besoinsService.getAll().subscribe({
      next: b => {
        this.besoins = this.appliquerTri(b);
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  private appliquerTri(besoins: BesoinDTO[]): BesoinDTO[] {
    const tri = this.settings.settings().triDefaut;
    switch (tri) {
      case 'dateAsc':  return [...besoins].sort((a, b) => new Date(a.dateCreation).getTime() - new Date(b.dateCreation).getTime());
      case 'titre':    return [...besoins].sort((a, b) => a.titre.localeCompare(b.titre));
      case 'dateDesc':
      default:         return [...besoins].sort((a, b) => new Date(b.dateCreation).getTime() - new Date(a.dateCreation).getTime());
    }
  }

  openCreate() {
    this.form.reset();
    this.showModal = true;
  }

  fermerModal() {
    this.showModal = false;
    this.saving = false;
    this.fichierSelectionne = null;
    this.form.reset();
  }

  onFichierChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.fichierSelectionne = input.files?.[0] ?? null;
  }

  supprimerFichierSelectionne() {
    this.fichierSelectionne = null;
  }

  openEdit(b: BesoinDTO) {
    // Bloquer l'édition si le besoin n'est pas en statut BROUILLON
    if (b.statut !== 'BROUILLON') {
      this.toastr.error('Seuls les besoins en statut BROUILLON peuvent être modifiés.');
      return;
    }
    this.editId = b.id;
    this.editDocuments = [];
    this.editForm.patchValue({
      titre: b.titre,
      description: b.description,
      niveauImportance: b.niveauImportance,
      categorieId: b.categorieId
    });
    this.showEditModal = true;

    // Charger les pièces jointes existantes
    this.besoinsService.getDocuments(b.id).subscribe({
      next: docs => this.editDocuments = docs,
      error: () => {}
    });
  }

  fermerEditModal() {
    this.showEditModal = false;
    this.saving = false;
    this.editId = null;
    this.fichierEditSelectionne = null;
    this.editDocuments = [];
    this.editForm.reset();
  }

  onFichierEditChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.fichierEditSelectionne = input.files?.[0] ?? null;
  }

  submitEdit() {
    if (this.editForm.invalid || !this.editId) return;
    this.saving = true;
    this.besoinsService.update(this.editId, this.editForm.value as any).subscribe({
      next: updated => {
        if (this.fichierEditSelectionne) {
          // Supprimer les documents existants avant d'ajouter le nouveau
          const suppressions$ = this.editDocuments.length > 0
            ? forkJoin(this.editDocuments.map(doc =>
                this.besoinsService.supprimerDocument(updated.id, doc.id)
              ))
            : of([]);

          suppressions$.subscribe({
            next: () => {
              this.besoinsService.ajouterPieceJointe(updated.id, this.fichierEditSelectionne!).subscribe({
                next: () => {
                  this.toastr.success('Besoin modifié avec succès.');
                  this.besoins = this.besoins.map(b => b.id === updated.id ? updated : b);
                  this.fermerEditModal();
                },
                error: () => {
                  this.toastr.warning('Besoin modifié mais l\'upload du fichier a échoué.');
                  this.besoins = this.besoins.map(b => b.id === updated.id ? updated : b);
                  this.fermerEditModal();
                }
              });
            },
            error: () => {
              // Même si la suppression échoue, on ajoute quand même le nouveau fichier
              this.besoinsService.ajouterPieceJointe(updated.id, this.fichierEditSelectionne!).subscribe({
                next: () => {
                  this.toastr.success('Besoin modifié avec succès.');
                  this.besoins = this.besoins.map(b => b.id === updated.id ? updated : b);
                  this.fermerEditModal();
                },
                error: () => {
                  this.toastr.warning('Besoin modifié mais l\'upload du fichier a échoué.');
                  this.besoins = this.besoins.map(b => b.id === updated.id ? updated : b);
                  this.fermerEditModal();
                }
              });
            }
          });
        } else {
          this.toastr.success('Besoin modifié avec succès.');
          this.besoins = this.besoins.map(b => b.id === updated.id ? updated : b);
          this.fermerEditModal();
        }
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
    this.besoinsService.create(this.form.value as any, this.fichierSelectionne ?? undefined).subscribe({
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

  etapeStatut(ordre: number, roleRequis: string, besoinStatut: string): 'active' | 'done' | 'pending' | 'rejected' {
    if (besoinStatut === 'TERMINE') return 'done';
    const roleUp = roleRequis.trim().toUpperCase();

    // Étape rejetée par ce rôle
    if (besoinStatut === `REJETE_PAR_${roleUp}`) return 'rejected';

    // Étape active (en attente, approuvée ou signée par ce rôle)
    const isActive =
      besoinStatut === `EN_ATTENTE_${roleUp}` ||
      besoinStatut === `APPROUVE_PAR_${roleUp}` ||
      besoinStatut === `SIGNE_PAR_${roleUp}`;
    if (isActive) return 'active';

    // Si le statut est un rejet par un autre rôle → les étapes suivantes restent pending
    if (besoinStatut.startsWith('REJETE_PAR_')) {
      const roleRejete = besoinStatut.replace('REJETE_PAR_', '');
      const ordreRejete = this.circuitDetail?.circuit?.etapes
        ?.find(e => e.roleRequis.trim().toUpperCase() === roleRejete)?.ordre ?? 0;
      if (ordreRejete > 0) {
        if (ordre < ordreRejete) return 'done';
        return 'pending'; // étapes après le rejet restent pending
      }
    }

    // Trouver l'ordre de l'étape courante pour les statuts EN_ATTENTE/APPROUVE/SIGNE
    const etapeCouranteOrdre = this.circuitDetail?.circuit?.etapes
      ?.find(e => {
        const r = e.roleRequis.trim().toUpperCase();
        return besoinStatut === `EN_ATTENTE_${r}` ||
               besoinStatut === `APPROUVE_PAR_${r}` ||
               besoinStatut === `SIGNE_PAR_${r}`;
      })?.ordre ?? 0;

    if (etapeCouranteOrdre === 0) return 'pending';
    if (ordre < etapeCouranteOrdre) return 'done';
    if (ordre === etapeCouranteOrdre) return 'active';
    return 'pending';
  }

  async delete(id: number) {
    const ok = await this.confirm.confirm({ titre: 'Supprimer le besoin', message: 'Êtes-vous sûr de vouloir supprimer ce besoin ?', labelConfirm: 'Supprimer', danger: true });
    if (!ok) return;
    this.besoinsService.delete(id).subscribe({
      next: () => { this.toastr.success('Besoin supprimé.'); this.besoins = this.besoins.filter(b => b.id !== id); },
      error: e => this.toastr.error(e.error?.message ?? 'Erreur lors de la suppression.')
    });
  }

  minutesToHHmm(minutes: number): string {
    const h = Math.floor(minutes / 60).toString().padStart(2, '0');
    const m = (minutes % 60).toString().padStart(2, '0');
    return `${h}:${m}`;
  }

  peutSupprimer(b: BesoinDTO): boolean {
    return b.statut === 'BROUILLON' ||
           b.statut === 'ENREGISTRE' ||
           b.statut.startsWith('REJETE_PAR_') ||
           b.estTermine;
  }
}
