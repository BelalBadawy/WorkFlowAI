namespace WFAI.Domain.Entities;

/// <summary>
/// Entity for storing user activity logs.
/// </summary>
public class LogUserActivity : BaseEntity<int>
{
    /// <summary>
    /// ID of the user who performed the action.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Date and time when the activity was logged.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// The URL that was accessed.
    /// </summary>
    public string UrlData { get; set; } = string.Empty;

    /// <summary>
    /// Additional user data (e.g., request body, parameters).
    /// </summary>
    public string UserData { get; set; } = string.Empty;

    /// <summary>
    /// IP address of the client.
    /// </summary>
    public string IPAddress { get; set; } = string.Empty;

    /// <summary>
    /// Browser user agent string.
    /// </summary>
    public string Browser { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE, etc.).
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// ID of the user who impersonated the acting user (if applicable).
    /// </summary>
    public int? ImpersonatedBy { get; set; }
}