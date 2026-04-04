export interface UtilisateurDTO {
  id: number;
  nom: string;
  prenom: string;
  email: string;
  role: string;
  dateCreation: string;
  actif: boolean;
}

export interface CreateUtilisateurDTO {
  nom: string;
  prenom: string;
  email: string;
  motDePasse: string;
  roleId: number;
  permissionsSupplementaires?: number[];
}

export interface UpdateUtilisateurDTO {
  nom?: string;
  prenom?: string;
  email?: string;
  roleId?: number;
  actif?: boolean;
}

export interface PermissionsEffectivesDTO {
  permissionsRole: string[];
  permissionsDirectes: string[];
  permissionsEffectives: string[];
}

export interface LogUtilisateurDTO {
  id: number;
  action: string;
  details: string;
  dateAction: string;
}
