using FinAssist.Core.Interfaces;
using UAParser;

namespace FinAssist.Infrastructure.Services;

public class UserAgentParserService : IUserAgentParser
{
    private readonly Parser _parser = Parser.GetDefault();

    public (string os, string navigateur) Parse(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return ("Inconnu", "Inconnu");

        var client = _parser.Parse(userAgent);

        var os  = $"{client.OS.Family} {client.OS.Major}".Trim();
        var nav = $"{client.UA.Family} {client.UA.Major}".Trim();

        return (
            string.IsNullOrWhiteSpace(os)  ? "Inconnu" : os,
            string.IsNullOrWhiteSpace(nav) ? "Inconnu" : nav
        );
    }
}
