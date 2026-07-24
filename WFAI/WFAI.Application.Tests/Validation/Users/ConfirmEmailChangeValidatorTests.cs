using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.Users;

public class ConfirmEmailChangeValidatorTests
{
    private readonly ConfirmEmailChangeValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var command = new ConfirmEmailChangeCommand
        {
            ConfirmEmailChange = TestData.ConfirmEmailChangeRequest()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_user_id_is_zero()
    {
        var command = new ConfirmEmailChangeCommand
        {
            ConfirmEmailChange = TestData.ConfirmEmailChangeRequest(userId: 0)
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmEmailChange.UserId");
    }

    [Fact]
    public void Validate_should_fail_when_new_email_is_empty()
    {
        var request = TestData.ConfirmEmailChangeRequest();
        request.NewEmail = "";
        var command = new ConfirmEmailChangeCommand { ConfirmEmailChange = request };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmEmailChange.NewEmail");
    }

    [Fact]
    public void Validate_should_fail_when_new_email_is_invalid_format()
    {
        var request = TestData.ConfirmEmailChangeRequest();
        request.NewEmail = "not-an-email";
        var command = new ConfirmEmailChangeCommand { ConfirmEmailChange = request };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmEmailChange.NewEmail");
    }

    [Fact]
    public void Validate_should_fail_when_token_is_empty()
    {
        var request = TestData.ConfirmEmailChangeRequest();
        request.Token = "";
        var command = new ConfirmEmailChangeCommand { ConfirmEmailChange = request };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmEmailChange.Token");
    }
}