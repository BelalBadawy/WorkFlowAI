using System.Collections.Concurrent;
using System.Web;

namespace WFAI.API.Tests.Support;

public sealed class ApiTestEmailSink
{
    private readonly ConcurrentQueue<ApiTestEmailMessage> _messages = new();

    public void Add(ApiTestEmailMessage message)
    {
        _messages.Enqueue(message);
    }

    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }
    }

    public ApiTestEmailMessage? FindLatestFor(string email)
    {
        return _messages
            .Where(message => string.Equals(message.MailTo, email, StringComparison.OrdinalIgnoreCase))
            .LastOrDefault();
    }

    public string GetLatestResetToken(string email)
    {
        var message = FindLatestFor(email)
            ?? throw new InvalidOperationException($"No API test email was captured for '{email}'.");

        if (string.IsNullOrWhiteSpace(message.ResetUrl))
        {
            throw new InvalidOperationException($"No reset URL was captured for '{email}'.");
        }

        var uri = new Uri(message.ResetUrl);
        var query = HttpUtility.ParseQueryString(uri.Query);
        var token = query["token"] ?? query["code"];

        return token
            ?? throw new InvalidOperationException($"No reset token was found in the reset URL for '{email}'.");
    }

    public string GetQueryParam(string email, string paramName)
    {
        var message = FindLatestFor(email)
            ?? throw new InvalidOperationException($"No API test email was captured for '{email}'.");

        if (string.IsNullOrWhiteSpace(message.ResetUrl))
        {
            throw new InvalidOperationException($"No URL was captured for '{email}'.");
        }

        var uri = new Uri(message.ResetUrl);
        var query = HttpUtility.ParseQueryString(uri.Query);
        return query[paramName]
            ?? throw new InvalidOperationException($"Query param '{paramName}' not found in URL for '{email}'.");
    }
}

public sealed record ApiTestEmailMessage(
    string? MailTo,
    string? Subject,
    string? MessageBody,
    string? ResetUrl);