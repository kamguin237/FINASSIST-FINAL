import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="p-4">
      <h2 class="text-xl font-bold mb-4">Journaux d'activité</h2>
      <div class="mb-4 flex gap-2">
        <input [(ngModel)]="filtres.action" placeholder="Action" class="border p-2 rounded" />
        <input [(ngModel)]="filtres.entiteType" placeholder="Type entité" class="border p-2 rounded" />
        <button (click)="charger()" class="bg-blue-600 text-white px-4 py-2 rounded">Filtrer</button>
      </div>
      <table class="w-full border-collapse border">
        <thead>
          <tr class="bg-gray-100">
            <th class="border p-2">Date</th>
            <th class="border p-2">Action</th>
            <th class="border p-2">Entité</th>
            <th class="border p-2">Utilisateur</th>
            <th class="border p-2">IP</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let log of logs">
            <td class="border p-2">{{ log.date | date:'short' }}</td>
            <td class="border p-2">{{ log.action }}</td>
            <td class="border p-2">{{ log.entiteType }}</td>
            <td class="border p-2">{{ log.nomUtilisateur }}</td>
            <td class="border p-2">{{ log.adresseIp }}</td>
          </tr>
        </tbody>
      </table>
      <div class="mt-4 flex gap-2">
        <button (click)="page(-1)" [disabled]="filtres.page === 1" class="border px-3 py-1 rounded">Précédent</button>
        <span>Page {{ filtres.page }}</span>
        <button (click)="page(1)" [disabled]="logs.length < filtres.pageSize" class="border px-3 py-1 rounded">Suivant</button>
      </div>
    </div>
  `
})
export class LogsComponent implements OnInit {
  logs: any[] = [];
  filtres = { action: '', entiteType: '', page: 1, pageSize: 20 };

  constructor(private http: HttpClient) {}

  ngOnInit() { this.charger(); }

  charger() {
    const params = Object.entries(this.filtres)
      .filter(([, v]) => v !== '' && v !== null)
      .map(([k, v]) => `${k}=${v}`).join('&');
    this.http.get<any>(`${environment.apiUrl}/api/logs?${params}`)
      .subscribe(r => this.logs = r.items ?? []);
  }

  page(dir: number) {
    this.filtres.page += dir;
    this.charger();
  }
}
