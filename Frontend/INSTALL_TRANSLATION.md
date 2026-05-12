# Installation et Configuration de la Traduction

## Étape 1: Installation des packages

Dans le dossier `Frontend`, exécutez:

```bash
npm install @ngx-translate/core @ngx-translate/http-loader
```

## Étape 2: Vérification des fichiers

Les fichiers suivants ont déjà été créés/modifiés:

✅ `Frontend/src/assets/i18n/fr.json` - Traductions françaises
✅ `Frontend/src/assets/i18n/en.json` - Traductions anglaises  
✅ `Frontend/src/app/app.config.ts` - Configuration de ngx-translate
✅ `Frontend/src/app/core/services/language.service.ts` - Service de langue mis à jour
✅ `Frontend/src/app/shared/components/layout/layout.component.ts` - Layout mis à jour
✅ `Frontend/src/app/modules/settings/settings.component.ts` - Settings mis à jour

## Étape 3: Redémarrer l'application

Après l'installation des packages, redémarrez le serveur de développement:

```bash
npm start
```

## Utilisation dans les composants

### 1. Importer TranslateModule

Dans chaque composant standalone qui utilise des traductions:

```typescript
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-my-component',
  standalone: true,
  imports: [CommonModule, TranslateModule], // Ajouter TranslateModule
  // ...
})
```

### 2. Utiliser dans les templates

```html
<!-- Traduction simple -->
<h1>{{ 'settings.title' | translate }}</h1>

<!-- Avec interpolation -->
<p>{{ 'layout.hello' | translate }}, {{ userName }}</p>

<!-- Dans les attributs -->
<button [title]="'common.save' | translate">
  {{ 'common.save' | translate }}
</button>
```

### 3. Utiliser dans le TypeScript

```typescript
import { TranslateService } from '@ngx-translate/core';

constructor(private translate: TranslateService) {}

ngOnInit() {
  // Traduction instantanée
  const title = this.translate.instant('dashboard.title');
  
  // Traduction avec observable (réactive)
  this.translate.get('common.success').subscribe(text => {
    this.toastr.success(text);
  });
}
```

## Structure des clés de traduction

Les traductions sont organisées par domaine:

- `nav.*` - Navigation (menu, liens)
- `common.*` - Éléments communs (boutons, actions)
- `layout.*` - Layout (header, sidebar)
- `modal.*` - Modales
- `settings.*` - Page paramètres
- `dashboard.*` - Dashboard
- `besoins.*` - Module besoins
- `validations.*` - Module validations
- `users.*` - Module utilisateurs
- `roles.*` - Module rôles
- `workflow.*` - Module workflow
- `reporting.*` - Module reporting
- `logs.*` - Module logs
- `auth.*` - Authentification

## Ajouter de nouvelles traductions

1. Ouvrez `Frontend/src/assets/i18n/fr.json`
2. Ajoutez votre clé dans la section appropriée:

```json
{
  "myModule": {
    "title": "Mon Module",
    "description": "Description de mon module"
  }
}
```

3. Faites de même dans `Frontend/src/assets/i18n/en.json`:

```json
{
  "myModule": {
    "title": "My Module",
    "description": "My module description"
  }
}
```

4. Utilisez dans votre template:

```html
<h1>{{ 'myModule.title' | translate }}</h1>
```

## Changer la langue

La langue se change automatiquement depuis la page Paramètres. Le `LanguageService` gère:

- La détection de la langue du navigateur au premier chargement
- La sauvegarde de la préférence dans localStorage
- Le changement de langue via `setLanguage()`
- La synchronisation avec ngx-translate

## Avantages de cette solution

✅ **Standard**: Solution la plus utilisée dans l'écosystème Angular
✅ **Scalable**: Facile d'ajouter de nouvelles langues
✅ **Maintenable**: Fichiers JSON séparés par langue
✅ **Performant**: Lazy loading des traductions
✅ **Réactif**: Changement de langue en temps réel
✅ **Type-safe**: Avec des outils comme `@ngneat/transloco-keys-manager`

## Prochaines étapes

Pour traduire toute l'application:

1. Identifiez les composants avec du texte en dur
2. Ajoutez `TranslateModule` dans leurs imports
3. Remplacez le texte par `{{ 'key' | translate }}`
4. Ajoutez les clés correspondantes dans fr.json et en.json

Exemple de composants à traduire:
- Dashboard
- Besoins (liste, détails, formulaire)
- Validations
- Utilisateurs
- Rôles
- Workflow
- Reporting
- Logs
- Profil
- Ma Signature

## Support

Pour plus d'informations sur ngx-translate:
- Documentation: https://github.com/ngx-translate/core
- Exemples: https://github.com/ngx-translate/example
