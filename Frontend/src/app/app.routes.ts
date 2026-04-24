import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./modules/auth/login/login.component').then(m => m.LoginComponent) },
  { path: '403',   loadComponent: () => import('./shared/components/forbidden/forbidden.component').then(m => m.ForbiddenComponent) },
  { path: 'sign-mobile/:token', loadComponent: () => import('./modules/sign-mobile/sign-mobile.component').then(m => m.SignMobileComponent) },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./shared/components/layout/layout.component').then(m => m.LayoutComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        canActivate: [permissionGuard],
        data: { permission: 'DASHBOARD_CONSULTER' },
        loadComponent: () => import('./modules/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'besoins',
        canActivate: [permissionGuard],
        data: { permission: 'BESOIN_CONSULTER' },
        children: [
          { path: '', loadComponent: () => import('./modules/besoins/besoins-list/besoins-list.component').then(m => m.BesoinsListComponent) },
          { path: 'validations', canActivate: [permissionGuard], data: { permission: 'BESOIN_VALIDER' }, loadComponent: () => import('./modules/besoins/validations-dashboard/validations-dashboard.component').then(m => m.ValidationsDashboardComponent) },
          { path: 'new', canActivate: [permissionGuard], data: { permission: 'BESOIN_CREER' }, loadComponent: () => import('./modules/besoins/besoin-form/besoin-form.component').then(m => m.BesoinFormComponent) },
          { path: ':id', loadComponent: () => import('./modules/besoins/besoin-detail/besoin-detail.component').then(m => m.BesoinDetailComponent) },
          { path: ':id/edit', canActivate: [permissionGuard], data: { permission: 'BESOIN_MODIFIER' }, loadComponent: () => import('./modules/besoins/besoin-form/besoin-form.component').then(m => m.BesoinFormComponent) },
        ]
      },
      {
        path: 'categories',
        canActivate: [permissionGuard],
        data: { permission: 'CATEGORIE_CONSULTER' },
        loadComponent: () => import('./modules/categories/categories.component').then(m => m.CategoriesComponent)
      },
      {
        path: 'workflow',
        canActivate: [permissionGuard],
        data: { permission: 'WORKFLOW_CONSULTER' },
        loadComponent: () => import('./modules/workflow/workflow.component').then(m => m.WorkflowComponent)
      },
      {
        path: 'notifications',
        canActivate: [permissionGuard],
        data: { permission: 'NOTIFICATION_LIRE' },
        loadComponent: () => import('./modules/notifications/notifications.component').then(m => m.NotificationsComponent)
      },
      {
        path: 'reporting',
        canActivate: [permissionGuard],
        data: { permission: 'DASHBOARD_CONSULTER' },
        loadComponent: () => import('./modules/reporting/reporting.component').then(m => m.ReportingComponent)
      },
      {
        path: 'logs',
        canActivate: [permissionGuard],
        data: { permission: 'LOG_CONSULTER' },
        loadComponent: () => import('./modules/logs/logs.component').then(m => m.LogsComponent)
      },
      {
        path: 'admin/users',
        canActivate: [permissionGuard],
        data: { permission: 'USER_CONSULTER' },
        loadComponent: () => import('./modules/admin/users/users.component').then(m => m.UsersComponent)
      },
      {
        path: 'admin/roles',
        canActivate: [permissionGuard],
        data: { permission: 'ROLE_CONSULTER' },
        loadComponent: () => import('./modules/admin/roles/roles.component').then(m => m.RolesComponent)
      },
      {
        path: 'admin/permissions',
        canActivate: [permissionGuard],
        data: { permission: 'PERMISSION_CONSULTER' },
        loadComponent: () => import('./modules/admin/permissions/permissions.component').then(m => m.PermissionsComponent)
      },
      {
        path: 'profil',
        loadComponent: () => import('./modules/profil/profil.component').then(m => m.ProfilComponent)
      },
      {
        path: 'ma-signature',
        canActivate: [permissionGuard],
        data: { permission: 'SIGNATURE_PERSO_GERER' },
        loadComponent: () => import('./modules/ma-signature/ma-signature.component').then(m => m.MaSignatureComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./modules/settings/settings.component').then(m => m.SettingsComponent)
      },
    ]
  },
  { path: '**', redirectTo: '' }
];
