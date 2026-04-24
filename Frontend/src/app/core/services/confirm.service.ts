import { Injectable, signal } from '@angular/core';

export interface ConfirmOptions {
  titre?: string;
  message: string;
  labelConfirm?: string;
  labelCancel?: string;
  danger?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ConfirmService {
  visible = signal(false);
  options = signal<ConfirmOptions>({ message: '' });

  private resolveFn: ((v: boolean) => void) | null = null;

  confirm(opts: ConfirmOptions): Promise<boolean> {
    this.options.set({ labelConfirm: 'Confirmer', labelCancel: 'Annuler', danger: true, ...opts });
    this.visible.set(true);
    return new Promise(resolve => { this.resolveFn = resolve; });
  }

  respond(value: boolean) {
    this.visible.set(false);
    this.resolveFn?.(value);
    this.resolveFn = null;
  }
}
