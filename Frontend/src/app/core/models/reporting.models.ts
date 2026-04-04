export interface DashboardDTO {
  besoinsEnAttente: number;
  besoinsSoumis: number;
  besoinsApprouves: number;
  besoinsRejetes: number;
  besoinsSignes: number;
  notificationsNonLues: number;
  derniersBesoins: BesoinRecent[];
}

export interface EvolutionPoint {
  date: string;
  recus: number;
  approuves: number;
  rejetes: number;
  enAttentePlus48h: number;
  tauxApprobation: number;
}

export interface BesoinRecent {
  id: number;
  titre: string;
  statut: string;
  dateCreation: string;
  dateModification: string;
}

export interface StatistiquesDTO {
  totalBesoins: number;
  totalUtilisateurs: number;
  totalActifs: number;
  besoinsByStatut: Record<string, number>;
  besoinsByCategorie: Record<string, number>;
  signaturesApposees: number;
  notificationsEnvoyees: number;
}

export interface FiltreRapportDTO {
  dateDebut?: string;
  dateFin?: string;
  statut?: string;
}

export interface ExportRequestDTO {
  format: 'PDF' | 'EXCEL';
  filtres?: FiltreRapportDTO;
}
