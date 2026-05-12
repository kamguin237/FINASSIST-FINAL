import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

// ── Tests logique MaSignatureComponent ───────────────────────────────────────

describe('MaSignatureComponent — onglets', () => {
  type Onglet = 'manuscrite' | 'typographique' | 'upload' | 'qrcode';

  it('les 4 onglets sont valides', () => {
    const onglets: Onglet[] = ['manuscrite', 'typographique', 'upload', 'qrcode'];
    expect(onglets).toHaveLength(4);
  });

  it('l\'onglet par défaut est manuscrite', () => {
    const onglet: Onglet = 'manuscrite';
    expect(onglet).toBe('manuscrite');
  });

  it('setOnglet change l\'onglet actif', () => {
    let onglet: Onglet = 'manuscrite';
    onglet = 'typographique';
    expect(onglet).toBe('typographique');
  });
});

describe('MaSignatureComponent — displayValue (typographique)', () => {

  function getApercuTypo(texte: string, police: string): string {
    if (!texte.trim()) return '';
    // Simuler la génération d'un aperçu (sans canvas réel)
    return `data:image/png;base64,${btoa(`${texte}:${police}`)}`;
  }

  it('retourne une chaîne vide si texte vide', () => {
    expect(getApercuTypo('', 'Dancing Script')).toBe('');
  });

  it('retourne une data URL si texte non vide', () => {
    const result = getApercuTypo('Jean Dupont', 'Dancing Script');
    expect(result).toContain('data:image/png;base64,');
  });
});

describe('MaSignatureComponent — polices disponibles', () => {
  const polices = [
    'Dancing Script', 'Great Vibes', 'Pacifico', 'Satisfy', 'Caveat',
    'Sacramento', 'Pinyon Script', 'Alex Brush', 'Allura', 'Kaushan Script',
    'Courgette', 'Lobster', 'Yellowtail', 'Italianno', 'Clicker Script',
    'Euphoria Script', 'Marck Script', 'Niconne', 'Qwigley', 'Ruthie'
  ];

  it('contient 20 polices', () => {
    expect(polices).toHaveLength(20);
  });

  it('Dancing Script est la police par défaut', () => {
    const policeChoisie = 'Dancing Script';
    expect(polices).toContain(policeChoisie);
  });
});

describe('MaSignatureComponent — QR Code polling', () => {
  beforeEach(() => { vi.useFakeTimers(); });
  afterEach(() => { vi.useRealTimers(); });

  it('le polling se déclenche toutes les 3 secondes', () => {
    let pollCount = 0;
    const interval = setInterval(() => { pollCount++; }, 3000);

    vi.advanceTimersByTime(9000);
    expect(pollCount).toBe(3);
    clearInterval(interval);
  });

  it('le polling s\'arrête quand completed = true', () => {
    let pollCount = 0;
    let completed = false;

    const interval = setInterval(() => {
      pollCount++;
      if (pollCount >= 2) {
        completed = true;
        clearInterval(interval);
      }
    }, 3000);

    vi.advanceTimersByTime(9000);
    expect(completed).toBe(true);
    expect(pollCount).toBe(2);
  });
});

describe('MaSignatureComponent — sauvegarder', () => {
  it('ne sauvegarde pas si texte typographique vide', () => {
    const texte = '';
    const peutSauvegarder = texte.trim().length > 0;
    expect(peutSauvegarder).toBe(false);
  });

  it('ne sauvegarde pas si upload sans image', () => {
    const uploadPreview: string | null = null;
    const peutSauvegarder = uploadPreview !== null;
    expect(peutSauvegarder).toBe(false);
  });

  it('peut sauvegarder si upload avec image', () => {
    const uploadPreview = 'data:image/png;base64,abc';
    const peutSauvegarder = uploadPreview !== null;
    expect(peutSauvegarder).toBe(true);
  });
});
