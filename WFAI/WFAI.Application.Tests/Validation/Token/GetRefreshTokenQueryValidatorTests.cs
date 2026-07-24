using WFAI.Application.Features.Token.Queries;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.Token;

public class GetRefreshTokenQueryValidatorTests
{
    private readonly GetRefreshTokenQueryValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var query = new GetRefreshTokenQuery
        {
            RefreshTokenRequest = TestData.RefreshTokenRequest()
        };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_token_is_missing()
    {
        var request = TestData.RefreshTokenRequest();
        request.Token = string.Empty;
        var query = new GetRefreshTokenQuery { RefreshTokenRequest = request };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "RefreshTokenRequest.Token");
    }

    [Fact]
    public void Validate_should_fail_when_refresh_token_is_missing()
    {
        var request = TestData.RefreshTokenRequest();
        request.RefreshToken = string.Empty;
        var query = new GetRefreshTokenQuery { RefreshTokenRequest = request };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "RefreshTokenRequest.RefreshToken");
    }
}