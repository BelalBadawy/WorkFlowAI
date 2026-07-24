using WFAI.Application.Features.Users.Models.Requests;

namespace WFAI.Application.Features.Users.Queries
{
    public class GetUserRolesQuery : IRequest<IResponseWrapper<List<UserRoleViewModel>>>, IValidateMe
    {
        public int UserId { get; set; }
    }

    public class GetUserRolesQueryHandler : IRequestHandler<GetUserRolesQuery, IResponseWrapper<List<UserRoleViewModel>>>
    {
        private readonly IUserService _userService;

        public GetUserRolesQueryHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async ValueTask<IResponseWrapper<List<UserRoleViewModel>>> Handle(GetUserRolesQuery request, CancellationToken ct)
        {
            return await _userService.GetUserRolesAsync(request.UserId);
        }
    }
}