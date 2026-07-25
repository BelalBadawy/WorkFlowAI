using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WFAI.API.Tests.Contracts;
using WFAI.API.Tests.Fixtures;

namespace WFAI.API.Tests.Endpoints;

[Collection("API collection")]
public class PhaseEndpointsTests : ApiTestBase
{
    public PhaseEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Get_phases_paged_should_return_ok_status()
    {
        var response = await Client.GetAsync("/api/v1/phases/paged?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
