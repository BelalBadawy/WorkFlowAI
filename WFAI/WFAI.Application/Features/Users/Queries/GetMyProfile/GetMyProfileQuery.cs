using WFAI.Application.Features.Users.Models.Responses;

namespace WFAI.Application.Features.Users.Queries.GetMyProfile
{
    public class GetMyProfileQuery : IRequest<IResponseWrapper<ProfileResponse>> { }

    public class GetMyProfileQueryHandler
        : IRequestHandler<GetMyProfileQuery, IResponseWrapper<ProfileResponse>>
    {
        private readonly IUserService _userService;

        public GetMyProfileQueryHandler(IUserService userService)
            => _userService = userService;

        public async ValueTask<IResponseWrapper<ProfileResponse>> Handle(
            GetMyProfileQuery request, CancellationToken ct)
            => await _userService.GetMyProfileAsync();
    }
}