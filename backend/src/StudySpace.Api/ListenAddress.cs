using System.Net;

namespace StudySpace.Api;

/// The API listens on this machine only (REQ-SEC-001). A configured address that is not loopback stops the start-up.
public static class ListenAddress
{
    public const string Default = "http://127.0.0.1:5080";

    public static IReadOnlyList<string> Resolve(IConfiguration configuration)
    {
        var configured = configuration["Urls"];
        var urls = string.IsNullOrWhiteSpace(configured)
            ? [Default]
            : configured.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var url in urls)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || uri.Scheme is not ("http" or "https")
                || !IsLoopbackHost(uri.Host))
            {
                throw new InvalidOperationException($"The API may only listen on a loopback address, but '{url}' was configured.");
            }
        }

        return urls;
    }

    private static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || (IPAddress.TryParse(host.Trim('[', ']'), out var ip) && IPAddress.IsLoopback(ip));
}
