using FinAssist.API.Middleware;
using FinAssist.Infrastructure.Services;
using FluentAssertions;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour les utilitaires d'infrastructure :
/// - UserAgentParserService : parsing de chaînes User-Agent réelles
/// - AuditMiddleware (méthodes statiques DeduireEntite / DeduireId via réflexion)
/// Ces services sont purs (sans dépendances externes) et testables directement.
/// </summary>
public class InfrastructureUtilitiesIntegrationTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // UserAgentParserService
    // ═══════════════════════════════════════════════════════════════════════════

    private static UserAgentParserService CreateParser() => new();

    // ── Parse — navigateurs courants ──────────────────────────────────────────

    [Fact]
    public void Parse_ChromeWindows_RetourneChromeetWindows()
    {
        // Arrange
        var parser = CreateParser();
        const string ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                          "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

        // Act
        var (os, navigateur) = parser.Parse(ua);

        // Assert
        os.Should().Contain("Windows");
        navigateur.Should().Contain("Chrome");
    }

    [Fact]
    public void Parse_FirefoxLinux_RetourneFirefoxEtLinux()
    {
        // Arrange
        var parser = CreateParser();
        const string ua = "Mozilla/5.0 (X11; Linux x86_64; rv:125.0) Gecko/20100101 Firefox/125.0";

        // Act
        var (os, navigateur) = parser.Parse(ua);

        // Assert
        os.Should().Contain("Linux");
        navigateur.Should().Contain("Firefox");
    }

    [Fact]
    public void Parse_SafariMacOS_RetourneSafariEtMac()
    {
        // Arrange
        var parser = CreateParser();
        const string ua = "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4_1) " +
                          "AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4.1 Safari/605.1.15";

        // Act
        var (os, navigateur) = parser.Parse(ua);

        // Assert
        os.Should().Contain("Mac");
        navigateur.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Parse_EdgeWindows_RetourneEdge()
    {
        // Arrange
        var parser = CreateParser();
        const string ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                          "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0";

        // Act
        var (os, navigateur) = parser.Parse(ua);

        // Assert
        os.Should().Contain("Windows");
        navigateur.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Parse_MobileAndroid_RetourneAndroid()
    {
        // Arrange
        var parser = CreateParser();
        const string ua = "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 " +
                          "(KHTML, like Gecko) Chrome/124.0.6367.82 Mobile Safari/537.36";

        // Act
        var (os, navigateur) = parser.Parse(ua);

        // Assert
        os.Should().Contain("Android");
        navigateur.Should().Contain("Chrome");
    }

    // ── Parse — cas limites ───────────────────────────────────────────────────

    [Fact]
    public void Parse_UserAgentNull_RetourneInconnu()
    {
        // Arrange
        var parser = CreateParser();

        // Act
        var (os, navigateur) = parser.Parse(null);

        // Assert
        os.Should().Be("Inconnu");
        navigateur.Should().Be("Inconnu");
    }

    [Fact]
    public void Parse_UserAgentVide_RetourneInconnu()
    {
        // Arrange
        var parser = CreateParser();

        // Act
        var (os, navigateur) = parser.Parse(string.Empty);

        // Assert
        os.Should().Be("Inconnu");
        navigateur.Should().Be("Inconnu");
    }

    [Fact]
    public void Parse_UserAgentEspacesSeuls_RetourneInconnu()
    {
        // Arrange
        var parser = CreateParser();

        // Act
        var (os, navigateur) = parser.Parse("   ");

        // Assert
        os.Should().Be("Inconnu");
        navigateur.Should().Be("Inconnu");
    }

    [Fact]
    public void Parse_UserAgentInconnu_RetourneValeurNonVide()
    {
        // Arrange
        var parser = CreateParser();
        const string ua = "MonApplicationCustome/1.0";

        // Act
        var (os, navigateur) = parser.Parse(ua);

        // Assert — le parser retourne quelque chose (même si "Other")
        os.Should().NotBeNull();
        navigateur.Should().NotBeNull();
    }

    [Fact]
    public void Parse_AppelMultiples_ResultatsCoherents()
    {
        // Arrange
        var parser = CreateParser();
        const string ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                          "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

        // Act — appeler plusieurs fois avec le même UA
        var (os1, nav1) = parser.Parse(ua);
        var (os2, nav2) = parser.Parse(ua);

        // Assert — résultats identiques (déterministe)
        os1.Should().Be(os2);
        nav1.Should().Be(nav2);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AuditMiddleware — DeduireEntite et DeduireId (via réflexion)
    // ═══════════════════════════════════════════════════════════════════════════

    // Accès aux méthodes statiques privées via réflexion
    private static string InvokeDeduireEntite(string path)
    {
        var method = typeof(AuditMiddleware).GetMethod(
            "DeduireEntite",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (string)method!.Invoke(null, [path])!;
    }

    private static int? InvokeDeduireId(string path)
    {
        var method = typeof(AuditMiddleware).GetMethod(
            "DeduireId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (int?)method!.Invoke(null, [path]);
    }

    // ── DeduireEntite ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("/api/besoins",       "Besoin")]
    [InlineData("/api/besoins/42",    "Besoin")]
    [InlineData("/api/users",         "Utilisateur")]
    [InlineData("/api/users/5",       "Utilisateur")]
    [InlineData("/api/roles",         "Role")]
    [InlineData("/api/permissions",   "Permission")]
    [InlineData("/api/workflow",      "Workflow")]
    [InlineData("/api/signatures",    "Signature")]
    [InlineData("/api/notifications", "Notification")]
    [InlineData("/api/categories",    "Categorie")]
    [InlineData("/api/reporting",     "Reporting")]
    [InlineData("/api/logs",          "Log")]
    public void DeduireEntite_PathsConnus_RetourneEntiteCorrecte(string path, string entiteAttendue)
    {
        var result = InvokeDeduireEntite(path);
        result.Should().Be(entiteAttendue);
    }

    [Fact]
    public void DeduireEntite_PathInconnu_RetourneSegment()
    {
        // Arrange — segment non mappé
        var result = InvokeDeduireEntite("/api/unknown-resource");

        // Assert — retourne le segment tel quel
        result.Should().Be("unknown-resource");
    }

    [Fact]
    public void DeduireEntite_PathCourt_RetourneInconnu()
    {
        // Arrange — path sans segment suffisant
        var result = InvokeDeduireEntite("/api");

        // Assert
        result.Should().Be("Inconnu");
    }

    [Fact]
    public void DeduireEntite_PathVide_RetourneInconnu()
    {
        var result = InvokeDeduireEntite("/");
        result.Should().Be("Inconnu");
    }

    // ── DeduireId ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("/api/besoins/42",    42)]
    [InlineData("/api/users/100",     100)]
    [InlineData("/api/roles/7",       7)]
    [InlineData("/api/besoins/1",     1)]
    public void DeduireId_PathAvecId_RetourneId(string path, int idAttendu)
    {
        var result = InvokeDeduireId(path);
        result.Should().Be(idAttendu);
    }

    [Theory]
    [InlineData("/api/besoins")]
    [InlineData("/api/besoins/action")]
    [InlineData("/api")]
    [InlineData("/")]
    public void DeduireId_PathSansId_RetourneNull(string path)
    {
        var result = InvokeDeduireId(path);
        result.Should().BeNull();
    }

    [Fact]
    public void DeduireId_PathAvecIdEtSousRessource_RetourneId()
    {
        // /api/besoins/42/documents → id = 42
        var result = InvokeDeduireId("/api/besoins/42/documents");
        result.Should().Be(42);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GeoIpService — logique locale (sans appel HTTP)
    // ═══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("localhost")]
    [InlineData("192.168.1.100")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    public async Task GeoIpService_IpLocale_RetourneLocal(string ip)
    {
        // Arrange — GeoIpService avec un HttpClient factice (ne sera pas appelé pour les IPs locales)
        var httpClient = new HttpClient();
        var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var service = new GeoIpService(httpClient, cache);

        // Act
        var (pays, ville) = await service.GetLocalisationAsync(ip);

        // Assert — IPs locales/privées → "Local"
        pays.Should().Be("Local");
        ville.Should().Be("Local");
    }

    [Fact]
    public async Task GeoIpService_IpNullOuVide_RetourneLocal()
    {
        // Arrange
        var httpClient = new HttpClient();
        var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var service = new GeoIpService(httpClient, cache);

        // Act
        var (pays1, ville1) = await service.GetLocalisationAsync(null);
        var (pays2, ville2) = await service.GetLocalisationAsync(string.Empty);

        // Assert
        pays1.Should().Be("Local");
        pays2.Should().Be("Local");
    }
}
