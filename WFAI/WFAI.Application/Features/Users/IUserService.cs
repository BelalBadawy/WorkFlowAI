using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Features.Users.Commands.DisableTwoFactorAuth;
using WFAI.Application.Features.Users.Commands.Logout;
using WFAI.Application.Features.Users.Models.Requests;
using WFAI.Application.Features.Users.Models.Responses;

namespace WFAI.Application.Features.Users
{
    public interface IUserService
    {
        Task<IResponseWrapper> RegisterUserAsync(UserRegistrationRequest userRegistration);
        Task<IResponseWrapper> UpdateUserAsync(UpdateUserRequest userUpdate);

        // Start
        Task<IResponseWrapper<UserResponse>> GetUserByIdAsync(int userId);
        Task<IResponseWrapper<PagedResult<UserResponse>>> GetUsersPagedQueryAsync(PagedFilterRequest pagedFilterRequest, CancellationToken ct);
        Task<IResponseWrapper<List<UserExportResponse>>> GetUsersListAsync(PagedFilterRequest filter, CancellationToken ct);
        Task<byte[]> ExportUsersAsync(List<UserExportResponse> data, string format, CancellationToken ct);
        Task<IResponseWrapper> ChangeUserPasswordAsync(int userId, ChangePasswordRequest changePassword);
        Task<IResponseWrapper> ChangeUserStatusAsync(ChangeUserStatusRequest changeUserStatus);
        Task<IResponseWrapper<List<UserRoleViewModel>>> GetUserRolesAsync(int userId);
        Task<IResponseWrapper> UpdateUserRolesAsync(UpdateUserRolesRequest updateUserRoles,CancellationToken ct);
        Task<IResponseWrapper> ForgotPasswordAsync(string email);
        Task<IResponseWrapper> ResetPasswordAsync(ResetPasswordRequest request);
        Task<IResponseWrapper> ConfirmEmailAsync(int userId, string token);
        Task<IResponseWrapper> ConfirmEmailChangeAsync(int userId, string newEmail, string token);
        Task<IResponseWrapper> ResendConfirmationEmailAsync(string email);
        Task<IResponseWrapper> GenerateChangeEmailTokenAsync(string newEmail);
        Task<IResponseWrapper<List<string>>> GenerateNew2FARecoveryCodesAsync();
        Task<IResponseWrapper> LockUserAsync(int userId);
        Task<IResponseWrapper> UnlockUserAsync(int userId);

        Task<IResponseWrapper<ProfileResponse>> GetMyProfileAsync();
        Task<IResponseWrapper> LogoutAsync(LogoutRequest request);
        Task<IResponseWrapper<TwoFactorAuthViewModel>> SetupTwoFactorAuthAsync();
        Task<IResponseWrapper> ConfirmTwoFactorAuthAsync(TwoFactorCodeRequest request);
        Task<IResponseWrapper<List<string>>> EnableTwoFactorAuthAsync(TwoFactorCodeRequest request);
        Task<IResponseWrapper> DisableTwoFactorAuthAsync(DisableTwoFactorAuthRequest request);
    }
}