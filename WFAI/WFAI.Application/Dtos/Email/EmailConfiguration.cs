namespace WFAI.Application.Dtos.Email
{
    /// <summary>
    /// Configuration options for SMTP email sending.
    /// </summary>
    public class EmailConfiguration
    {
        /// <summary>
        /// SMTP port to connect to.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// SMTP host name or IP address.
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// The sender email address used for authentication.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Password used to authenticate with the SMTP server.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the sender.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Whether to enable SSL/TLS when connecting to SMTP.
        /// </summary>
        public bool EnableSsl { get; set; }
    }
}