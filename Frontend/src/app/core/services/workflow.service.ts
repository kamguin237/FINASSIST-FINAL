import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { WorkflowCircuitDTO, CreateWorkflowCircuitDTO, UpdateWorkflowCircuitDTO, ValiderBesoinDTO } from '../models/workflow.models';
import { BesoinDTO } from '../models/besoin.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class WorkflowService {
  private url = `${environment.apiUrl}/workflow`;

  constructor(private http: HttpClient) {}

  // Circuits
  getCircuits()                                   { return this.http.get<WorkflowCircuitDTO[]>(`${this.url}/circuits`); }
  getCircuit(id: number)                          { return this.http.get<WorkflowCircuitDTO>(`${this.url}/circuits/${id}`); }
  createCircuit(dto: CreateWorkflowCircuitDTO)    { return this.http.post<WorkflowCircuitDTO>(`${this.url}/circuits`, dto); }
  updateCircuit(id: number, dto: UpdateWorkflowCircuitDTO) { return this.http.put<WorkflowCircuitDTO>(`${this.url}/circuits/${id}`, dto); }
  deleteCircuit(id: number)                       { return this.http.delete<void>(`${this.url}/circuits/${id}`); }

  // Actions sur besoin
  valider(id: number, dto: ValiderBesoinDTO)      { return this.http.post<BesoinDTO>(`${this.url}/${id}/valider`, dto); }
  transmettre(id: number)                         { return this.http.post<BesoinDTO>(`${this.url}/${id}/transmettre`, {}); }
}
