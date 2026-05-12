import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DeadlineService } from '../../../core/services/deadline.service';
import { BesoinDeadlineDTO } from '../../../core/models/deadline.models';

@Component({
  selector: 'app-validations-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './validations-dashboard.component.html',
  styleUrl: './validations-dashboard.component.scss'
})
export class ValidationsDashboardComponent implements OnInit, OnDestroy {
  besoins: BesoinDeadlineDTO[] = [];
  loading = true;
  Math = Math;

  private tickInterval?: ReturnType<typeof setInterval>;

  constructor(
    private deadlineService: DeadlineService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.charger();

    // Recalculer les barres de progression toutes les 60 secondes sans appel API
    this.tickInterval = setInterval(() => {
      this.recalculerProgression();
      this.cdr.detectChanges();
    }, 60_000);
  }

  ngOnDestroy() {
    if (this.tickInterval) clearInterval(this.tickInterval);
  }

  charger() {
    this.deadlineService.getMesDeadlines().subscribe({
      next: data => {
        this.besoins = data;
        this.loading = false;
        // Recalcul immédiat dès le chargement pour corriger les valeurs stales
        this.recalculerProgression();
        this.cdr.detectChanges();
      },
      error: () => this.loading = false
    });
  }

  /**
   * Recalcule localement pourcentageEcoule, minutesRestantes, urgence
   * et les flags de rappels à partir de dateEntreeEnAttente et delaiMaxMinutes.
   */
  private recalculerProgression() {
    const now = Date.now();
    for (const b of this.besoins) {
      if (!b.dateEntreeEnAttente || b.delaiMaxMinutes <= 0) continue;

      const dateRef = new Date(b.dateEntreeEnAttente).getTime();
      const elapsedMin = (now - dateRef) / 60_000;
      const pct = Math.min((elapsedMin / b.delaiMaxMinutes) * 100, 200);

      b.pourcentageEcoule = Math.round(pct * 10) / 10;
      b.minutesRestantes  = Math.max(b.delaiMaxMinutes - elapsedMin, 0);

      // Mettre à jour l'urgence
      if (pct >= 100)     b.urgence = 'expired';
      else if (pct >= 80) b.urgence = 'danger';
      else if (pct >= 50) b.urgence = 'warning';
      else                b.urgence = 'normal';

      // Basculer les flags de rappels localement quand le seuil est franchi
      // (le backend les enverra réellement, mais l'UI reflète l'état attendu)
      if (pct >= 50  && !b.rappel1Envoye)        b.rappel1Envoye = true;
      if (pct >= 80  && !b.rappel2Envoye)        b.rappel2Envoye = true;
      if (pct >= 100 && !b.emailRappelEnvoye)    b.emailRappelEnvoye = true;
      if (pct >= 160 && !b.rejeteAutomatiquement) b.rejeteAutomatiquement = true; // 100% + 60 min
    }
  }

  getUrgenceClass(b: BesoinDeadlineDTO): string {
    return b.urgence;
  }

  getBarColor(pct: number): string {
    if (pct >= 100) return '#ef4444';
    if (pct >= 80)  return '#f97316';
    if (pct >= 50)  return '#f59e0b';
    return '#22c55e';
  }

  formatDuree(minutes: number): string {
    if (minutes <= 0) return '0 min';
    if (minutes < 60) return `${Math.round(minutes)} min`;
    const h = Math.floor(minutes / 60);
    const m = Math.round(minutes % 60);
    return m > 0 ? `${h}h${m.toString().padStart(2, '0')}` : `${h}h`;
  }

  tempsAvantRejet(b: BesoinDeadlineDTO): string {
    const min = b.minutesRestantes + 60;
    return min > 0 ? `dans ${this.formatDuree(min)}` : '';
  }

  tempsAvantSeuil(b: BesoinDeadlineDTO, seuilPct: number): string {
    if (b.delaiMaxMinutes <= 0) return '';
    const minutesEcoulees = b.delaiMaxMinutes - b.minutesRestantes;
    const minutesAuSeuil = (b.delaiMaxMinutes * seuilPct / 100) - minutesEcoulees;
    if (minutesAuSeuil <= 0) return '';
    return `dans ${this.formatDuree(minutesAuSeuil)}`;
  }

  formatRole(etapeRole: string): string {
    if (!etapeRole) return '';
    return etapeRole.charAt(0) + etapeRole.slice(1).toLowerCase();
  }
}
