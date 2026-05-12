import { describe, it, expect, beforeEach } from 'vitest';

// ── Tests logique CustomSelectComponent ──────────────────────────────────────

interface SelectOption {
  value: string | number | null;
  label: string;
}

// Simulation de la logique du composant sans DI Angular
function createSelectLogic(options: SelectOption[], selected: string | number | null = null, placeholder = 'Sélectionner') {
  let _selected = selected;
  let _isOpen = false;
  const emitted: (string | number | null)[] = [];

  return {
    get selected() { return _selected; },
    get isOpen() { return _isOpen; },
    get emitted() { return emitted; },
    options,
    placeholder,
    disabled: false,

    get selectedLabel(): string {
      return options.find(o => o.value === _selected)?.label ?? placeholder;
    },

    toggle() {
      if (this.disabled) return;
      _isOpen = !_isOpen;
    },

    select(value: string | number | null) {
      _selected = value;
      emitted.push(value);
      _isOpen = false;
    },

    isSelected(option: SelectOption): boolean {
      return _selected === option.value;
    },

    close() { _isOpen = false; }
  };
}

describe('CustomSelectComponent — logique', () => {

  const options: SelectOption[] = [
    { value: 'BROUILLON', label: 'Brouillon' },
    { value: 'ENREGISTRE', label: 'Enregistré' },
    { value: 'TERMINE', label: 'Terminé' }
  ];

  // ── selectedLabel ───────────────────────────────────────────────────────────

  it('selectedLabel retourne le placeholder si rien n\'est sélectionné', () => {
    const select = createSelectLogic(options, null, 'Choisir...');
    expect(select.selectedLabel).toBe('Choisir...');
  });

  it('selectedLabel retourne le label de l\'option sélectionnée', () => {
    const select = createSelectLogic(options, 'BROUILLON');
    expect(select.selectedLabel).toBe('Brouillon');
  });

  it('selectedLabel retourne le placeholder si la valeur ne correspond à aucune option', () => {
    const select = createSelectLogic(options, 'INCONNU', 'Sélectionner');
    expect(select.selectedLabel).toBe('Sélectionner');
  });

  // ── toggle ──────────────────────────────────────────────────────────────────

  it('toggle ouvre le dropdown', () => {
    const select = createSelectLogic(options);
    expect(select.isOpen).toBe(false);
    select.toggle();
    expect(select.isOpen).toBe(true);
  });

  it('toggle ferme le dropdown si déjà ouvert', () => {
    const select = createSelectLogic(options);
    select.toggle();
    select.toggle();
    expect(select.isOpen).toBe(false);
  });

  it('toggle ne fait rien si disabled', () => {
    const select = createSelectLogic(options);
    select.disabled = true;
    select.toggle();
    expect(select.isOpen).toBe(false);
  });

  // ── select ──────────────────────────────────────────────────────────────────

  it('select met à jour la valeur sélectionnée', () => {
    const select = createSelectLogic(options);
    select.select('ENREGISTRE');
    expect(select.selected).toBe('ENREGISTRE');
  });

  it('select ferme le dropdown', () => {
    const select = createSelectLogic(options);
    select.toggle(); // ouvrir
    select.select('BROUILLON');
    expect(select.isOpen).toBe(false);
  });

  it('select émet la valeur choisie', () => {
    const select = createSelectLogic(options);
    select.select('TERMINE');
    expect(select.emitted).toContain('TERMINE');
  });

  it('select accepte null comme valeur', () => {
    const select = createSelectLogic(options, 'BROUILLON');
    select.select(null);
    expect(select.selected).toBeNull();
  });

  // ── isSelected ──────────────────────────────────────────────────────────────

  it('isSelected retourne true pour l\'option sélectionnée', () => {
    const select = createSelectLogic(options, 'BROUILLON');
    expect(select.isSelected({ value: 'BROUILLON', label: 'Brouillon' })).toBe(true);
  });

  it('isSelected retourne false pour les autres options', () => {
    const select = createSelectLogic(options, 'BROUILLON');
    expect(select.isSelected({ value: 'ENREGISTRE', label: 'Enregistré' })).toBe(false);
  });

  it('isSelected retourne false si rien n\'est sélectionné', () => {
    const select = createSelectLogic(options, null);
    expect(select.isSelected({ value: 'BROUILLON', label: 'Brouillon' })).toBe(false);
  });

  // ── options numériques ──────────────────────────────────────────────────────

  it('fonctionne avec des valeurs numériques', () => {
    const numOptions: SelectOption[] = [
      { value: 1, label: 'Option 1' },
      { value: 2, label: 'Option 2' }
    ];
    const select = createSelectLogic(numOptions, 1);
    expect(select.selectedLabel).toBe('Option 1');
    expect(select.isSelected({ value: 1, label: 'Option 1' })).toBe(true);
  });
});
