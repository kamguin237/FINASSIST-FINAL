export interface SignatureUtilisateurDTO {
  id: number;
  type: 'manuscrite' | 'typographique' | 'upload';
  imageBase64: string;
  police?: string;
  dateCreation: string;
  dateModification: string;
}

export interface SaveSignatureUtilisateurDTO {
  type: 'manuscrite' | 'typographique' | 'upload';
  imageBase64: string;
  police?: string;
}
