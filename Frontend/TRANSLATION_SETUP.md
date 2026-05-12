# Configuration de la traduction avec @ngx-translate

## Installation

Exécutez cette commande dans le dossier `Frontend`:

```bash
npm install @ngx-translate/core @ngx-translate/http-loader
```

## Configuration dans app.config.ts

Créez ou modifiez `Frontend/src/app/app.config.ts`:

```typescript
import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, HttpClient } from '@angular/common/http';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { routes } from './app.routes';

// Factory pour le loader de traductions
export function HttpLoaderFactory(http: HttpClient) {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(),
    importProvidersFrom(
      TranslateModule.forRoot({
        defaultLanguage: 'fr',
        loader: {
          provide: TranslateLoader,
          useFactory: HttpLoaderFactory,
          deps: [HttpClient]
        }
      })
    )
  ]
};
```

## Mise à jour du LanguageService

Le `LanguageService` doit maintenant utiliser `TranslateService` de ngx-translate:

```typescript
import { Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type Language = 'fr' | 'en';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly STORAGE_KEY = 'app-language';
  
  currentLanguage = signal<Language>(this.getInitialLanguage());

  constructor(private translate: TranslateService) {
    const lang = this.getInitialLanguage();
    this.translate.use(lang);
    this.currentLanguage.set(lang);
    document.documentElement.lang = lang;
  }

  private getInitialLanguage(): Language {
    const stored = localStorage.getItem(this.STORAGE_KEY) as Language;
    if (stored === 'fr' || stored === 'en') return stored;
    
    const browserLang = navigator.language.toLowerCase();
    return browserLang.startsWith('fr') ? 'fr' : 'en';
  }

  setLanguage(lang: Language) {
    this.currentLanguage.set(lang);
    this.translate.use(lang);
    localStorage.setItem(this.STORAGE_KEY, lang);
    document.documentElement.lang = lang;
  }

  t(key: string): string {
    return this.translate.instant(key);
  }
}
```

## Utilisation dans les templates

### Avec le pipe translate:
```html
<h1>{{ 'settings.title' | translate }}</h1>
<button>{{ 'common.save' | translate }}</button>
```

### Avec des paramètres:
```html
<p>{{ 'layout.hello' | translate }}, {{ userName }}</p>
```

### Dans le TypeScript:
```typescript
constructor(private translate: TranslateService) {}

ngOnInit() {
  this.title = this.translate.instant('dashboard.title');
  
  // Ou avec observable
  this.translate.get('common.success').subscribe(text => {
    console.log(text);
  });
}
```

## Mise à jour des composants

### Layout Component

Importer TranslateModule:
```typescript
import { TranslateModule } from '@ngx-translate/core';

@Component({
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, TranslateModule],
  // ...
})
```

Dans le template, remplacer les pipes `translate` par le pipe de ngx-translate (même syntaxe).

### Settings Component

```typescript
import { TranslateModule } from '@ngx-translate/core';

@Component({
  imports: [CommonModule, RouterLink, FormsModule, TranslateModule],
  // ...
})
```

## Avantages de ngx-translate

1. **Standard de l'industrie**: Solution la plus utilisée pour Angular
2. **Lazy loading**: Charge les traductions à la demande
3. **Interpolation**: Support des paramètres dynamiques
4. **Pluralisation**: Gestion automatique du singulier/pluriel
5. **Fallback**: Langue par défaut si traduction manquante
6. **Observable**: Réactivité automatique aux changements de langue
7. **AOT compatible**: Fonctionne avec la compilation ahead-of-time

## Structure des fichiers de traduction

Les fichiers JSON sont organisés par domaine:
- `nav.*`: Navigation
- `common.*`: Éléments communs
- `settings.*`: Page paramètres
- `besoins.*`: Module besoins
- etc.

Cela facilite la maintenance et l'ajout de nouvelles traductions.
