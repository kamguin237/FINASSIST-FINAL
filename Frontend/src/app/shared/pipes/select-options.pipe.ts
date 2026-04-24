import { Pipe, PipeTransform } from '@angular/core';
import { SelectOption } from '../components/custom-select/custom-select.component';

/** Transforme string[] (niveaux) → SelectOption[] */
@Pipe({ name: 'niveauOptions', standalone: true, pure: true })
export class NiveauOptionsPipe implements PipeTransform {
  transform(niveaux: readonly string[]): SelectOption[] {
    return (niveaux ?? []).map(n => ({ value: n, label: n }));
  }
}

/** Transforme CategorieDTO[] → SelectOption[] */
@Pipe({ name: 'categorieOptions', standalone: true, pure: true })
export class CategorieOptionsPipe implements PipeTransform {
  transform(categories: { id: number; nom: string }[]): SelectOption[] {
    return (categories ?? []).map(c => ({ value: c.id, label: c.nom }));
  }
}

/** Transforme RoleDTO[] → SelectOption[] (exclut Administrateur) */
@Pipe({ name: 'roleOptions', standalone: true, pure: true })
export class RoleOptionsPipe implements PipeTransform {
  transform(roles: { id: number; code: string }[]): SelectOption[] {
    return (roles ?? [])
      .filter(r => r.code !== 'Administrateur')
      .map(r => ({ value: r.id, label: r.code }));
  }
}

/** Transforme WorkflowCircuitDTO[] → SelectOption[] */
@Pipe({ name: 'circuitOptions', standalone: true, pure: true })
export class CircuitOptionsPipe implements PipeTransform {
  transform(circuits: { id: number; nom: string }[]): SelectOption[] {
    return (circuits ?? []).map(c => ({ value: c.id, label: c.nom }));
  }
}
