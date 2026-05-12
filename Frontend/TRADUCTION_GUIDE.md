# Guide d'utilisation de la traduction

## ✅ Installation terminée

Les packages `@ngx-translate/core` et `@ngx-translate/http-loader` sont maintenant installés et configurés.

## 🎯 Comment ça fonctionne

### 1. Changement de langue

Allez dans **Paramètres** → Section **Apparence** → **Langue**

Sélectionnez:
- 🇫🇷 Français
- 🇬🇧 English

Le changement est **immédiat** et affecte toute l'interface!

### 2. Ce qui est déjà traduit

✅ **Navigation** (sidebar)
- Dashboard, Besoins, Validations, Workflow, etc.

✅ **Layout** (header, modales)
- Notifications, déconnexion, modales de confirmation

✅ **Page Paramètres**
- Tous les titres et labels

### 3. Pour traduire d'autres composants

#### Étape 1: Importer TranslateModule

Dans votre composant TypeScript:

```typescript
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-mon-composant',
  standalone: true,
  imports: [CommonModule, TranslateModule], // ← Ajouter ici
  // ...
})
```

#### Étape 2: Utiliser dans le template

```html
<!-- Avant -->
<h1>Tableau de bord</h1>
<button>Enregistrer</button>

<!-- Après -->
<h1>{{ 'dashboard.title' | translate }}</h1>
<button>{{ 'common.save' | translate }}</button>
```

#### Étape 3: Ajouter les traductions

Dans `Frontend/src/assets/i18n/fr.json`:
```json
{
  "dashboard": {
    "title": "Tableau de bord"
  },
  "common": {
    "save": "Enregistrer"
  }
}
```

Dans `Frontend/src/assets/i18n/en.json`:
```json
{
  "dashboard": {
    "title": "Dashboard"
  },
  "common": {
    "save": "Save"
  }
}
```

## 📚 Clés de traduction disponibles

Plus de 200 clés sont déjà définies dans les fichiers JSON:

### Navigation (`nav.*`)
- `nav.dashboard` → "Tableau de bord" / "Dashboard"
- `nav.besoins` → "Besoins" / "Requests"
- `nav.users` → "Utilisateurs" / "Users"
- etc.

### Commun (`common.*`)
- `common.save` → "Enregistrer" / "Save"
- `common.cancel` → "Annuler" / "Cancel"
- `common.delete` → "Supprimer" / "Delete"
- `common.edit` → "Modifier" / "Edit"
- etc.

### Settings (`settings.*`)
- `settings.title` → "Paramètres" / "Settings"
- `settings.account` → "Compte" / "Account"
- `settings.language` → "Langue" / "Language"
- etc.

### Dashboard (`dashboard.*`)
- `dashboard.title` → "Tableau de bord" / "Dashboard"
- `dashboard.statistics` → "Statistiques" / "Statistics"
- etc.

### Besoins (`besoins.*`)
- `besoins.title` → "Besoins" / "Requests"
- `besoins.create` → "Créer un besoin" / "Create request"
- `besoins.status` → "Statut" / "Status"
- etc.

**Voir les fichiers complets:**
- `Frontend/src/assets/i18n/fr.json`
- `Frontend/src/assets/i18n/en.json`

## 🔧 Utilisation avancée

### Dans le TypeScript

```typescript
import { TranslateService } from '@ngx-translate/core';

constructor(private translate: TranslateService) {}

ngOnInit() {
  // Traduction instantanée
  const message = this.translate.instant('common.success');
  
  // Avec observable (réactif)
  this.translate.get('common.error').subscribe(text => {
    this.toastr.error(text);
  });
  
  // Avec paramètres
  this.translate.get('welcome.message', { name: 'John' }).subscribe(text => {
    console.log(text); // "Bienvenue, John"
  });
}
```

### Interpolation dans les traductions

Dans le JSON:
```json
{
  "welcome": {
    "message": "Bienvenue, {{name}}"
  }
}
```

Dans le template:
```html
<p>{{ 'welcome.message' | translate:{ name: userName } }}</p>
```

## 📝 Composants à traduire

Pour avoir une application 100% traduite, il faut ajouter `TranslateModule` et utiliser le pipe `translate` dans:

- [ ] Dashboard
- [ ] Besoins (liste, détails, formulaire)
- [ ] Validations dashboard
- [ ] Utilisateurs
- [ ] Rôles
- [ ] Permissions
- [ ] Workflow
- [ ] Reporting
- [ ] Logs
- [ ] Profil
- [ ] Ma Signature
- [ ] Notifications

## 🎨 Exemple complet

**Avant (texte en dur):**
```html
<div class="card">
  <h2>Mes besoins</h2>
  <p>Aucun besoin trouvé</p>
  <button>Créer un besoin</button>
</div>
```

**Après (traduit):**
```html
<div class="card">
  <h2>{{ 'besoins.myRequests' | translate }}</h2>
  <p>{{ 'besoins.noRequests' | translate }}</p>
  <button>{{ 'besoins.create' | translate }}</button>
</div>
```

## 🚀 Prochaines étapes

1. **Testez le changement de langue** dans Paramètres
2. **Identifiez les composants** avec du texte en dur
3. **Ajoutez TranslateModule** dans leurs imports
4. **Remplacez le texte** par des clés de traduction
5. **Ajoutez les traductions manquantes** dans fr.json et en.json

## 💡 Conseils

- Utilisez des clés descriptives: `besoins.create` plutôt que `btn1`
- Organisez par domaine: `nav.*`, `common.*`, `besoins.*`
- Réutilisez les clés communes: `common.save`, `common.cancel`
- Testez dans les deux langues après chaque modification

## 📖 Documentation

- [ngx-translate GitHub](https://github.com/ngx-translate/core)
- [Documentation officielle](https://github.com/ngx-translate/core#readme)

---

**La traduction est maintenant opérationnelle! Changez la langue dans Paramètres pour voir le résultat.**
