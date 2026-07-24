namespace WFAI.Domain.Enums;

/// <summary>
/// Supported Multi-Factor Authentication methods.
/// </summary>
public enum MfaMethod
{
    None = 0,
    Email = 1,
    Totp = 2,
    Sms = 3
}

public enum AuditType
{
    None = 0,
    Create = 1,
    Update = 2,
    Delete = 3
}