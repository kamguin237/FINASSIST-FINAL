import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { ReportingService } from '../../core/services/reporting.service';
import { AuthService } from '../../core/services/auth.service';
import { StatistiquesDTO } from '../../core/models/reporting.models';
import { CustomSelectComponent, SelectOption } from '../../shared/components/custom-select/custom-select.component';

interface BesoinRapport {
  id: number;
  titre: string;
  statut: string;
  niveauImportance: string;
  categorie: string;
  createur: string;
  dateCreation: string;
  dateModification: string;
}

@Component({
  selector: 'app-reporting',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomSelectComponent],
  templateUrl: './reporting.component.html',
  styleUrl: './reporting.component.scss'
})
export class ReportingComponent implements OnInit {
  stats: StatistiquesDTO | null = null;
  loading = true;
  searching = false;
  resultats: BesoinRapport[] | null = null;
  erreurDates = false;

  statutOptions: SelectOption[] = [
    { value: 'BROUILLON', label: 'Brouillon' },
    { value: 'ENREGISTRE', label: 'Enregistré' },
    { value: 'EN_ATTENTE', label: 'En attente' },
    { value: 'TRANSMIS', label: 'Transmis' },
    { value: 'APPROUVE_ROLE1', label: 'Approuvé N1' },
    { value: 'APPROUVE_ROLE2', label: 'Approuvé N2' },
    { value: 'REJETE_ROLE1', label: 'Rejeté N1' },
    { value: 'REJETE_ROLE2', label: 'Rejeté N2' },
    { value: 'SIGNE_ROLE1', label: 'Signé N1' },
    { value: 'SIGNE_ROLE2', label: 'Signé N2' },
    { value: 'TERMINE', label: 'Terminé' },
  ];

  filtreForm = this.fb.group({ dateDebut: [''], dateFin: [''], statut: [''] });

  constructor(public auth: AuthService, private reportingService: ReportingService, private fb: FormBuilder) {}

  ngOnInit() {
    if (this.auth.hasPermission('RAPPORT_CONSULTER')) {
      this.reportingService.getStatistiques().subscribe({
        next: s => { this.stats = s; this.loading = false; },
        error: () => this.loading = false
      });
    } else {
      this.loading = false;
    }
  }

  rechercher() {
    const { dateDebut, dateFin } = this.filtreForm.value;
    if (!dateDebut || !dateFin) {
      this.erreurDates = true;
      return;
    }
    this.erreurDates = false;
    this.searching = true;
    this.resultats = null;
    const filtres = this.filtreForm.value as any;
    this.reportingService.getRapportBesoins(filtres).subscribe({
      next: (rapport: any) => {
        this.resultats = rapport.contenu ?? [];
        this.searching = false;
      },
      error: () => { this.resultats = []; this.searching = false; }
    });
  }

  get peutExporter(): boolean {
    return this.resultats !== null && this.resultats.length > 0;
  }

  exporter(format: 'PDF' | 'EXCEL') {
    if (!this.peutExporter) return;
    const filtres = this.filtreForm.value as any;
    this.reportingService.exporter({ format, filtres }).subscribe(blob => {
      const url = URL.createObjectURL(blob as Blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `rapport_besoins.${format === 'PDF' ? 'pdf' : 'xlsx'}`;
      a.click();
      URL.revokeObjectURL(url);
    });
  }
}
