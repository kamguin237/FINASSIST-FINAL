export interface EtapeCircuitDTO {
  id: number;
  ordre: number;
  roleRequis: string;
  approbationRequise: boolean;
  signatureRequise: boolean;
  delaiMaxJours: number;
  estDerniereEtape: boolean;
  statutApres: string;
}

export interface WorkflowCircuitDTO {
  id: number;
  nom: string;
  description?: string;
  nomCreateur: string;
  dateCreation: string;
  dateModification: string;
  etapes: EtapeCircuitDTO[];
}

export interface CreateEtapeDTO {
  ordre: number;
  roleRequis: string;
  approbationRequise: boolean;
  signatureRequise: boolean;
  delaiMaxJours: number;
  estDerniereEtape: boolean;
}

export interface CreateWorkflowCircuitDTO {
  nom: string;
  description?: string;
  etapes: CreateEtapeDTO[];
}

export interface UpdateWorkflowCircuitDTO {
  nom?: string;
  description?: string;
  etapes?: CreateEtapeDTO[];
}

export interface ValiderBesoinDTO {
  decision: 'APPROUVE' | 'REJETE';
  motif?: string;
  commentaire?: string;
}
