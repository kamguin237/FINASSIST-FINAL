namespace FinAssist.Core.Interfaces;

public interface IGeoIpService
{
    Task<(string pays, string ville)> GetLocalisationAsync(string? ip);
}
