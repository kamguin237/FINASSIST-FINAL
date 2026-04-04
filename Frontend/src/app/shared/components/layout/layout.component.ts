import { Component, computed, signal, OnInit, HostListener } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { NotificationsService } from '../../../core/services/notifications.service';
import { NotificationDTO } from '../../../core/models/notification.models';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss'
})
export class LayoutComponent implements OnInit {
  user = this.auth.currentUser;
  expanded = signal(false);
  showNotifDropdown = signal(false);
  notifications: NotificationDTO[] = [];

  toggle() { this.expanded.update(v => !v); }

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

  navItems = computed(() => {
    const has = (p: string) => this.auth.hasPermission(p);
    return [
      { label: 'Dashboard',     path: '/dashboard',         icon: '/assets/icons/dashboard1.png',  show: has('DASHBOARD_CONSULTER') },
      { label: 'Besoins',       path: '/besoins',           icon: '/assets/icons/besoin2.png', show: has('BESOIN_CONSULTER') },
      { label: 'Catégories',    path: '/categories',        icon: '/assets/icons/categories.png', show: has('CATEGORIE_CONSULTER') },
      { label: 'Workflow',      path: '/workflow',          icon: '/assets/icons/workflow.png', show: has('WORKFLOW_CONSULTER') },
      { label: 'Notifications', path: '/notifications',     icon: '/assets/icons/notification.png', show: has('NOTIFICATION_LIRE') },
      { label: 'Reporting',     path: '/reporting',         icon: '/assets/icons/reporting.png', show: has('RAPPORT_CONSULTER') || has('DASHBOARD_CONSULTER') },
      { label: 'Logs',          path: '/logs',              icon: '/assets/icons/logs.png', show: has('LOG_CONSULTER') },
      { label: 'Utilisateurs',  path: '/admin/users',       icon: '/assets/icons/utilisateurs.png', show: has('USER_CONSULTER') },
      { label: 'Rôles',         path: '/admin/roles',       icon: '/assets/icons/role.png', show: has('ROLE_CONSULTER') },
      { label: 'Permissions',   path: '/admin/permissions', icon: '/assets/icons/permissions.png', show: has('PERMISSION_CONSULTER') },
    ].filter(i => i.show);
  });

  constructor(
    public auth: AuthService,
    public theme: ThemeService,
    private notifService: NotificationsService,
    private router: Router
  ) {}

  ngOnInit() {
    if (this.auth.hasPermission('NOTIFICATION_LIRE')) {
      this.chargerNotifications();
    }
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
  }
}
