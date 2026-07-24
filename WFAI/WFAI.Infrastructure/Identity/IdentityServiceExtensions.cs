using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text;
using System.Text.Json;
using WFAI.Application.Authorization;
using WFAI.Application.Dtos.JWT;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Roles;
using WFAI.Application.Features.Token;
using WFAI.Application.Features.Users;
using WFAI.Infrastructure.Identity.Permissions;
using WFAI.Infrastructure.Identity.Services;
using WFAI.Infrastructure.Persistence.DbInitializers;

namespace WFAI.Infrastructure.Identity
{
    internal static class IdentityServiceExtensions
    {
        internal static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration config)
        {
            return services
                .AddIdentity<ApplicationUser, ApplicationRole>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedEmail = true;
                    options.Lockout = new LockoutOptions
                    {
                        DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15),
                        MaxFailedAccessAttempts = 5,
                        AllowedForNewUsers = true
                    };
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .Services
                .AddScoped<IUserService, UserService>()
                .AddScoped<IRoleService, RoleService>()
                .AddScoped<ITokenService, TokenService>()
                .AddScoped<CurrentUserMiddleware>()
                .AddTransient<IdentityDbSeeder>()
                .Configure<JwtConfiguration>(config.GetSection("JwtConfiguration"));
        }

        internal static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CurrentUserMiddleware>();
        }

        internal static IServiceCollection AddPermissions(this IServiceCollection services)
        {
            services
                .AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>()
                .AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            return services;
        }

        internal static JwtConfiguration GetTokenSettings(this IServiceCollection services, IConfiguration config)
        {
            var tokenSettingsConfig = config.GetSection(nameof(JwtConfiguration));
            services.Configure<JwtConfiguration>(tokenSettingsConfig);

            return tokenSettingsConfig.Get<JwtConfiguration>();
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration
                .GetSection("JwtConfiguration")
                .Get<JwtConfiguration>();

            if (jwtSettings == null)
            {
                throw new InvalidOperationException("JwtConfiguration section is not configured in appsettings.json");
            }

            var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

            services
              .AddAuthentication(auth =>
              {
                  auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                  auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
              })
              .AddJwtBearer(bearer =>
              {
                  bearer.RequireHttpsMetadata = true;
                  bearer.SaveToken = true;
                  bearer.TokenValidationParameters = new TokenValidationParameters
                  {
                      ValidateIssuerSigningKey = true,
                      ValidateIssuer = true,
                      ValidateAudience = true,
                      ValidateLifetime = true,
                      ValidIssuer = jwtSettings.Issuer,
                      ValidAudience = jwtSettings.Audience,
                      RoleClaimType = ClaimTypes.Role,
                      ClockSkew = TimeSpan.Zero,
                      IssuerSigningKey = new SymmetricSecurityKey(key)
                  };

                  bearer.Events = new JwtBearerEvents
                  {
                      OnMessageReceived = context =>
                      {
                          var path = context.HttpContext.Request.Path.Value ?? string.Empty;

                          if (path.Contains("refresh-token", StringComparison.OrdinalIgnoreCase))
                          {
                              context.NoResult();
                          }

                          return Task.CompletedTask;
                      },
                      OnAuthenticationFailed = context =>
                      {
                          context.HttpContext.Items["AuthError"] = context.Exception;
                          return Task.CompletedTask;
                      },
                      OnChallenge = context =>
                      {
                          context.HandleResponse();
                          if (!context.Response.HasStarted)
                          {
                              context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                              context.Response.ContentType = "application/json";

                              string errorMessage = "You are not Authorized.";
                              if (context.HttpContext.Items.TryGetValue("AuthError", out var errorObj) && errorObj is Exception ex)
                              {
                                  if (ex is SecurityTokenExpiredException)
                                      errorMessage = "The token has expired. Please log in again.";
                                  else if (ex is ArgumentException && ex.Message.Contains("IDX14100"))
                                      errorMessage = "The provided token format is invalid.";
                                  else if (ex is SecurityTokenInvalidSignatureException)
                                      errorMessage = "The token signature is invalid.";
                                  else
                                      errorMessage = "You are not authorized to access this resource.";
                              }

                              var result = JsonSerializer.Serialize(ResponseWrapper.Fail(errorMessage, (int)HttpStatusCode.Unauthorized));
                              return context.Response.WriteAsync(result);
                          }

                          return Task.CompletedTask;
                      },
                      OnForbidden = context =>
                      {
                          context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                          context.Response.ContentType = "application/json";
                          var result = JsonSerializer.Serialize(
                              ResponseWrapper.Fail("You are not authorized to access this resource.", (int)HttpStatusCode.Forbidden));
                          return context.Response.WriteAsync(result);
                      }
                  };
              });

            services.AddAuthorization(options =>
            {
                foreach (var permission in AppPermissions.AllPermissions)
                {
                    options.AddPolicy(permission.Name, policy =>
                        policy.RequireClaim(AppClaim.Permission, permission.Name));
                }
            });

            return services;
        }
    }
}