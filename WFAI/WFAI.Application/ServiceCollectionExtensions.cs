
using WFAI.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace WFAI.Application
{
    /// <summary>
    /// Extension methods for setting up application-specific services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds application layer services including validation, Mediator, and Mapster.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Add services for FluentValidation auto-validation
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // Fully qualify the method call to resolve ambiguity
            Microsoft.Extensions.DependencyInjection.MediatorDependencyInjectionExtensions.AddMediator(services, options =>
            {
                options.ServiceLifetime = ServiceLifetime.Scoped;
                options.Namespace = "WFAI.Application";
            });

            services.AddSingleton(typeof(IValidationFailureFactory<>), typeof(ValidationFailureFactory<>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));
           // services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));

            services.AddMapster();

            return services;
        }
    }
}