import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of } from 'rxjs';

const mockHttp = { get: vi.fn(), post: vi.fn() };
const BASE_URL = 'http://localhost:4200/api/signatures';

function createService() {
  return {
    signerParBesoin: (besoinId: number, payload?: any) =>
      mockHttp.post(`${BASE_URL}/besoins/${besoinId}/signer`, payload ?? {}),
    signerParDocument: (documentId: number) =>
      mockHttp.post(`${BASE_URL}/${documentId}/signer`, {}),
    verifier: (id: number) =>
      mockHttp.get(`${BASE_URL}/${id}/verifier`),
    getApercuBesoin: (besoinId: number) =>
      mockHttp.get(`${BASE_URL}/besoins/${besoinId}`),
    telechargerDocumentSigne: (signatureId: number) =>
      mockHttp.get(`${BASE_URL}/${signatureId}/document-signe`)
  };
}

describe('SignaturesService — appels HTTP', () => {
  let service: ReturnType<typeof createService>;

  beforeEach(() => { vi.clearAllMocks(); service = createService(); });

  it('signerParBesoin appelle POST /signatures/besoins/:id/signer', () => {
    mockHttp.post.mockReturnValue(of({}));
    service.signerParBesoin(1);
    expect(mockHttp.post).toHaveBeenCalledWith(`${BASE_URL}/besoins/1/signer`, {});
  });

  it('signerParBesoin passe le payload si fourni', () => {
    const payload = { documentId: 5, signatureBase64: 'data:image/png;base64,abc' };
    mockHttp.post.mockReturnValue(of({}));
    service.signerParBesoin(1, payload);
    expect(mockHttp.post).toHaveBeenCalledWith(`${BASE_URL}/besoins/1/signer`, payload);
  });

  it('signerParDocument appelle POST /signatures/:id/signer', () => {
    mockHttp.post.mockReturnValue(of({}));
    service.signerParDocument(5);
    expect(mockHttp.post).toHaveBeenCalledWith(`${BASE_URL}/5/signer`, {});
  });

  it('verifier appelle GET /signatures/:id/verifier', () => {
    mockHttp.get.mockReturnValue(of({}));
    service.verifier(1);
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/1/verifier`);
  });

  it('getApercuBesoin appelle GET /signatures/besoins/:id', () => {
    mockHttp.get.mockReturnValue(of({}));
    service.getApercuBesoin(1);
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/besoins/1`);
  });

  it('telechargerDocumentSigne appelle GET /signatures/:id/document-signe', () => {
    mockHttp.get.mockReturnValue(of(new Blob()));
    service.telechargerDocumentSigne(1);
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/1/document-signe`);
  });
});
