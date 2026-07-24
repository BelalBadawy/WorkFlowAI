namespace WFAI.Application.Features.Users.Commands
{
    public class ConfirmEmailCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public ConfirmEmailRequest ConfirmEmail { get; set; }
    }

    public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, IResponseWrapper>
    {
        private readonly IUserService _userService;

        public ConfirmEmailCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async ValueTask<IResponseWrapper> Handle(ConfirmEmailCommand request, CancellationToken ct)
        {
            return await _userService.ConfirmEmailAsync(
                request.ConfirmEmail.UserId,
                request.ConfirmEmail.Token);
        }
    }
}