using CreditEngine.Domain;
using CreditEngine.Infrastructure.Enrichment;
using CreditEngine.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace CreditEngine.Api.Tests;

public sealed class FakeRegistry : ICompanyRegistry
{
    public Func<Cnpj, CompanyProfile?> Respond { get; set; } = _ => new CompanyProfile("ATIVA", new DateOnly(2015, 1, 1), "6201501");
    public Task<CompanyProfile?> FindAsync(Cnpj cnpj, CancellationToken ct) => Task.FromResult(Respond(cnpj));
}

public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:17").Build();
    public FakeRegistry Registry { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Postgres", _db.GetConnectionString());
        builder.UseSetting("Outbox:Enabled", "false");
        builder.ConfigureServices(s =>
        {
            s.RemoveAll<ICompanyRegistry>();
            s.AddSingleton<ICompanyRegistry>(Registry);
        });
    }

    public CreditDbContext Db() => Services.CreateScope().ServiceProvider.GetRequiredService<CreditDbContext>();

    public Task InitializeAsync() => _db.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}
