import { Pipe, PipeTransform } from '@angular/core';
import { LanguageService } from '../../core/services/language.service';

@Pipe({
  name: 'translate',
  standalone: true,
  pure: false // Pour que le pipe se mette à jour quand la langue change
})
export class TranslatePipe implements PipeTransform {
  constructor(private langService: LanguageService) {}

  transform(key: string): string {
    return this.langService.t(key);
  }
}
