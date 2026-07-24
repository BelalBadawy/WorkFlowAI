using FluentValidation;
using Mediator;
using Moq;
using WFAI.Application.Behaviors;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Application.Tests.Behaviors;

public class ValidationPipelineBehaviorTests
{
    [Fact]
    public async Task Handle_should_fail_when_any_validator_fails_even_if_another_validator_passes()
    {
        var validators = new IValidator<PipelineTestRequest>[]
        {
            new PassingPipelineTestValidator(),
            new FailingPipelineTestValidator()
        };
        var mockFactory = new Mock<IValidationFailureFactory<IResponseWrapper>>();
        mockFactory.Setup(f => f.CreateFailure(It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>()))
                   .Returns<IReadOnlyList<string>, int>((msgs, code) => ResponseWrapper.Fail(msgs, code));
        var behavior = new ValidationPipelineBehavior<PipelineTestRequest, IResponseWrapper>(validators, mockFactory.Object);
        var handlerWasCalled = false;

        var result = await behavior.Handle(
            new PipelineTestRequest { Name = "invalid" },
            (_, _) =>
            {
                handlerWasCalled = true;
                return new ValueTask<IResponseWrapper>(ResponseWrapper.Success("Handler reached."));
            },
            CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("Name must be 'expected'.");
        handlerWasCalled.Should().BeFalse();
    }

    private sealed class PipelineTestRequest : IRequest<IResponseWrapper>, IValidateMe
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed class PassingPipelineTestValidator : AbstractValidator<PipelineTestRequest>
    {
        public PassingPipelineTestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    private sealed class FailingPipelineTestValidator : AbstractValidator<PipelineTestRequest>
    {
        public FailingPipelineTestValidator()
        {
            RuleFor(x => x.Name)
                .Equal("expected")
                .WithMessage("Name must be 'expected'.");
        }
    }
}