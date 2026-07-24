using Microsoft.AspNetCore.Http;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Infrastructure.Identity
{
    public class CurrentUserMiddleware(ICurrentUserService currentUserService) : IMiddleware
    {
        private readonly ICurrentUserService _currentUserService = currentUserService;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            _currentUserService.SetCurrentUser(context.User);
            await next(context);
        }
    }
}