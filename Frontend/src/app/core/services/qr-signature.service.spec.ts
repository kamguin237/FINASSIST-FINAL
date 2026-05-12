import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of } from 'rxjs';

const mockHttp = { get: vi.fn(), post: vi.fn() };
const BASE_URL = 'http://localhost:4200/api/signatures/qr';

function createQrSignatureService() {
  return {
    createSession: () => mockHttp.post(`${BASE_URL}/session`, {}),
    getStatus: (token: string) => mockHttp.get(`${BASE_URL}/session/${token}/status`),
    getSessionInfo: (token: string) => mockHttp.get(`${BASE_URL}/session/${token}`),
    submitSignature: (token: string, imageBase64: string) =>
      mockHttp.post(`${BASE_URL}/session/${token}/submit`, { imageBase64 })
  };
}

describe('QrSignatureService — appels HTTP', () => {
  let service: ReturnType<typeof createQrSignatureService>;

  beforeEach(() => {
    vi.clearAllMocks();
    service = createQrSignatureService();
  });

  it('createSession appelle POST /signatures/qr/session', () => {
    mockHttp.post.mockReturnValue(of({ token: 'abc', urlMobile: 'https://...', expiration: '' }));
    service.createSession();
    expect(mockHttp.post).toHaveBeenCalledWith(`${BASE_URL}/session`, {});
  });

  it('getStatus appelle GET /signatures/qr/session/:token/status', () => {
    mockHttp.get.mockReturnValue(of({ completed: false, expired: false }));
    service.getStatus('abc123');
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/session/abc123/status`);
  });

  it('getSessionInfo appelle GET /signatures/qr/session/:token', () => {
    mockHttp.get.mockReturnValue(of({ nom: 'Dupont', prenom: 'Jean', role: 'Responsable' }));
    service.getSessionInfo('abc123');
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/session/abc123`);
  });

  it('submitSignature appelle POST /signatures/qr/session/:token/submit', () => {
    mockHttp.post.mockReturnValue(of({ message: 'OK' }));
    service.submitSignature('abc123', 'data:image/png;base64,abc');
    expect(mockHttp.post).toHaveBeenCalledWith(
      `${BASE_URL}/session/abc123/submit`,
      { imageBase64: 'data:image/png;base64,abc' }
    );
  });
});
