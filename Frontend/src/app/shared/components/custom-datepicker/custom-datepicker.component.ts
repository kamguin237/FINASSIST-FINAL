import { Component, Input, Output, EventEmitter, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-custom-datepicker',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './custom-datepicker.component.html',
  styleUrl: './custom-datepicker.component.scss'
})
export class CustomDatepickerComponent {
  @Input() value: string = '';
  @Input() placeholder: string = 'Sélectionner une date';
  @Input() label: string = '';
  @Output() valueChange = new EventEmitter<string>();

  showCalendar = signal(false);
  currentDate = signal(new Date());
  selectedDate = signal<Date | null>(null);

  currentMonth = computed(() => this.currentDate().getMonth());
  currentYear = computed(() => this.currentDate().getFullYear());

  monthNames = [
    'Janvier', 'Février', 'Mars', 'Avril', 'Mai', 'Juin',
    'Juillet', 'Août', 'Septembre', 'Octobre', 'Novembre', 'Décembre'
  ];

  dayNames = ['Lu', 'Ma', 'Me', 'Je', 'Ve', 'Sa', 'Di'];

  ngOnInit() {
    if (this.value) {
      this.selectedDate.set(new Date(this.value));
      this.currentDate.set(new Date(this.value));
    }
  }

  get displayValue(): string {
    if (!this.selectedDate()) return '';
    const date = this.selectedDate()!;
    return `${String(date.getDate()).padStart(2, '0')}/${String(date.getMonth() + 1).padStart(2, '0')}/${date.getFullYear()}`;
  }

  get calendarDays(): (number | null)[] {
    const year = this.currentYear();
    const month = this.currentMonth();
    
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    
    let startDay = firstDay.getDay();
    startDay = startDay === 0 ? 6 : startDay - 1;
    
    const days: (number | null)[] = [];
    
    for (let i = 0; i < startDay; i++) {
      days.push(null);
    }
    
    for (let i = 1; i <= lastDay.getDate(); i++) {
      days.push(i);
    }
    
    return days;
  }

  toggleCalendar() {
    this.showCalendar.update(v => !v);
  }

  closeCalendar() {
    this.showCalendar.set(false);
  }

  previousMonth() {
    const current = this.currentDate();
    this.currentDate.set(new Date(current.getFullYear(), current.getMonth() - 1, 1));
  }

  nextMonth() {
    const current = this.currentDate();
    this.currentDate.set(new Date(current.getFullYear(), current.getMonth() + 1, 1));
  }

  selectDay(day: number | null) {
    if (day === null) return;
    
    const selected = new Date(this.currentYear(), this.currentMonth(), day);
    this.selectedDate.set(selected);
    
    const formatted = `${selected.getFullYear()}-${String(selected.getMonth() + 1).padStart(2, '0')}-${String(selected.getDate()).padStart(2, '0')}`;
    this.valueChange.emit(formatted);
    this.closeCalendar();
  }

  isToday(day: number | null): boolean {
    if (day === null) return false;
    const today = new Date();
    return day === today.getDate() && 
           this.currentMonth() === today.getMonth() && 
           this.currentYear() === today.getFullYear();
  }

  isSelected(day: number | null): boolean {
    if (day === null || !this.selectedDate()) return false;
    const selected = this.selectedDate()!;
    return day === selected.getDate() && 
           this.currentMonth() === selected.getMonth() && 
           this.currentYear() === selected.getFullYear();
  }

  selectToday() {
    const today = new Date();
    this.selectedDate.set(today);
    this.currentDate.set(today);
    
    const formatted = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;
    this.valueChange.emit(formatted);
    this.closeCalendar();
  }

  clear() {
    this.selectedDate.set(null);
    this.valueChange.emit('');
    this.closeCalendar();
  }
}
