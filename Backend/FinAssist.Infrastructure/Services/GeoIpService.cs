using System.Net.Http.Json;
using FinAssist.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace FinAssist.Infrastructure.Services;

public class GeoIpService(HttpClient http, IMemoryCache cache) : IGeoIpService
{
    private static readonly HashSet<string> IpsLocales = new()
        { "127.0.0.1", "::1", "localhost" };

    public async Task<(string pays, string ville)> GetLocalisationAsync(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip) || IpsLocales.Contains(ip)
            || ip.StartsWith("192.168.") || ip.StartsWith("10.")
            || ip.StartsWith("172."))
            return ("Local", "Local");

        // Cache par IP pour respecter la limite 45 req/min de ip-api.com
        var cacheKey = $"geoip_{ip}";
        if (cache.TryGetValue(cacheKey, out (string pays, string ville) cached))
            return cached;

        try
        {
            var response = await http.GetFromJsonAsync<IpApiResponse>(
                $"http://ip-api.com/json/{ip}?fields=status,country,city&lang=fr");

            var result = response?.Status == "success"
                ? (response.Country ?? "Inconnu", response.City ?? "Inconnu")
                : ("Inconnu", "Inconnu");

            cache.Set(cacheKey, result, TimeSpan.FromHours(24));
            return result;
        }
        catch
        {
            return ("Inconnu", "Inconnu");
        }
    }

    private record IpApiResponse(string? Status, string? Country, string? City);
}
