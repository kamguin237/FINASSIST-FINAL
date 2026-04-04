import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { NotificationsService } from '../../core/services/notifications.service';
import { UsersService } from '../../core/services/users.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationDTO } from '../../core/models/notification.models';
import { UtilisateurDTO } from '../../core/models/user.models';
import { CustomSelectComponent } from '../../shared/components/custom-select/custom-select.component';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomSelectComponent],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss',
  encapsulation: ViewEncapsulation.None
})
export class NotificationsComponent implements OnInit {
  notifications: NotificationDTO[] = [];
  utilisateurs: UtilisateurDTO[] = [];
  showForm = false;
  selectedIds: number[] = [];
  filtreLecture: string = '';

  lectureOptions = [
    { value: 'NON_LU', label: 'Messages non lus' },
    { value: 'LU',     label: 'Messages lus' },
  ];

  get notificationsFiltrees(): NotificationDTO[] {
    if (!this.filtreLecture) return this.notifications;
    return this.notifications.filter(n =>
      this.filtreLecture === 'LU' ? n.lu : !n.lu
    );
  }

  onFiltreLectureChange(val: string | null) {
    this.filtreLecture = val ?? '';
  }

  reinitialiserFiltre() {
    this.filtreLecture = '';
  }

  get tousSelectionnes(): boolean {
    return this.utilisateurs.length > 0 && this.selectedIds.length === this.utilisateurs.length;
  }

  estSelectionne(id: number): boolean {
    return this.selectedIds.includes(id);
  }

  toggleDestinataire(id: number, event: Event) {
    const checked = (event.target as HTMLInputElement).checked;
    if (checked) {
      if (!this.selectedIds.includes(id)) this.selectedIds = [...this.selectedIds, id];
    } else {
      this.selectedIds = this.selectedIds.filter(x => x !== id);
    }
    this.form.patchValue({ utilisateurIds: this.selectedIds });
  }

  toggleTous(event: Event) {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectedIds = checked ? this.utilisateurs.map(u => u.id) : [];
    this.form.patchValue({ utilisateurIds: this.selectedIds });
  }

  form = this.fb.group({
    message:        ['', Validators.required],
    type:           ['INFO', Validators.required],
    utilisateurIds: [[] as number[]]
  });

  constructor(
    public auth: AuthService,
    private notifService: NotificationsService,
    private usersService: UsersService,
    private fb: FormBuilder
  ) {}

  ngOnInit() {
    this.notifService.getMesNotifications().subscribe(n => this.notifications = n);
    if (this.auth.hasPermission('USER_CONSULTER')) {
      this.usersService.getAll().subscribe(u => {
        const moi = this.auth.currentUser();
        this.utilisateurs = u.filter(user => user.id !== moi?.id);
      });
    }
  }

  marquerLu(id: number) {
    this.notifService.marquerLu(id).subscribe(() => {
      const n = this.notifications.find(x => x.id === id);
      if (n) n.lu = true;
    });
  }

  envoyer() {
    if (this.form.invalid || this.selectedIds.length === 0) return;
    const payload = { ...this.form.value, utilisateurIds: this.selectedIds };
    this.notifService.envoyer(payload as any).subscribe(() => {
      this.fermerForm();
      this.notifService.getMesNotifications().subscribe(n => this.notifications = n);
    });
  }

  fermerForm() {
    this.showForm = false;
    this.selectedIds = [];
    this.form.reset({ type: 'INFO', utilisateurIds: [] });
  }
}
