namespace WFAI.Application.Features.Users.Commands
{
    public class ResendConfirmationEmailCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public ResendConfirmationEmailRequest ResendConfirmation { get; set; }
    }

    public class ResendConfirmationEmailCommandHandler : IRequestHandler<ResendConfirmationEmailCommand, IResponseWrapper>
    {
        private readonly IUserService _userService;

        public ResendConfirmationEmailCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async ValueTask<IResponseWrapper> Handle(ResendConfirmationEmailCommand request, CancellationToken ct)
        {
            return await _userService.ResendConfirmationEmailAsync(
                request.ResendConfirmation.Email);
        }
    }
}