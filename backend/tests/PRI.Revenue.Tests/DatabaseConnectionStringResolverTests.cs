using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PRI.Infrastructure;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Revenue.Tests;

public sealed class DatabaseConnectionStringResolverTests
{
    [Fact]
    public void Render_database_url_is_converted_to_a_tls_enabled_connection_string()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_URL"] = "postgres://test-user:p%3Assword@db.example:5433/pri%20data"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var db = provider.GetRequiredService<AppDbContext>();
        var connection = Assert.IsType<NpgsqlConnection>(db.Database.GetDbConnection());
        var parsed = new NpgsqlConnectionStringBuilder(connection.ConnectionString);

        Assert.Equal("db.example", parsed.Host);
        Assert.Equal(5433, parsed.Port);
        Assert.Equal("pri data", parsed.Database);
        Assert.Equal("test-user", parsed.Username);
        Assert.Equal("p:ssword", parsed.Password);
        Assert.Equal(SslMode.Require, parsed.SslMode);
    }
}
