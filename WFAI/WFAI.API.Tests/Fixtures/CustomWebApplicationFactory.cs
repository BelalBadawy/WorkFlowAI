using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using WFAI.Application.Interfaces.Common;
using WFAI.Infrastructure.Persistence.Contexts;
using WFAI.API.Tests.Support;

namespace WFAI.API.Tests.Fixtures;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly object InitializationLock = new();
    private static Task? _databaseInitializationTask;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAuthenticationSchemeProvider>();
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<ApiTestEmailSink>();
            services.AddScoped<IEmailService, ApiTestEmailService>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = ApiTestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = ApiTestAuthenticationHandler.SchemeName;
                options.DefaultScheme = ApiTestAuthenticationHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, ApiTestAuthenticationHandler>(
                ApiTestAuthenticationHandler.SchemeName,
                _ => { });
        });
    }

    public async Task InitializeAsync()
    {
        await EnsureDatabaseInitializedAsync();
    }

    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    public HttpClient CreateAnonymousClient()
    {
        return CreateClient();
    }

    public HttpClient CreateLowPrivilegeClient(string requiredPermission)
    {
        var client = CreateClient();
        ApiTestAuthenticationHeaderHelper.ConfigureLowPrivilegeClient(client, requiredPermission);
        return client;
    }

    public HttpClient CreatePrivilegedClient(string requiredPermission)
    {
        var client = CreateClient();
        ApiTestAuthenticationHeaderHelper.ConfigurePrivilegedClient(client, requiredPermission);
        return client;
    }

    public HttpClient CreateSelfServiceClient(int userId)
    {
        var client = CreateClient();
        ApiTestAuthenticationHeaderHelper.ConfigureSelfServiceClient(client, userId);
        return client;
    }

    internal async Task EnsureDatabaseInitializedAsync()
    {
        Task initializationTask;

        lock (InitializationLock)
        {
            _databaseInitializationTask ??= ApiTestDatabaseInitializer.InitializeAsync(Services);
            initializationTask = _databaseInitializationTask;
        }

        await initializationTask;
    }
}