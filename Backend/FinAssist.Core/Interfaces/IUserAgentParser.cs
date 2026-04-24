namespace FinAssist.Core.Interfaces;

public interface IUserAgentParser
{
    (string os, string navigateur) Parse(string? userAgent);
}
