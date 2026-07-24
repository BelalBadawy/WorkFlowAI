using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Web;
using System.IO;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WFAI.Application.Dtos.Common;
using WFAI.Application.Dtos.Email;
using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Dtos.TwoFactor;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Features.Users.Commands.DisableTwoFactorAuth;
using WFAI.Application.Features.Users.Commands.Logout;
using WFAI.Application.Features.Users.Models.Requests;
using WFAI.Application.Features.Users.Models.Responses;
using WFAI.Application.Interfaces.Common;
using WFAI.Infrastructure.Identity.Configurations;
using WFAI.Infrastructure.Identity.Models;
using WFAI.Application.Features.Users.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace WFAI.Infrastructure.Identity.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDateTimeService _dateTimeService;
        private readonly ICurrentUserService _currentUserService;
        private readonly TwoFactorOptions _twoFactorOptions;
        private readonly ClientSettings _clientSettings;
        private readonly ILogger<UserService> _logger;
        private readonly IApplicationDbContext _context;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IEmailService emailService,
            IHttpContextAccessor contextAccessor,
            IDateTimeService dateTimeService,
            ICurrentUserService currentUserService,
            IOptions<TwoFactorOptions> twoFactorOptions,
            IOptions<ClientSettings> clientSettings,
            ILogger<UserService> logger,
            IApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _httpContextAccessor = contextAccessor;
            _dateTimeService = dateTimeService;
            _currentUserService = currentUserService;
            _twoFactorOptions = twoFactorOptions.Value;
            _clientSettings = clientSettings.Value;
            _logger = logger;
            _context = context;
        }

        private static string GenerateSecureToken()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        public async Task<IResponseWrapper> RegisterUserAsync(UserRegistrationRequest userRegistration)
        {
            var userWithSameEmail = await _userManager.FindByEmailAsync(userRegistration.Email);
            if (userWithSameEmail is not null)
                return ResponseWrapper.Fail("Email address already taken.");

            var newUser = new ApplicationUser
            {
                FullName = userRegistration.FullName,
                Email = userRegistration.Email,
                UserName = userRegistration.Email,
                PhoneNumber = userRegistration.PhoneNumber,
                IsActive = userRegistration.ActivateUser,
                EmailConfirmed = userRegistration.AutoConfirmEmail,
                RefreshToken = GenerateSecureToken(),
                RefreshTokenExpiryDate = _dateTimeService.NowUtc.AddDays(1)
            };

            var identityUserResult = await _userManager.CreateAsync(newUser, userRegistration.Password);

            if (identityUserResult.Succeeded)
            {
                var identityRoleResult = await _userManager.AddToRoleAsync(newUser, AppRoles.Basic);

                if (identityRoleResult.Succeeded)
                {
                    if (!userRegistration.AutoConfirmEmail)
                    {
                        var clientBaseUrl = _clientSettings.BaseUrl;
                        var emailToken  = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
                        var callbackUrl = $"{clientBaseUrl.TrimEnd('/')}/confirm-email" +
                                          $"?userId={newUser.Id}" +
                                          $"&token={HttpUtility.UrlEncode(emailToken)}";

                        await _emailService.SendAsync(new SendEmailDto
                        {
                            Subject     = "Confirm Your Email",
                            MailTo      = newUser.Email,
                            MessageBody = $"<p>Hello: {newUser.FullName}</p>" +
                                          "<p>Please confirm your email by clicking the link below.</p>" +
                                          $"<p><a href=\"{callbackUrl}\">Confirm Email</a></p>"
                        });
                    }

                    return ResponseWrapper.Success("User registered successfully.");
                }

                return ResponseWrapper.Fail(GetIdentityResultErrorDescriptions(identityRoleResult));
            }

            return ResponseWrapper.Fail(GetIdentityResultErrorDescriptions(identityUserResult));
        }

        public async Task<IResponseWrapper> UpdateUserAsync(UpdateUserRequest userUpdate)
        {
            var userInDb = await _userManager.FindByIdAsync(userUpdate.UserId.ToString());

            if (userInDb is not null)
            {
                userInDb.FullName = userUpdate.FullName;
                userInDb.PhoneNumber = userUpdate.PhoneNumber;

                var identityResult = await _userManager.UpdateAsync(userInDb);

                if (identityResult.Succeeded)
                {
                    return ResponseWrapper.Success("User updated successfully.");
                }

                return ResponseWrapper.Fail(GetIdentityResultErrorDescriptions(identityResult));
            }

            return ResponseWrapper.Fail("User does not exists.");
        }

        #region Private Helpers
        private static List<string> GetIdentityResultErrorDescriptions(IdentityResult identityResult)
        {
            var errorDescriptions = new List<string>();
            foreach (var error in identityResult.Errors)
            {
                errorDescriptions.Add(error.Description);
            }

            return errorDescriptions;
        }
        #endregion

        public async Task<IResponseWrapper<UserResponse>> GetUserByIdAsync(int userId)
        {
            var userInDb = await _userManager.FindByIdAsync(userId.ToString());
            if (userInDb is not null)
            {
                var mappedUser = userInDb.Adapt<UserResponse>();
                mappedUser.IsLocked = userInDb.LockoutEnd != null && userInDb.LockoutEnd > DateTimeOffset.UtcNow;

                return ResponseWrapper<UserResponse>.Success(data: mappedUser);
            }

            return ResponseWrapper<UserResponse>.Fail("User does not exists.", StatusCodes.Status404NotFound);
        }

        private IQueryable<ApplicationUser> BuildUserQuery(GetUsersPagedQuery query)
        {
            var pagedFilterRequest = query.PagedFilterRequest;
            var dbContext = _context as DbContext;
            var usersQuery = dbContext != null 
                ? dbContext.Set<ApplicationUser>() 
                : _userManager.Users.AsQueryable();

            if (pagedFilterRequest.IsActive.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.IsActive == pagedFilterRequest.IsActive.Value);
            }

            if (pagedFilterRequest.IsLocked.HasValue)
            {
                var utcNow = DateTimeOffset.UtcNow;
                if (pagedFilterRequest.IsLocked.Value)
                {
                    usersQuery = usersQuery.Where(u => u.LockoutEnd != null && u.LockoutEnd > utcNow);
                }
                else
                {
                    usersQuery = usersQuery.Where(u => u.LockoutEnd == null || u.LockoutEnd <= utcNow);
                }
            }

            if (pagedFilterRequest.RoleId.HasValue && dbContext != null)
            {
                var userIdsInRole = dbContext.Set<ApplicationUserRole>()
                    .Where(ur => ur.RoleId == pagedFilterRequest.RoleId.Value)
                    .Select(ur => ur.UserId);

                usersQuery = usersQuery.Where(u => userIdsInRole.Contains(u.Id));
            }

            if (!string.IsNullOrWhiteSpace(pagedFilterRequest.SearchTerm))
            {
                var term = pagedFilterRequest.SearchTerm.Trim();
                var searchPattern = $"%{term}%";

                usersQuery = usersQuery.Where(u =>
                    EF.Functions.Like(u.FullName, searchPattern) ||
                    EF.Functions.Like(u.Email, searchPattern)
                );
            }

            usersQuery = pagedFilterRequest.SortBy?.ToLower() switch
            {
                "email" => pagedFilterRequest.SortDirection == "desc"
                    ? usersQuery.OrderByDescending(u => u.Email)
                    : usersQuery.OrderBy(u => u.Email),

                "id" => pagedFilterRequest.SortDirection == "desc"
                    ? usersQuery.OrderByDescending(u => u.Id)
                    : usersQuery.OrderBy(u => u.Id),

                "fullname" or _ => pagedFilterRequest.SortDirection == "desc"
                    ? usersQuery.OrderByDescending(u => u.FullName)
                    : usersQuery.OrderBy(u => u.FullName),
            };

            return usersQuery;
        }

        public async Task<IResponseWrapper<PagedResult<UserResponse>>> GetUsersPagedQueryAsync(
            PagedFilterRequest pagedFilterRequest,
            CancellationToken ct)
        {
            var usersQuery = BuildUserQuery(new GetUsersPagedQuery { PagedFilterRequest = pagedFilterRequest });

            var totalRecords = await usersQuery.CountAsync(ct);

            var users = await usersQuery
                .Skip((pagedFilterRequest.PageNumber - 1) * pagedFilterRequest.PageSize)
                .Take(pagedFilterRequest.PageSize)
                .Select(o => new UserResponse
                {
                    FullName = o.FullName,
                    Email = o.Email,
                    Id = o.Id,
                    IsActive = o.IsActive,
                    PhoneNumber = o.PhoneNumber,
                    UserName = o.UserName,
                    EmailConfirmed = o.EmailConfirmed,
                    IsLocked = o.LockoutEnd != null && o.LockoutEnd > DateTimeOffset.UtcNow
                })
                .ToListAsync(ct);

            var data = new PagedResult<UserResponse>
            {
                Data = users,
                TotalCount = totalRecords,
                CurrentPage = pagedFilterRequest.PageNumber,
                PageSize = pagedFilterRequest.PageSize,
            };

            return ResponseWrapper<PagedResult<UserResponse>>.Success(data: data);
        }

        public async Task<IResponseWrapper<List<UserExportResponse>>> GetUsersListAsync(
            PagedFilterRequest filter,
            CancellationToken ct)
        {
            var dbContext = _context as DbContext;
            if (dbContext == null)
            {
                return ResponseWrapper<List<UserExportResponse>>.Fail("Invalid database context.");
            }

            var usersQuery = BuildUserQuery(new GetUsersPagedQuery { PagedFilterRequest = filter });

            var userRolesQuery = from ur in dbContext.Set<ApplicationUserRole>()
                                 join r in dbContext.Set<ApplicationRole>() on ur.RoleId equals r.Id
                                 select new { ur.UserId, RoleName = r.Name };

            var usersData = await usersQuery.ToListAsync(ct);
            var userIds = usersData.Select(u => u.Id).ToList();

            var rolesList = await userRolesQuery
                .Where(ur => userIds.Contains(ur.UserId))
                .ToListAsync(ct);

            var rolesGrouped = rolesList
                .GroupBy(ur => ur.UserId)
                .ToDictionary(g => g.Key, g => g.Select(ur => ur.RoleName!).ToList());

            var result = usersData.Select(u => new UserExportResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                UserName = u.UserName,
                IsActive = u.IsActive,
                EmailConfirmed = u.EmailConfirmed,
                PhoneNumber = u.PhoneNumber,
                IsLocked = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow,
                Roles = rolesGrouped.TryGetValue(u.Id, out var roles) ? roles : new List<string>()
            }).ToList();

            return ResponseWrapper<List<UserExportResponse>>.Success(result);
        }

        public async Task<byte[]> ExportUsersAsync(List<UserExportResponse> data, string format, CancellationToken ct)
        {
            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                return GeneratePdfExport(data);
            }
            else
            {
                return GenerateExcelExport(data);
            }
        }

        private byte[] GenerateExcelExport(List<UserExportResponse> data)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Users");

            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Full Name";
            worksheet.Cell(1, 3).Value = "Email";
            worksheet.Cell(1, 4).Value = "Phone Number";
            worksheet.Cell(1, 5).Value = "Assigned Roles";
            worksheet.Cell(1, 6).Value = "Status";
            worksheet.Cell(1, 7).Value = "Lockout Status";
            worksheet.Cell(1, 8).Value = "Email Confirmed";

            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5"); // Indigo-600
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.Id;
                worksheet.Cell(row, 2).Value = item.FullName;
                worksheet.Cell(row, 3).Value = item.Email;
                worksheet.Cell(row, 4).Value = item.PhoneNumber ?? "N/A";
                worksheet.Cell(row, 5).Value = string.Join(", ", item.Roles);
                worksheet.Cell(row, 6).Value = item.IsActive ? "Active" : "Inactive";
                worksheet.Cell(row, 7).Value = item.IsLocked ? "Locked" : "Unlocked";
                worksheet.Cell(row, 8).Value = item.EmailConfirmed ? "Yes" : "No";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private byte[] GeneratePdfExport(List<UserExportResponse> data)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Helvetica"));

                    page.Header()
                        .PaddingBottom(10)
                        .Text("System Users Report")
                        .SemiBold().FontSize(16).FontColor(Colors.Indigo.Medium);

                    page.Content()
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30); // ID
                                columns.RelativeColumn(2.5f); // Full Name
                                columns.RelativeColumn(3f); // Email
                                columns.RelativeColumn(1.8f); // Phone
                                columns.RelativeColumn(2.5f); // Roles
                                columns.RelativeColumn(1.2f); // Status
                                columns.RelativeColumn(1.2f); // Locked
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("ID").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("Full Name").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("Email").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("Phone").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("Roles").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("Status").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("Locked").SemiBold().FontColor(Colors.White);

                                static IContainer HeaderStyle(IContainer container)
                                {
                                    return container
                                        .Background(Colors.Indigo.Medium)
                                        .Padding(6)
                                        .AlignMiddle();
                                }
                            });

                            foreach (var item in data)
                            {
                                table.Cell().Element(CellStyle).Text(item.Id.ToString());
                                table.Cell().Element(CellStyle).Text(item.FullName);
                                table.Cell().Element(CellStyle).Text(item.Email);
                                table.Cell().Element(CellStyle).Text(item.PhoneNumber ?? "N/A");
                                table.Cell().Element(CellStyle).Text(string.Join(", ", item.Roles));
                                table.Cell().Element(CellStyle).Text(item.IsActive ? "Active" : "Inactive");
                                table.Cell().Element(CellStyle).Text(item.IsLocked ? "Locked" : "Unlocked");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container
                                        .BorderBottom(0.5f)
                                        .BorderColor(Colors.Grey.Lighten3)
                                        .Padding(6)
                                        .AlignMiddle();
                                }
                            }
                        });

                    page.Footer()
                        .PaddingTop(10)
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        public async Task<IResponseWrapper> ChangeUserPasswordAsync(int userId, ChangePasswordRequest changePassword)
        {
            var userInDb = await _userManager.FindByIdAsync(userId.ToString());
            if (userInDb is not null)
            {
                var identityResult = await _userManager.ChangePasswordAsync(
                    userInDb,
                    changePassword.CurrentPassword,
                    changePassword.NewPassword);

                if (identityResult.Succeeded)
                {
                    return ResponseWrapper.Success(message: "User password updated.");
                }

                return ResponseWrapper.Fail(GetIdentityResultErrorDescriptions(identityResult));
            }

            return ResponseWrapper.Fail("User does not exist.");
        }

        public async Task<IResponseWrapper> ChangeUserStatusAsync(ChangeUserStatusRequest changeUserStatus)
        {
            var userInDb = await _userManager.FindByIdAsync(changeUserStatus.UserId.ToString());
            if (userInDb is not null)
            {
                if (await _userManager.IsInRoleAsync(userInDb, AppRoles.Admin) && !changeUserStatus.ActivateOrDeactivate)
                {
                    return ResponseWrapper.Fail("Cannot de-activate the system administrator.");
                }

                userInDb.IsActive = changeUserStatus.ActivateOrDeactivate;

                var identityResult = await _userManager.UpdateAsync(userInDb);

                if (identityResult.Succeeded)
                {
                    return ResponseWrapper
                        .Success(changeUserStatus.ActivateOrDeactivate
                            ? "User activated successfully."
                            : "User de-activated successfully");
                }

                return ResponseWrapper.Fail(GetIdentityResultErrorDescriptions(identityResult));
            }

            return ResponseWrapper.Fail("User does not exist.");
        }

        public async Task<IResponseWrapper<List<UserRoleViewModel>>> GetUserRolesAsync(int userId)
        {
            var userInDb = await _userManager.FindByIdAsync(userId.ToString());

            if (userInDb is not null)
            {
                var assignedRoleNames = (await _userManager.GetRolesAsync(userInDb)).ToHashSet(StringComparer.OrdinalIgnoreCase);

                var userRolesViewModel = (await _roleManager.Roles.ToListAsync())
                    .Where(r => assignedRoleNames.Contains(r.Name!))
                    .Select(r => new UserRoleViewModel { RoleName = r.Name, RoleDescription = r.Description })
                    .ToList();

                return ResponseWrapper<List<UserRoleViewModel>>.Success(userRolesViewModel);
            }

            return ResponseWrapper<List<UserRoleViewModel>>.Fail("User does not exist.");
        }

        public async Task<IResponseWrapper> UpdateUserRolesAsync(UpdateUserRolesRequest request, CancellationToken ct)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

            if (user is null)
                return ResponseWrapper.Fail("User does not exist.");

            if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
                return ResponseWrapper.Fail("User roles update not permitted.");

            var rolesToAssign = request.Roles.ToList();

            foreach (var roleName in rolesToAssign)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                    return ResponseWrapper.Fail($"Role '{roleName}' does not exist.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
                return ResponseWrapper.Fail(GetIdentityResultErrorDescriptions(removeResult));

            var addResult = await _userManager.AddToRolesAsync(user, rolesToAssign);
            if (!addResult.Succeeded)
                return ResponseWrapper.Fail(GetIdentityResultErrorDescriptions(addResult));

            return ResponseWrapper.Success("Updated user roles successfully.");
        }

        public async Task<IResponseWrapper> ForgotPasswordAsync(string email)
        {
            const string safeMessage = "If the email is registered, you will receive an email shortly.";

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null || !user.EmailConfirmed)
                return ResponseWrapper.Success(safeMessage);

            var clientBaseUrl = _clientSettings.BaseUrl;
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = $"{clientBaseUrl.TrimEnd('/')}/reset-password" +
                              $"?email={HttpUtility.UrlEncode(user.Email)}" +
                              $"&token={HttpUtility.UrlEncode(code)}";

            var emailModel = new SendEmailDto
            {
                Subject = "Reset Password",
                MailTo = user.Email,
                MessageBody = $"<p>Hello: {user.FullName}</p>" +
                $"<p>Username: {user.UserName}.</p>" +
                "<p>In order to reset your password, please click on the following link.</p>" +
                $"<p><a href=\"{callbackUrl}\">Click here to Reset Password</a></p>" +
                "<p>If the link above does not work, copy and paste the following URL into your browser:</p>" +
                $"<p>{callbackUrl}</p>" +
                "<p>Thank you,</p>"
            };

            try
            {
                await _emailService.SendAsync(emailModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
            }

            return ResponseWrapper.Success(safeMessage);
        }

        public async Task<IResponseWrapper> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return ResponseWrapper.Fail("This email doesn't exist.");

            if (!user.EmailConfirmed)
                return ResponseWrapper.Fail("This email is not confirmed.");

            try
            {
                var result = await _userManager.ResetPasswordAsync(user, request.Token, request.Password);

                if (result.Succeeded)
                {
                    await _userManager.UpdateSecurityStampAsync(user);
                    return ResponseWrapper.Success("Your password has changed successfully.");
                }

                return ResponseWrapper.Fail(GetIdentityResultErrorDescriptions(result));
            }
            catch (Exception)
            {
                return ResponseWrapper.Fail(SD.ErrorOccured);
            }
        }

        public async Task<IResponseWrapper> ConfirmEmailAsync(int userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return ResponseWrapper.Fail("User does not exist.");

            if (user.EmailConfirmed)
                return ResponseWrapper.Success("Email is already confirmed.");
            
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
                return ResponseWrapper.Fail(GetIdentityResultErrorDescriptions(result));

            return ResponseWrapper.Success("Email confirmed successfully.");
        }

        public async Task<IResponseWrapper> ConfirmEmailChangeAsync(int userId, string newEmail, string token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return ResponseWrapper.Fail("User does not exist.");

            var result = await _userManager.ChangeEmailAsync(user, newEmail, token);
            if (!result.Succeeded)
                return ResponseWrapper.Fail(GetIdentityResultErrorDescriptions(result));

            await _userManager.SetUserNameAsync(user, newEmail);
            return ResponseWrapper.Success("Email changed successfully.");
        }

        public async Task<IResponseWrapper> ResendConfirmationEmailAsync(string email)
        {
            const string safeMessage = "If the email is registered, you will receive an email shortly.";

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null || user.EmailConfirmed)
                return ResponseWrapper.Success(safeMessage);

            var clientBaseUrl = _clientSettings.BaseUrl;
            var token       = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = $"{clientBaseUrl.TrimEnd('/')}/confirm-email" +
                              $"?userId={user.Id}" +
                              $"&token={HttpUtility.UrlEncode(token)}";

            await _emailService.SendAsync(new SendEmailDto
            {
                Subject     = "Confirm Your Email",
                MailTo      = user.Email,
                MessageBody = $"<p>Hello: {user.FullName}</p>" +
                              "<p>Please confirm your email by clicking the link below.</p>" +
                              $"<p><a href=\"{callbackUrl}\">Confirm Email</a></p>"
            });

            return ResponseWrapper.Success(safeMessage);
        }

        public async Task<IResponseWrapper> GenerateChangeEmailTokenAsync(string newEmail)
        {
            var userId = _currentUserService.GetUserId();
            var user   = await _userManager.FindByIdAsync(userId?.ToString());
            if (user is null)
                return ResponseWrapper.Fail("User does not exist.");

            if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
                return ResponseWrapper.Fail("New email must be different from your current email.");

            var clientBaseUrl = _clientSettings.BaseUrl;
            var token       = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
            var callbackUrl = $"{clientBaseUrl.TrimEnd('/')}/confirm-email-change" +
                              $"?userId={user.Id}" +
                              $"&newEmail={HttpUtility.UrlEncode(newEmail)}" +
                              $"&token={HttpUtility.UrlEncode(token)}";

            await _emailService.SendAsync(new SendEmailDto
            {
                Subject     = "Confirm Your Email Change",
                MailTo      = user.Email,
                MessageBody = $"<p>Hello: {user.FullName}</p>" +
                              "<p>Click the link below to confirm your email change.</p>" +
                              $"<p><a href=\"{callbackUrl}\">Confirm Email Change</a></p>"
            });

            return ResponseWrapper.Success("Email change confirmation sent. Please check your inbox.");
        }

        public async Task<IResponseWrapper<List<string>>> GenerateNew2FARecoveryCodesAsync()
        {
            var userId = _currentUserService.GetUserId();
            var user   = await _userManager.FindByIdAsync(userId?.ToString());
            if (user is null)
                return ResponseWrapper<List<string>>.Fail("User does not exist.");

            if (!user.TwoFactorEnabled)
                return ResponseWrapper<List<string>>.Fail("Two-factor authentication is not enabled.");

            var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            return ResponseWrapper<List<string>>.Success(codes!.ToList(), "New recovery codes generated.");
        }

        public async Task<IResponseWrapper> LockUserAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return ResponseWrapper.Fail("User does not exist.");

            if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
                return ResponseWrapper.Fail("Cannot lock the system administrator.");

            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(1000));
            await _userManager.UpdateSecurityStampAsync(user);

            user.RefreshTokenExpiryDate = _dateTimeService.NowUtc.AddDays(-1);
            await _userManager.UpdateAsync(user);

            return ResponseWrapper.Success("User locked successfully.");
        }

        public async Task<IResponseWrapper> UnlockUserAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return ResponseWrapper.Fail("User does not exist.");

            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
            await _userManager.ResetAccessFailedCountAsync(user);

            return ResponseWrapper.Success("User unlocked successfully.");
        }

        public async Task<IResponseWrapper<ProfileResponse>> GetMyProfileAsync()
        {
            var userId = _currentUserService.GetUserId();
            var user   = await _userManager.FindByIdAsync(userId?.ToString());
            if (user is null)
                return ResponseWrapper<ProfileResponse>.Fail("User not found.");

            var roles = (await _userManager.GetRolesAsync(user)).ToList();

            var permissionsSet = new HashSet<string>();
            foreach (var roleName in roles)
            {
                var role   = await _roleManager.FindByNameAsync(roleName);
                var claims = await _roleManager.GetClaimsAsync(role);
                foreach (var claim in claims)
                    permissionsSet.Add(claim.Value);
            }

            return ResponseWrapper<ProfileResponse>.Success(new ProfileResponse
            {
                Id               = user.Id,
                FullName         = user.FullName,
                Email            = user.Email,
                UserName         = user.UserName,
                IsActive         = user.IsActive,
                EmailConfirmed   = user.EmailConfirmed,
                PhoneNumber      = user.PhoneNumber,
                TwoFactorEnabled = user.TwoFactorEnabled,
                CreatedDate      = user.CreatedDate,
                Roles            = roles,
                Permissions      = [.. permissionsSet]
            });
        }

        public async Task<IResponseWrapper> LogoutAsync(LogoutRequest request)
        {
            var userId = _currentUserService.GetUserId();
            var user   = await _userManager.FindByIdAsync(userId?.ToString());
            if (user is null)
                return ResponseWrapper.Fail("User not found.");

            if (string.IsNullOrEmpty(request.RefreshToken))
                return ResponseWrapper.Fail("Refresh token is required.");

            if (user.RefreshToken != request.RefreshToken)
                return ResponseWrapper.Fail("Invalid refresh token.");

            user.RefreshToken           = string.Empty;
            user.RefreshTokenExpiryDate = _dateTimeService.NowUtc.AddDays(-1);
            await _userManager.UpdateAsync(user);

            return ResponseWrapper.Success("Logged out successfully.");
        }

        public async Task<IResponseWrapper<TwoFactorAuthViewModel>> SetupTwoFactorAuthAsync()
        {
            var userId = _currentUserService.GetUserId();
            var user   = await _userManager.FindByIdAsync(userId?.ToString());
            if (user is null)
                return ResponseWrapper<TwoFactorAuthViewModel>.Fail("User not found.");

            if (user.TwoFactorEnabled)
                return ResponseWrapper<TwoFactorAuthViewModel>.Fail(
                    "Two-factor authentication is already enabled. Disable it first to reconfigure.");

            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                key = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            var issuer = Uri.EscapeDataString(_twoFactorOptions.Issuer);
            var email  = Uri.EscapeDataString(user.Email);
            var codeQR = $"otpauth://totp/{issuer}:{email}?secret={key}&issuer={issuer}";

            return ResponseWrapper<TwoFactorAuthViewModel>.Success(
                new TwoFactorAuthViewModel { KeySecret = key, CodeQR = codeQR });
        }

        public async Task<IResponseWrapper> ConfirmTwoFactorAuthAsync(TwoFactorCodeRequest request)
        {
            var userId = _currentUserService.GetUserId();
            var user   = await _userManager.FindByIdAsync(userId?.ToString());
            if (user is null)
                return ResponseWrapper.Fail("User not found.");

            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
                return ResponseWrapper.Fail(
                    "No authenticator configured. Please call setup-2fa first.");

            var valid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                request.Code);

            if (!valid)
            {
                await _userManager.AccessFailedAsync(user);
                return ResponseWrapper.Fail("Invalid verification code.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            return ResponseWrapper.Success("Verification code is valid.");
        }

        public async Task<IResponseWrapper<List<string>>> EnableTwoFactorAuthAsync(
            TwoFactorCodeRequest request)
        {
            var userId = _currentUserService.GetUserId();
            var user   = await _userManager.FindByIdAsync(userId?.ToString());
            if (user is null)
                return ResponseWrapper<List<string>>.Fail("User not found.");

            if (user.TwoFactorEnabled)
                return ResponseWrapper<List<string>>.Fail(
                    "Two-factor authentication is already enabled.");

            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
                return ResponseWrapper<List<string>>.Fail(
                    "No authenticator configured. Please call setup-2fa first.");

            var valid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                request.Code);

            if (!valid)
            {
                await _userManager.AccessFailedAsync(user);
                if (await _userManager.IsLockedOutAsync(user))
                    return ResponseWrapper<List<string>>.Fail(
                        "Account locked due to multiple failed attempts.");
                return ResponseWrapper<List<string>>.Fail("Invalid authenticator code.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.SetTwoFactorEnabledAsync(user, true);

            var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            return ResponseWrapper<List<string>>.Success(
                codes!.ToList(),
                "Two-factor authentication enabled. Store your recovery codes safely.");
        }

        public async Task<IResponseWrapper> DisableTwoFactorAuthAsync(
            DisableTwoFactorAuthRequest request)
        {
            var userId = _currentUserService.GetUserId();
            var user   = await _userManager.FindByIdAsync(userId?.ToString());
            if (user is null)
                return ResponseWrapper.Fail("User not found.");

            if (!user.TwoFactorEnabled)
                return ResponseWrapper.Fail("Two-factor authentication is not enabled.");

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                await _userManager.AccessFailedAsync(user);
                if (await _userManager.IsLockedOutAsync(user))
                    return ResponseWrapper.Fail("Account locked due to multiple failed attempts.");
                return ResponseWrapper.Fail("Invalid password.");
            }

            if (!string.IsNullOrEmpty(request.Code))
            {
                var codeValid = await _userManager.VerifyTwoFactorTokenAsync(
                    user,
                    _userManager.Options.Tokens.AuthenticatorTokenProvider,
                    request.Code);

                if (!codeValid)
                    return ResponseWrapper.Fail("Invalid authenticator code.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.SetTwoFactorEnabledAsync(user, false);

            return ResponseWrapper.Success("Two-factor authentication disabled.");
        }
    }
}