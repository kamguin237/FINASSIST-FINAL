export interface RoleDTO {
  id: number;
  code: string;
  description?: string;
  dateCreation: string;
  dateModification: string;
}

export interface CreateRoleDTO {
  code: string;
  description?: string;
}

export interface PermissionDTO {
  id: number;
  code: string;
  description?: string;
  fonctionnalite?: string;
  module?: string;
  dateCreation: string;
  dateModification: string;
}

export interface AssignerPermissionsDTO {
  permissionIds: number[];
}
