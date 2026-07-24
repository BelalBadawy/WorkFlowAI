using WFAI.Application.Features.Token.Queries;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.Token;

public class GetTokenQueryValidatorTests
{
    private readonly GetTokenQueryValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var query = new GetTokenQuery
        {
            TokenRequest = TestData.TokenRequest()
        };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_email_is_invalid()
    {
        var request = TestData.TokenRequest();
        request.Email = "not-an-email";
        var query = new GetTokenQuery { TokenRequest = request };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "TokenRequest.Email");
    }

    [Fact]
    public void Validate_should_fail_when_password_is_too_short()
    {
        var request = TestData.TokenRequest();
        request.Password = "123";
        var query = new GetTokenQuery { TokenRequest = request };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "TokenRequest.Password");
    }
}