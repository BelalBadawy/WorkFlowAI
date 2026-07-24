using System.Text.RegularExpressions;
using WFAI.Application.Dtos.Email;
using WFAI.Application.Interfaces.Common;

namespace WFAI.API.Tests.Support;

public sealed class ApiTestEmailService : IEmailService
{
    private static readonly Regex ResetLinkRegex = new("href=\"(?<url>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly ApiTestEmailSink _sink;

    public ApiTestEmailService(ApiTestEmailSink sink)
    {
        _sink = sink;
    }

    public Task<string> SendAsync(SendEmailDto request, CancellationToken ct = default)
    {
        var resetUrl = TryExtractResetUrl(request.MessageBody);

        _sink.Add(new ApiTestEmailMessage(
            request.MailTo,
            request.Subject,
            request.MessageBody,
            resetUrl));

        return Task.FromResult(string.Empty);
    }

    private static string? TryExtractResetUrl(string? messageBody)
    {
        if (string.IsNullOrWhiteSpace(messageBody))
        {
            return null;
        }

        var match = ResetLinkRegex.Match(messageBody);
        return match.Success ? match.Groups["url"].Value : null;
    }
}