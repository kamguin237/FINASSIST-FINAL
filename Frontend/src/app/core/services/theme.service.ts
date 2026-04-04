import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly KEY = 'finassist_theme';
  isDark = signal<boolean>(this.loadTheme());

  private loadTheme(): boolean {
    return localStorage.getItem(this.KEY) !== 'light';
  }

  toggle() {
    const next = !this.isDark();
    this.isDark.set(next);
    localStorage.setItem(this.KEY, next ? 'dark' : 'light');
    this.apply(next);
  }

  apply(dark: boolean) {
    document.body.classList.toggle('light-mode', !dark);
  }

  init() {
    this.apply(this.isDark());
  }
}
