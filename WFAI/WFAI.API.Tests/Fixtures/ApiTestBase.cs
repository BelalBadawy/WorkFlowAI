using Microsoft.Extensions.DependencyInjection;
using WFAI.API.Tests.Support;

namespace WFAI.API.Tests.Fixtures;

public abstract class ApiTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected ApiTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateAnonymousClient();
        Verifier = new ApiStateVerifier(factory);
        Seeder = new ApiTestDataSeeder(factory);
    }

    protected CustomWebApplicationFactory Factory { get; }
    protected HttpClient Client { get; private set; }
    protected ApiStateVerifier Verifier { get; }
    protected ApiTestDataSeeder Seeder { get; }

    protected T GetRequiredService<T>() where T : notnull
    {
        using var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    protected void UseAnonymousClient()
    {
        Client.Dispose();
        Client = Factory.CreateAnonymousClient();
    }

    protected void UseLowPrivilegeClient(string requiredPermission)
    {
        Client.Dispose();
        Client = Factory.CreateLowPrivilegeClient(requiredPermission);
    }

    protected void UsePrivilegedClient(string requiredPermission)
    {
        Client.Dispose();
        Client = Factory.CreatePrivilegedClient(requiredPermission);
    }

    protected void UseSelfServiceClient(int userId)
    {
        Client.Dispose();
        Client = Factory.CreateSelfServiceClient(userId);
    }
}