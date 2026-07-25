using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;
using WFAI.API;
using WFAI.API.Endpoints;
using WFAI.API.Helpers;
using WFAI.Application;
using WFAI.Application.Dtos.Common;
using WFAI.Application.Dtos.TwoFactor;
using WFAI.Infrastructure;
using WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
        new BearerSchemeTransformer().TransformAsync(document, context, ct)
    );
});

builder.Services.AddCorsConfig(builder.Configuration);
builder.Services.AddHttpContextAccessor();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddSlidingWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.SegmentsPerWindow = 4;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });
});

builder.Services.AddApiVersioningConfig();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
builder.Services.Configure<TwoFactorOptions>(builder.Configuration.GetSection("TwoFactor"));
builder.Services.Configure<ClientSettings>(builder.Configuration.GetSection("ClientSettings"));


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.AddPreferredSecuritySchemes("Bearer");
    });
        //.RequireAuthorization();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseRouting();
app.UseRateLimiter();

// CORS before authentication
app.UseCors("AllowedOrigins");
await app.UseInfrastructureAsync();
app.MapAccountEndpoints();
app.MapCategoryEndpoints();
app.MapPhaseEndpoints();
app.MapRoleEndpoints();
app.MapUserEndpoints();
app.MapAuditTrailEndpoints();
app.Run();
