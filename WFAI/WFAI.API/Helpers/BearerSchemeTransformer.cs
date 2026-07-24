using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WFAI.API.Helpers
{
    public class BearerSchemeTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

            if (!document.Components.SecuritySchemes.ContainsKey("Bearer"))
            {
                document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter JWT Bearer token"
                });
            }

            foreach (var path in document.Paths.Values)
            {
                // Microsoft.OpenApi v2 uses HttpMethod keys directly
                foreach (var operation in path.Operations.Values)
                {
                    operation.Security ??= [];

                    var hasBearerSecurity = operation.Security.Any(req =>
                        req.Any(pair => pair.Key is OpenApiSecuritySchemeReference r &&
                            r.Reference?.Id == "Bearer"));

                    if (!hasBearerSecurity)
                    {
                        var requirement = new OpenApiSecurityRequirement();
                        requirement.Add(new OpenApiSecuritySchemeReference("Bearer"), []);
                        operation.Security.Add(requirement);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}