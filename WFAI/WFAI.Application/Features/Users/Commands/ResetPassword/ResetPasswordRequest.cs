using System.ComponentModel.DataAnnotations;

namespace WFAI.Application.Features.Users.Commands
{
    public class ResetPasswordRequest
    {
        public string Token { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string ConfirmPassword { get; set; } = string.Empty;
    }
}