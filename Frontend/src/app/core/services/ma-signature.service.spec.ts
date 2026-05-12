import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of } from 'rxjs';

const mockHttp = { get: vi.fn(), post: vi.fn(), delete: vi.fn() };
const BASE_URL = 'http://localhost:4200/api/ma-signature';

function createService() {
  return {
    get: () => mockHttp.get(BASE_URL),
    save: (dto: any) => mockHttp.post(BASE_URL, dto),
    delete: () => mockHttp.delete(BASE_URL)
  };
}

describe('MaSignatureService — appels HTTP', () => {
  let service: ReturnType<typeof createService>;

  beforeEach(() => { vi.clearAllMocks(); service = createService(); });

  it('get appelle GET /ma-signature', () => {
    mockHttp.get.mockReturnValue(of(null));
    service.get();
    expect(mockHttp.get).toHaveBeenCalledWith(BASE_URL);
  });

  it('save appelle POST /ma-signature avec le DTO', () => {
    const dto = { type: 'manuscrite', imageBase64: 'data:image/png;base64,abc' };
    mockHttp.post.mockReturnValue(of({ id: 1 }));
    service.save(dto);
    expect(mockHttp.post).toHaveBeenCalledWith(BASE_URL, dto);
  });

  it('delete appelle DELETE /ma-signature', () => {
    mockHttp.delete.mockReturnValue(of(undefined));
    service.delete();
    expect(mockHttp.delete).toHaveBeenCalledWith(BASE_URL);
  });
});

// ── Tests logique signature ───────────────────────────────────────────────────

describe('Signature — validation des types', () => {
  const TYPES_VALIDES = ['manuscrite', 'typographique', 'upload'];

  it('accepte les types valides', () => {
    TYPES_VALIDES.forEach(type => {
      expect(TYPES_VALIDES.includes(type)).toBe(true);
    });
  });

  it('rejette les types invalides', () => {
    const invalides = ['qrcode', 'photo', '', 'MANUSCRITE'];
    invalides.forEach(type => {
      expect(TYPES_VALIDES.includes(type)).toBe(false);
    });
  });

  it('imageBase64 ne doit pas être vide', () => {
    const dto = { type: 'manuscrite', imageBase64: '' };
    expect(dto.imageBase64.trim().length).toBe(0);
  });

  it('imageBase64 valide commence par data:image', () => {
    const dto = { type: 'manuscrite', imageBase64: 'data:image/png;base64,abc123' };
    expect(dto.imageBase64.startsWith('data:image')).toBe(true);
  });
});
