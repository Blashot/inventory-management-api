using Application.Abstractions.Data;
using Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Infrastructure.IntegrationTests;

public abstract class BaseIntegrationTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    private WebApplicationFactory<Web.Api.Program> _factory = null!;

    protected IServiceScope Scope { get; private set; } = null!;

    protected IApplicationDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _pgContainer.StartAsync();

#pragma warning disable CA2000
        _factory = new WebApplicationFactory<Web.Api.Program>()
#pragma warning restore CA2000
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.UseSetting(
                    "ConnectionStrings:Database",
                    _pgContainer.GetConnectionString());
            });

        Scope = _factory.Services.CreateScope();

        DbContext = Scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        ApplicationDbContext dbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        Scope.Dispose();
        await _factory.DisposeAsync();
        await _pgContainer.DisposeAsync();
    }
}

