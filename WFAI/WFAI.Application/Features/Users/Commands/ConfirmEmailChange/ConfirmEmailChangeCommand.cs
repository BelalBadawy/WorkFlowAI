namespace WFAI.Application.Features.Users.Commands
{
    public class ConfirmEmailChangeCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public ConfirmEmailChangeRequest ConfirmEmailChange { get; set; }
    }

    public class ConfirmEmailChangeCommandHandler : IRequestHandler<ConfirmEmailChangeCommand, IResponseWrapper>
    {
        private readonly IUserService _userService;

        public ConfirmEmailChangeCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async ValueTask<IResponseWrapper> Handle(ConfirmEmailChangeCommand request, CancellationToken ct)
        {
            return await _userService.ConfirmEmailChangeAsync(
                request.ConfirmEmailChange.UserId,
                request.ConfirmEmailChange.NewEmail,
                request.ConfirmEmailChange.Token);
        }
    }
}