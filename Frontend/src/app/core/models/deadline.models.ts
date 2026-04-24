export interface BesoinDeadlineDTO {
  id: number;
  titre: string;
  statut: string;
  categorieNom: string;
  utilisateurNom: string;
  dateEntreeEnAttente: string | null;
  delaiMaxMinutes: number;
  pourcentageEcoule: number;
  minutesRestantes: number;
  rappel1Envoye: boolean;
  rappel2Envoye: boolean;
  emailRappelEnvoye: boolean;
  rejeteAutomatiquement: boolean;
  etapeRole: string;
  urgence: 'normal' | 'warning' | 'danger' | 'expired';
}
