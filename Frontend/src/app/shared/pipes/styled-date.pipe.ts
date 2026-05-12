import { Pipe, PipeTransform } from '@angular/core';
import { DatePipe } from '@angular/common';

@Pipe({
  name: 'styledDate',
  standalone: true
})
export class StyledDatePipe implements PipeTransform {
  private datePipe = new DatePipe('fr-FR');

  transform(value: any, format: 'date' | 'time' | 'datetime' = 'datetime'): string {
    if (!value) return '';

    let formattedDate: string;
    let cssClass: string;

    switch (format) {
      case 'date':
        formattedDate = this.datePipe.transform(value, 'dd/MM/yyyy') || '';
        cssClass = 'date-badge';
        break;
      case 'time':
        formattedDate = this.datePipe.transform(value, 'HH:mm') || '';
        cssClass = 'time-badge';
        break;
      case 'datetime':
      default:
        formattedDate = this.datePipe.transform(value, 'dd/MM/yyyy HH:mm') || '';
        cssClass = 'datetime-badge';
        break;
    }

    return `<span class="${cssClass}">${formattedDate}</span>`;
  }
}
