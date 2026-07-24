using System.Net;
using System.Net.Http.Json;
using WFAI.Application.Authorization;
using WFAI.API.Tests.Contracts;
using WFAI.API.Tests.Fixtures;
using WFAI.API.Tests.Support;

namespace WFAI.API.Tests.Endpoints;

[Collection("API collection")]
public class AuditTrailEndpointsTests : ApiTestBase
{
    public AuditTrailEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAuditLogsPaged_DefaultRequest_ReturnsSuccessfulOrDebugs400()
    {
        UsePrivilegedClient(AppPermission.NameFor(AppService.Identity, AppFeature.AuditTrails, AppAction.Read));

        const string route = "/api/v1/audit-logs/paged?pageNumber=1&pageSize=10&sortBy=datetime&sortDirection=desc";

        var response = await Client.GetAsync(route);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: $"API returned status {response.StatusCode} with content: {content}");
    }
}