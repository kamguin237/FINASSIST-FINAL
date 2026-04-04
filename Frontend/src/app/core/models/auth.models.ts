export interface LoginRequest {
  email: string;
  motDePasse: string;
}

export interface UtilisateurInfo {
  id: number;
  nom: string;
  prenom: string;
  email: string;
  role: string;
}

export interface LoginResponse {
  accessToken: string;
  expiration: string;
  utilisateur: UtilisateurInfo;
}
