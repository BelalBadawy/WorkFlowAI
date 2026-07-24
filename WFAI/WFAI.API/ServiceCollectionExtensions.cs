using Asp.Versioning;

namespace WFAI.API
{
    public static class ServiceCollectionExtensions
    {
        internal static IServiceCollection AddApiVersioningConfig(this IServiceCollection services)
        {
            services
                .AddApiVersioning(options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1, 0);
                    options.AssumeDefaultVersionWhenUnspecified = true;

                    //  This line triggers OnStarting (disable it)
                    options.ReportApiVersions = false;
                })
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });

            return services;
        }

        public static IServiceCollection AddCorsConfig(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration
                .GetSection("AllowedOrigins")
                .Get<string[]>() ?? [];

            return services.AddCors(options =>
            {
                options.AddPolicy("AllowedOrigins", policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy
                            .WithOrigins(allowedOrigins)
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    }
                    else
                    {
                        // Fallback for local development when no origins are configured
                        policy
                            .WithOrigins("https://localhost:7216", "https://localhost:7216", "http://localhost:5170")
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    }
                });
            });
        }


    }
}