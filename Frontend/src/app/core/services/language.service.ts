import { Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type Language = 'fr' | 'en';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly STORAGE_KEY = 'app-language';
  
  currentLanguage = signal<Language>(this.getInitialLanguage());

  constructor(private translate: TranslateService) {
    const lang = this.getInitialLanguage();
    this.translate.setDefaultLang('fr');
    this.translate.use(lang);
    this.currentLanguage.set(lang);
    document.documentElement.lang = lang;
  }

  private getInitialLanguage(): Language {
    const stored = localStorage.getItem(this.STORAGE_KEY) as Language;
    if (stored === 'fr' || stored === 'en') return stored;
    
    // Détecter la langue du navigateur
    const browserLang = navigator.language.toLowerCase();
    return browserLang.startsWith('fr') ? 'fr' : 'en';
  }

  setLanguage(lang: Language) {
    this.currentLanguage.set(lang);
    this.translate.use(lang);
    localStorage.setItem(this.STORAGE_KEY, lang);
    document.documentElement.lang = lang;
  }

  // Méthode helper pour traduction instantanée
  t(key: string): string {
    return this.translate.instant(key);
  }
}

