import { Component, computed, signal, OnInit, OnDestroy, HostListener, ElementRef, ViewChild } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { SettingsService } from '../../../core/services/settings.service';
import { InactivityService } from '../../../core/services/inactivity.service';
import { ConfirmService } from '../../../core/services/confirm.service';
import { PushNotificationService } from '../../../core/services/push-notification.service';
import { NotificationsService } from '../../../core/services/notifications.service';
import { NotificationDTO } from '../../../core/models/notification.models';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss'
})
export class LayoutComponent implements OnInit, OnDestroy {
  user = this.auth.currentUser;
  pinnedExpanded = signal(false);   // état épinglé via le bouton
  hoverExpanded  = signal(false);   // état ouvert par survol
  expanded = computed(() => this.pinnedExpanded() || this.hoverExpanded());
  showNotifDropdown = signal(false);
  showUserMenu = signal(false);
  notifications: NotificationDTO[] = [];

  // Inactivité
  showInactivityModal = signal(false);
  countdown = signal(0);
  private subs: Subscription[] = [];

  @ViewChild('userMenuRef') userMenuRef!: ElementRef;
  @ViewChild('userBlockRef') userBlockRef!: ElementRef;

  showLogoutConfirm = signal(false);

  confirmLogout() {
    this.showLogoutConfirm.set(true);
  }

  cancelLogout() {
    this.showLogoutConfirm.set(false);
  }

  doLogout() {
    this.showLogoutConfirm.set(false);
    this.auth.logout();
  }

  toggle() {
    this.pinnedExpanded.update(v => !v);
    // Si on ferme via le bouton, on désactive aussi le hover pour éviter le conflit
    if (!this.pinnedExpanded()) {
      this.hoverExpanded.set(false);
    }
  }

  onSidebarEnter() {
    // Le hover n'ouvre que si la sidebar n'est pas épinglée fermée
    if (!this.pinnedExpanded()) this.hoverExpanded.set(true);
  }

  onSidebarLeave() {
    this.hoverExpanded.set(false);
  }

  get initiales(): string {
    const u = this.user();
    if (!u) return '?';
    return `${u.prenom?.[0] ?? ''}${u.nom?.[0] ?? ''}`.toUpperCase();
  }

  get unreadCount(): number {
    return this.notifications.filter(n => !n.lu).length;
  }

  get recentNotifs(): NotificationDTO[] {
    return this.notifications.filter(n => !n.lu).slice(0, 5);
  }

  // Signal pour suivre quel groupe est ouvert (par label)
  openGroup = signal<string | null>(null);

  toggleGroup(label: string) {
    this.openGroup.update(v => v === label ? null : label);
  }

  navItems = computed(() => {
    const has = (p: string) => this.auth.hasPermission(p);
    return [
      { label: 'Dashboard', path: '/dashboard', icon: '/assets/icons/dashboard1.png', show: has('DASHBOARD_CONSULTER') },
      {
        label: 'Besoins', path: null, icon: '/assets/icons/besoin2.png',
        show: has('BESOIN_CONSULTER'),
        children: [
          { label: 'Validation besoin', path: '/besoins', icon: '/assets/icons/besoin2.png', show: has('BESOIN_CONSULTER') },
          { label: 'Besoins en attente', path: '/besoins/validations', icon: '/assets/icons/workflow.png', show: has('BESOIN_VALIDER') },
        ].filter(c => c.show)
      },
      { label: 'Catégories',    path: '/categories',        icon: '/assets/icons/categories.png',              show: has('CATEGORIE_CONSULTER') },
      { label: 'Workflow',      path: '/workflow',          icon: '/assets/icons/workflow.png',                show: has('WORKFLOW_CONSULTER') },
      { label: 'Notifications', path: '/notifications',     icon: '/assets/icons/notification.png',            show: has('NOTIFICATION_LIRE') },
      { label: 'Reporting',     path: '/reporting',         icon: '/assets/icons/reporting.png',               show: has('RAPPORT_CONSULTER') || has('DASHBOARD_CONSULTER') },
      { label: 'Logs',          path: '/logs',              icon: '/assets/icons/logs.png',                    show: has('LOG_CONSULTER') },
      { label: 'Utilisateurs',  path: '/admin/users',       icon: '/assets/icons/utilisateurs.png',            show: has('USER_CONSULTER') },
      { label: 'Rôles',         path: '/admin/roles',       icon: '/assets/icons/role.png',                    show: has('ROLE_CONSULTER') },
      { label: 'Permissions',   path: '/admin/permissions', icon: '/assets/icons/permissions.png',             show: has('PERMISSION_CONSULTER') },
      { label: 'Ma Signature',  path: '/ma-signature',      icon: '/assets/icons/signature-electronique.png', show: has('SIGNATURE_PERSO_GERER') },
    ].filter(i => i.show);
  });

  constructor(
    public auth: AuthService,
    public theme: ThemeService,
    private settingsService: SettingsService,
    private inactivity: InactivityService,
    public confirmService: ConfirmService,
    private pushService: PushNotificationService,
    private notifService: NotificationsService,
    private router: Router
  ) {}

  ngOnInit() {
    if (this.auth.hasPermission('NOTIFICATION_LIRE')) {
      this.chargerNotifications();
    }
    // Initialiser le Service Worker et s'abonner aux push si autorisé
    const s = this.settingsService.settings();
    if (s.notifApp) {
      this.pushService.init().then(() => {
        this.pushService.isSubscribed().then(already => {
          if (!already) this.pushService.subscribe();
        });
      });
    }
    // Démarrer la surveillance d'inactivité
    if (this.settingsService.settings().deconnexionAuto) {
      this.inactivity.start();
    }
    this.subs.push(
      this.inactivity.warningStart$.subscribe(sec => {
        this.countdown.set(sec);
        this.showInactivityModal.set(true);
      }),
      this.inactivity.countdown$.subscribe(sec => this.countdown.set(sec)),
      this.inactivity.logout$.subscribe(() => {
        this.showInactivityModal.set(false);
        this.auth.logout();
      })
    );
  }

  ngOnDestroy() {
    this.inactivity.stop();
    this.subs.forEach(s => s.unsubscribe());
  }

  resterConnecte() {
    this.showInactivityModal.set(false);
    this.inactivity.keepAlive();
  }

  chargerNotifications() {
    this.notifService.getMesNotifications().subscribe({
      next: n => this.notifications = n,
      error: () => {}
    });
  }

  toggleNotifDropdown(event: Event) {
    event.stopPropagation();
    this.showNotifDropdown.update(v => !v);
    this.showUserMenu.set(false);
  }

  toggleUserMenu(event: Event) {
    event.stopPropagation();
    this.showUserMenu.update(v => !v);
    this.showNotifDropdown.set(false);
  }

  navigateTo(path: string) {
    this.showUserMenu.set(false);
    this.router.navigate([path]);
  }

  marquerLuEtNaviguer(notif: NotificationDTO) {
    if (!notif.lu) {
      this.notifService.marquerLu(notif.id).subscribe(() => {
        notif.lu = true;
      });
    }
    this.showNotifDropdown.set(false);
    this.router.navigate(['/notifications']);
  }

  @HostListener('document:click')
  fermerDropdown() {
    this.showNotifDropdown.set(false);
    this.showUserMenu.set(false);
  }
}
