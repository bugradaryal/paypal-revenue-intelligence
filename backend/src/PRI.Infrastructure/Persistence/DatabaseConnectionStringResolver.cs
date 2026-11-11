using Microsoft.Extensions.Configuration;
using Npgsql;

namespace PRI.Infrastructure.Persistence;

internal static class DatabaseConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var configuredValue = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            configuredValue = configuration["DATABASE_URL"];
        }

        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            throw new InvalidOperationException(
                "Configure ConnectionStrings:Default or DATABASE_URL with a PostgreSQL connection.");
        }

        if (!Uri.TryCreate(configuredValue, UriKind.Absolute, out var databaseUri)
            || (databaseUri.Scheme != "postgres" && databaseUri.Scheme != "postgresql"))
        {
            return configuredValue;
        }

        var credentials = databaseUri.UserInfo.Split(':', 2);
        var databaseName = Uri.UnescapeDataString(databaseUri.AbsolutePath.TrimStart('/'));
        if (credentials.Length != 2 || string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("DATABASE_URL must include a database name, username, and password.");
        }

        return new NpgsqlConnectionStringBuilder
        {
            Host = databaseUri.Host,
            Port = databaseUri.IsDefaultPort ? 5432 : databaseUri.Port,
            Database = databaseName,
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            SslMode = SslMode.Require
        }.ConnectionString;
    }
}
