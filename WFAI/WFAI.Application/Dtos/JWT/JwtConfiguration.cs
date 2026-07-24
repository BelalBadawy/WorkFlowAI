namespace WFAI.Application.Dtos.JWT
{
    /// <summary>
    /// JWT configuration options used to generate and validate tokens.
    /// </summary>
    public class JwtConfiguration
    {
        /// <summary>
        /// Token issuer.
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Token audience.
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Secret signing key for tokens.
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// Token expiry duration in minutes.
        /// </summary>
        public int TokenExpiryInMinutes { get; set; }

        /// <summary>
        /// Refresh token expiry duration in days.
        /// </summary>
        public int RefreshTokenExpiryInDays { get; set; }

        public int TwoFactorChallengeTokenExpiryInMinutes { get; set; }
    }
}