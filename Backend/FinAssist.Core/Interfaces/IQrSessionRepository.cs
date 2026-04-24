using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface IQrSessionRepository
{
    Task<QrSignatureSession> CreateAsync(QrSignatureSession session);
    Task<QrSignatureSession?> GetByTokenAsync(string token);
    Task UpdateAsync(QrSignatureSession session);
}
