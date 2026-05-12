# Styles des Date Pickers - FinAssist

## Vue d'ensemble

Les date pickers (sélecteurs de date) ont été stylisés pour offrir une expérience utilisateur cohérente et moderne dans toute l'application.

## Caractéristiques

### Design

- **Bordure colorée** - Bordure violette (accent color) avec opacité variable
- **Fond semi-transparent** - S'intègre naturellement avec le thème dark/light
- **Icône calendrier personnalisée** - SVG inline avec couleur accent
- **Transitions fluides** - Animations sur hover et focus
- **États interactifs** - Hover, focus avec feedback visuel

### Modes

#### Dark Mode (par défaut)
- Fond: `rgba(255, 255, 255, 0.05)`
- Texte: `#e5e7eb`
- Bordure: `rgba(124, 58, 237, 0.3)`
- Hover: Bordure plus opaque + fond plus clair
- Focus: Bordure pleine + shadow violette

#### Light Mode
- Fond: `#ffffff`
- Texte: `#1a1d23`
- Bordure: `rgba(124, 58, 237, 0.2)`
- Hover: Fond gris très clair
- Focus: Shadow violette subtile

## Utilisation

### HTML de base

```html
<input type="date" formControlName="dateDebut" />
```

### Avec wrapper et label stylisé

```html
<div class="field date-picker-wrapper">
  <label>Date de début</label>
  <input type="date" formControlName="dateDebut" />
</div>
```

Le wrapper `.date-picker-wrapper` ajoute:
- Label avec icône emoji 📅
- Couleur violette pour le label
- Espacement vertical optimisé

## Styles appliqués

### Input date

```scss
input[type="date"] {
  padding: 0.65rem 1rem;
  border-radius: 8px;
  border: 1px solid rgba(124, 58, 237, 0.3);
  background: rgba(255, 255, 255, 0.05);
  color: #e5e7eb;
  font-size: 0.9rem;
  cursor: pointer;
  transition: all 0.2s ease;
  min-width: 180px;
}
```

### Icône calendrier

L'icône du calendrier natif est remplacée par un SVG personnalisé:
- Couleur: Violet accent (#7c3aed)
- Taille: 20x20px
- Opacité: 0.8 (1.0 au hover)

## Composants utilisant les date pickers

### Reporting
**Fichier:** `Frontend/src/app/modules/reporting/reporting.component.html`

Deux date pickers pour filtrer les résultats:
- Date de début
- Date de fin

**Utilisation:**
```html
<div class="field date-picker-wrapper">
  <label>{{ 'reporting.startDate' | translate }}</label>
  <input formControlName="dateDebut" type="date" />
</div>
```

## Personnalisation

### Changer la couleur accent

Pour modifier la couleur des date pickers, changez la variable `--accent` ou modifiez directement les valeurs dans `styles.scss`:

```scss
input[type="date"] {
  border: 1px solid rgba(VOTRE_COULEUR, 0.3);
  
  &:focus {
    border-color: VOTRE_COULEUR;
    box-shadow: 0 0 0 3px rgba(VOTRE_COULEUR, 0.1);
  }
}
```

### Ajuster la taille

```scss
input[type="date"] {
  padding: 0.5rem 0.8rem;  // Plus compact
  font-size: 0.85rem;       // Texte plus petit
  min-width: 150px;         // Largeur minimale réduite
}
```

## Compatibilité navigateurs

### Chrome/Edge/Safari
- Support complet
- Icône calendrier personnalisée
- Tous les styles appliqués

### Firefox
- Support complet
- Icône calendrier native (non personnalisable)
- Styles de base appliqués

### Fallback
Pour les navigateurs ne supportant pas `input[type="date"]`, le champ se comporte comme un input texte standard avec les mêmes styles.

## Accessibilité

- **Contraste** - Ratio de contraste conforme WCAG AA
- **Focus visible** - Bordure et shadow au focus
- **Cursor** - Cursor pointer pour indiquer l'interactivité
- **Labels** - Labels associés pour les lecteurs d'écran

## États

### Normal
- Bordure violette semi-transparente
- Fond légèrement transparent

### Hover
- Bordure plus opaque
- Fond légèrement plus clair
- Icône calendrier opacité 100%

### Focus
- Bordure violette pleine
- Shadow violette 3px
- Fond plus clair
- Outline natif supprimé

### Disabled (si implémenté)
```scss
input[type="date"]:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  pointer-events: none;
}
```

## Exemples de code

### Formulaire de filtrage

```html
<form [formGroup]="filtreForm" class="filtre-form">
  <div class="field date-picker-wrapper">
    <label>Date de début</label>
    <input formControlName="dateDebut" type="date" />
  </div>
  
  <div class="field date-picker-wrapper">
    <label>Date de fin</label>
    <input formControlName="dateFin" type="date" />
  </div>
  
  <button type="submit" class="btn-search">
    Rechercher
  </button>
</form>
```

### TypeScript

```typescript
filtreForm = this.fb.group({
  dateDebut: [''],
  dateFin: ['']
});

onSubmit() {
  const { dateDebut, dateFin } = this.filtreForm.value;
  // Traitement des dates
}
```

## Améliorations futures

### Possibles ajouts

1. **Date range picker** - Sélection de plage de dates en un clic
2. **Presets** - Boutons rapides (Aujourd'hui, Cette semaine, Ce mois)
3. **Validation visuelle** - Indicateurs d'erreur intégrés
4. **Format personnalisé** - Affichage de la date dans différents formats
5. **Calendrier inline** - Alternative au popup natif

### Librairies tierces

Si besoin de fonctionnalités avancées, considérer:
- **Angular Material Datepicker** - Riche en fonctionnalités
- **ngx-daterangepicker-material** - Pour les plages de dates
- **flatpickr** - Léger et personnalisable

## Maintenance

### Fichiers concernés

- `Frontend/src/styles.scss` - Styles globaux des date pickers
- `Frontend/src/app/modules/reporting/reporting.component.scss` - Styles spécifiques
- `Frontend/src/app/modules/reporting/reporting.component.html` - Utilisation

### Tests recommandés

1. Tester dans Chrome, Firefox, Safari, Edge
2. Vérifier le mode dark et light
3. Tester le responsive (mobile/tablet)
4. Valider l'accessibilité (navigation clavier)
5. Vérifier les traductions des labels

## Support

Pour toute question ou amélioration, consulter:
- Documentation Angular Forms
- MDN Web Docs - input type="date"
- WCAG Guidelines pour l'accessibilité
