using WFAI.Application.Dtos.JWT;

namespace WFAI.Infrastructure.Identity.Permissions
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IConfiguration _configuration;

        public PermissionAuthorizationHandler(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {


            if (context.User != null && context.User.Claims != null && context.User.Claims.Any())
            {
                var jwtSettings = _configuration
                    .GetSection("JwtConfiguration")
                    .Get<JwtConfiguration>();

                var hasPermission = context.User.Claims.Any(c =>
                    c.Type == AppClaim.Permission &&
                    c.Value == requirement.Permission &&
                    c.Issuer == jwtSettings.Issuer);

                if (hasPermission)
                {
                    context.Succeed(requirement);
                }
            }

            // You can optionally leave this or omit it
            await Task.CompletedTask;

            //if (context.User is null)
            //{
            //    await Task.CompletedTask;
            //}

            //if (context.User.Identity.IsAuthenticated == false)
            //{
            //    await Task.CompletedTask;
            //}

            //var jwtSettings = _configuration
            //    .GetSection("JwtConfiguration")
            //    .Get<JwtConfiguration>();

            //var permissions = context.User.Claims
            //    .Where(claim => claim.Type == AppClaim.Permission
            //        && claim.Value == requirement.Permission
            //        && claim.Issuer == jwtSettings.Issuer);
            //if (permissions.Any())
            //{
            //    context.Succeed(requirement);
            //    await Task.CompletedTask;
            //}
        }
    }
}