import { Component, OnInit } from '@angular/core';
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
export class ValidationsDashboardComponent implements OnInit {
  besoins: BesoinDeadlineDTO[] = [];
  loading = true;
  Math = Math;

  constructor(private deadlineService: DeadlineService) {}

  ngOnInit() {
    this.charger();
  }

  charger() {
    this.deadlineService.getMesDeadlines().subscribe({
      next: data => { this.besoins = data; this.loading = false; },
      error: () => this.loading = false
    });
  }

  getUrgenceClass(b: BesoinDeadlineDTO): string {
    return b.urgence; // 'normal' | 'warning' | 'danger' | 'expired'
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
