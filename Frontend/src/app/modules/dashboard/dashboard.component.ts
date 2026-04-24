import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { ReportingService } from '../../core/services/reporting.service';
import { DashboardDTO, EvolutionPoint } from '../../core/models/reporting.models';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('chartCanvas') chartCanvas!: ElementRef<HTMLCanvasElement>;

  dashboard: DashboardDTO | null = null;
  loading = true;
  periode: 'jours' | 'semaines' | 'mois' = 'jours';
  evolutionData: EvolutionPoint[] = [];
  private chart: Chart | null = null;
  private themeObserver: MutationObserver | null = null;
  private viewReady = false;
  private dataReady = false;

  // ── KPI getters ────────────────────────────────────────────────────────────
  get totalRecus() { return this.evolutionData.reduce((s, p) => s + p.recus, 0); }
  get totalApprouves() { return this.evolutionData.reduce((s, p) => s + p.approuves, 0); }
  get totalRejetes()   { return this.evolutionData.reduce((s, p) => s + p.rejetes, 0); }
  get maxAttente()     { return Math.max(...this.evolutionData.map(p => p.enAttentePlus48h), 0); }

  constructor(private reportingService: ReportingService) {}

  ngOnInit() {
    this.reportingService.getDashboard().subscribe({
      next: d => { this.dashboard = d; this.loading = false; },
      error: () => this.loading = false
    });
    this.chargerEvolution();
  }

  ngAfterViewInit() {
    this.viewReady = true;
    // Si les données sont déjà arrivées avant que la vue soit prête, on rend maintenant
    if (this.dataReady) this.renderChart();

    this.themeObserver = new MutationObserver(() => {
      if (this.evolutionData.length > 0) this.renderChart();
    });
    this.themeObserver.observe(document.body, { attributes: true, attributeFilter: ['class'] });
  }

  ngOnDestroy() {
    this.chart?.destroy();
    this.themeObserver?.disconnect();
  }

  chargerEvolution() {
    this.reportingService.getEvolutionBesoins(this.periode).subscribe({
      next: data => {
        this.evolutionData = data;
        this.dataReady = true;
        // Rendre seulement si la vue est prête (canvas disponible)
        if (this.viewReady) this.renderChart();
      },
      error: () => {}
    });
  }

  changerPeriode(p: 'jours' | 'semaines' | 'mois') {
    this.periode = p;
    this.chargerEvolution();
  }

  renderChart() {
    if (!this.chartCanvas) return;
    this.chart?.destroy();

    const isLight = document.body.classList.contains('light-mode');
    const gridColor   = isLight ? 'rgba(0,0,0,0.06)'      : 'rgba(255,255,255,0.07)';
    const labelColor  = isLight ? '#4a5080'                : '#a0a0c0';
    const tooltipBg   = isLight ? 'rgba(255,255,255,0.95)' : 'rgba(20,20,40,0.95)';
    const tooltipText = isLight ? '#1a1a3e'                : '#ffffff';

    const labels = this.evolutionData.map(p => {
      const d = new Date(p.date);
      if (this.periode === 'mois')
        return d.toLocaleDateString('fr-FR', { month: 'short', year: '2-digit' });
      return d.toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit' });
    });

    const config: ChartConfiguration = {
      type: 'line',
      data: {
        labels,
        datasets: [
          {
            label: 'Reçus',
            data: this.evolutionData.map(p => p.recus),
            borderColor: '#7c6ff7', backgroundColor: 'rgba(124,111,247,0.12)',
            borderWidth: 2.5, pointBackgroundColor: '#7c6ff7',
            pointRadius: 4, pointHoverRadius: 7, tension: 0.4, fill: true, yAxisID: 'y'
          },
          {
            label: 'Approuvés',
            data: this.evolutionData.map(p => p.approuves),
            borderColor: '#22c55e', backgroundColor: 'rgba(34,197,94,0.08)',
            borderWidth: 2.5, pointBackgroundColor: '#22c55e',
            pointRadius: 4, pointHoverRadius: 7, tension: 0.4, fill: false, yAxisID: 'y'
          },
          {
            label: 'Rejetés',
            data: this.evolutionData.map(p => p.rejetes),
            borderColor: '#ef4444', backgroundColor: 'rgba(239,68,68,0.08)',
            borderWidth: 2.5, pointBackgroundColor: '#ef4444',
            pointRadius: 4, pointHoverRadius: 7, tension: 0.4, fill: false, yAxisID: 'y'
          },
          {
            label: 'En attente +48h',
            data: this.evolutionData.map(p => p.enAttentePlus48h),
            borderColor: '#f59e0b', backgroundColor: 'rgba(245,158,11,0.08)',
            borderWidth: 2, borderDash: [6, 3], pointBackgroundColor: '#f59e0b',
            pointRadius: 4, pointHoverRadius: 7, tension: 0.4, fill: false, yAxisID: 'y'
          },
          {
            label: 'Taux approbation %',
            data: this.evolutionData.map(p => p.tauxApprobation),
            borderColor: '#facc15', backgroundColor: 'transparent',
            borderWidth: 2, borderDash: [4, 4], pointBackgroundColor: '#facc15',
            pointRadius: 3, pointHoverRadius: 6, tension: 0.4, fill: false, yAxisID: 'y2'
          }
        ]
      },
      options: {
        responsive: true, maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        plugins: {
          legend: {
            position: 'top',
            labels: { color: labelColor, font: { size: 12 }, padding: 20, usePointStyle: true, pointStyleWidth: 12 }
          },
          tooltip: {
            backgroundColor: tooltipBg, titleColor: tooltipText, bodyColor: tooltipText,
            borderColor: 'rgba(124,111,247,0.3)', borderWidth: 1, padding: 12, cornerRadius: 10,
            callbacks: {
              label: (ctx) => {
                const v = ctx.parsed.y ?? 0;
                // return ctx.dataset.label === 'Taux approbation %'
                //   ? ` Taux : ${v.toFixed(1)}%`
                //   : ` ${ctx.dataset.label} : ${v}`;
              }
            }
          }
        },
        scales: {
          x: {
            grid: { color: gridColor },
            ticks: { color: labelColor, font: { size: 11 } },
            border: { color: 'transparent' }
          },
          y: {
            position: 'left', grid: { color: gridColor },
            ticks: { color: labelColor, font: { size: 11 }, stepSize: 1 },
            border: { color: 'transparent' }, beginAtZero: true,
            title: { display: true, text: 'Nombre de besoins', color: labelColor, font: { size: 11 } }
          },
          y2: {
            position: 'right', grid: { drawOnChartArea: false },
            ticks: { color: '#facc15', font: { size: 11 }, callback: (v) => `${v}%` },
            border: { color: 'transparent' }, min: 0, max: 100,
            title: { display: true, text: 'Taux approbation', color: '#facc15', font: { size: 11 } }
          }
        }
      }
    };

    this.chart = new Chart(this.chartCanvas.nativeElement, config);
  }

  statutLabel(statut: string): string {
    if (statut === 'BROUILLON')  return 'Brouillon';
    if (statut === 'ENREGISTRE') return 'Enregistré';
    if (statut === 'SOUMISE')    return 'Soumis';
    if (statut === 'TRANSMIS')   return 'Transmis';
    if (statut === 'TERMINE')    return 'Terminé';

    // Statuts dynamiques basés sur le rôle
    if (statut.startsWith('EN_ATTENTE_')) {
      const role = statut.replace('EN_ATTENTE_', '');
      return `En attente — ${this.formatRole(role)}`;
    }
    if (statut.startsWith('APPROUVE_PAR_')) {
      const role = statut.replace('APPROUVE_PAR_', '');
      return `Approuvé par ${this.formatRole(role)}`;
    }
    if (statut.startsWith('REJETE_PAR_')) {
      const role = statut.replace('REJETE_PAR_', '');
      return `Rejeté par ${this.formatRole(role)}`;
    }
    if (statut.startsWith('SIGNE_PAR_')) {
      const role = statut.replace('SIGNE_PAR_', '');
      return `Signé par ${this.formatRole(role)}`;
    }
    return statut;
  }

  // Formate le nom du rôle extrait du statut pour l'affichage
  // Ex: "RESPONSABLE" → "le Responsable" | "DIRECTIONGENERALE" → "la Direction Générale"
  private formatRole(roleCode: string): string {
    const map: Record<string, string> = {
      'RESPONSABLE':        'le Responsable',
      'DIRECTION':          'la Direction',
      'DIRECTIONGENERALE':  'la Direction Générale',
      'ADMINISTRATEUR':     "l'Administrateur",
    };
    return map[roleCode] ?? roleCode.charAt(0) + roleCode.slice(1).toLowerCase();
  }

  badgeClass(statut: string): string {
    if (statut === 'BROUILLON')                          return 'grey';
    if (statut === 'SOUMISE' || statut === 'TRANSMIS')   return 'blue';
    if (statut.startsWith('EN_ATTENTE_'))                return 'blue';
    if (statut.startsWith('APPROUVE_PAR_'))              return 'green';
    if (statut.startsWith('REJETE_PAR_'))                return 'red';
    if (statut.startsWith('SIGNE_PAR_'))                 return 'purple';
    if (statut === 'TERMINE')                            return 'cyan';
    return 'grey';
  }
}
