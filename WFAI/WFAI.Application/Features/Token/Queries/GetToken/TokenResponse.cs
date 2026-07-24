using System.Text.Json.Serialization;

namespace WFAI.Application.Features.Token.Queries
{
    public class TokenResponse
    {
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public bool RequiresTwoFactor { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TwoFactorChallengeToken { get; set; }
    }
}