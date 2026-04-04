import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { NotificationDTO, CreateNotificationDTO } from '../models/notification.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private url = `${environment.apiUrl}/notifications`;

  constructor(private http: HttpClient) {}

  getMesNotifications()                     { return this.http.get<NotificationDTO[]>(this.url); }
  marquerLu(id: number)                     { return this.http.put<void>(`${this.url}/${id}/lire`, {}); }
  envoyer(dto: CreateNotificationDTO)       { return this.http.post<NotificationDTO>(this.url, dto); }
}
