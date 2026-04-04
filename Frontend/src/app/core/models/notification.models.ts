export interface NotificationDTO {
  id: number;
  message: string;
  type: string;
  dateEnvoi: string;
  lu: boolean;
}

export interface CreateNotificationDTO {
  message: string;
  type: string;
  utilisateurIds: number[];
}
