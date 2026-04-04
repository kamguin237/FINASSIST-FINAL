export interface BesoinDTO {
  id: number;
  titre: string;
  description: string;
  statut: string;
  niveauImportance: string;
  dateCreation: string;
  dateModification: string;
  utilisateurId: number;
  utilisateurNom: string;
  categorieId: number;
  categorieNom: string;
  estTermine: boolean;
}

export interface CreateBesoinDTO {
  titre: string;
  description: string;
  niveauImportance: string;
  categorieId: number;
}

export interface UpdateBesoinDTO {
  titre?: string;
  description?: string;
  niveauImportance?: string;
  categorieId?: number;
}

export interface HistoriqueDTO {
  id: number;
  action: string;
  description: string;
  dateAction: string;
}

export interface DocumentDTO {
  id: number;
  nom: string;
  type: string;
  checksum: string;
  dateCreation: string;
}

export const NIVEAUX_IMPORTANCE = ['FAIBLE', 'MOYEN', 'ELEVE', 'CRITIQUE'] as const;

export const STATUTS_BESOIN = {
  BROUILLON: 'BROUILLON',
  ENREGISTRE: 'ENREGISTRE',
  EN_ATTENTE: 'EN_ATTENTE',
  TRANSMIS: 'TRANSMIS',
  TERMINE: 'TERMINE',
} as const;
