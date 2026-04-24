import { Injectable, NgZone, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { SettingsService } from './settings.service';

@Injectable({ providedIn: 'root' })
export class InactivityService implements OnDestroy {
  /** Émis quand le compte à rebours commence */
  readonly warningStart$ = new Subject<number>(); // secondes restantes
  /** Émis à chaque tick du compte à rebours */
  readonly countdown$ = new Subject<number>();
  /** Émis quand la déconnexion doit avoir lieu */
  readonly logout$ = new Subject<void>();

  private inactivityTimer: any = null;
  private countdownTimer: any = null;
  private countdownValue = 0;
  private active = false;

  private readonly EVENTS = ['mousemove', 'mousedown', 'keydown', 'touchstart', 'scroll', 'click'];

  constructor(private settings: SettingsService, private zone: NgZone) {}

  start() {
    if (this.active) return;
    this.active = true;
    this.EVENTS.forEach(e => document.addEventListener(e, this.onActivity, { passive: true }));
    this.resetTimer();
  }

  stop() {
    this.active = false;
    this.EVENTS.forEach(e => document.removeEventListener(e, this.onActivity));
    this.clearTimers();
  }

  keepAlive() {
    this.clearTimers();
    this.resetTimer();
  }

  private onActivity = () => {
    if (!this.active) return;
    // Si le compte à rebours n'est pas en cours, reset le timer d'inactivité
    if (this.countdownTimer === null) {
      this.resetTimer();
    }
  };

  private resetTimer() {
    this.clearTimers();
    const s = this.settings.settings();
    if (!s.deconnexionAuto) return;

    const inactiviteMs = s.inactiviteMinutes * 60 * 1000;
    this.zone.runOutsideAngular(() => {
      this.inactivityTimer = setTimeout(() => {
        this.zone.run(() => this.startCountdown());
      }, inactiviteMs);
    });
  }

  private startCountdown() {
    const s = this.settings.settings();
    this.countdownValue = s.avertissementSecondes;
    this.warningStart$.next(this.countdownValue);

    this.zone.runOutsideAngular(() => {
      this.countdownTimer = setInterval(() => {
        this.zone.run(() => {
          this.countdownValue--;
          this.countdown$.next(this.countdownValue);
          if (this.countdownValue <= 0) {
            this.clearTimers();
            this.logout$.next();
          }
        });
      }, 1000);
    });
  }

  private clearTimers() {
    if (this.inactivityTimer) { clearTimeout(this.inactivityTimer); this.inactivityTimer = null; }
    if (this.countdownTimer)  { clearInterval(this.countdownTimer); this.countdownTimer = null; }
  }

  ngOnDestroy() { this.stop(); }
}
