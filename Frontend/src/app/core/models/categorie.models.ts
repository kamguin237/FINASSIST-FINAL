import { WorkflowCircuitDTO } from './workflow.models';

export interface CategorieDTO {
  id: number;
  nom: string;
  description?: string;
  dateCreation: string;
  workflowCircuitId?: number;
  workflowCircuitNom?: string;
}

export interface CategorieDetailDTO extends CategorieDTO {
  circuit?: WorkflowCircuitDTO;
}

export interface CreateCategorieDTO {
  nom: string;
  description?: string;
  workflowCircuitId: number; // obligatoire
}

export interface UpdateCategorieDTO {
  nom?: string;
  description?: string;
  workflowCircuitId?: number;
}
