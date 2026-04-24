using FinAssist.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FinAssist.Infrastructure.Services;

public class SignatureConfig(IConfiguration configuration) : ISignatureConfig
{
    public string Secret =>
        configuration["Signature:Secret"]
        ?? throw new InvalidOperationException("Clé Signature:Secret manquante dans la configuration.");
}
