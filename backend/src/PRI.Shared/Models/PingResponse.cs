namespace PRI.Shared.Models;

public sealed record PingResponse(
    string Name,
    string Version,
    string Environment,
    string PaypalEnvironment,
    DateTime ServerTimeUtc);
