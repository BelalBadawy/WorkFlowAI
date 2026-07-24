using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using WFAI.Application.Dtos.JWT;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Token;
using WFAI.Application.Features.Token.Queries;
using WFAI.Application.Features.Token.Queries.LoginWith2FA;
using WFAI.Application.Interfaces.Common;
using WFAI.Infrastructure.Persistence.Contexts;

namespace WFAI.Infrastructure.Identity.Services
{
    public class TokenService : ITokenService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly JwtConfiguration _tokenSettings;
        private readonly IDateTimeService _dateTimeService;
        private readonly IDistributedCache _cache;
        private readonly ApplicationDbContext? _dbContext;

        private string ChallengeIssuer => $"{_tokenSettings.Issuer}:2fa-challenge";
        private const string ChallengeAudience = "2fa-challenge";
        private const string ChallengeClaim = "2fa_challenge";

        public TokenService(UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IOptions<JwtConfiguration> tokenSettings,
            IDateTimeService dateTimeService,
            IDistributedCache cache,
            ApplicationDbContext? dbContext = null)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenSettings = tokenSettings.Value;
            _dateTimeService = dateTimeService;
            _cache = cache;
            _dbContext = dbContext;
        }

        public async Task<IResponseWrapper<TokenResponse>> GetTokenAsync(TokenRequest tokenRequest)
        {
            #region Validations
            var userInDb = await _userManager.FindByEmailAsync(tokenRequest.Email);

            if (userInDb == null)
            {
                return ResponseWrapper<TokenResponse>.Fail(message: "Invalid Credentials.");
            }
            // Check if Active
            if (!userInDb.IsActive)
            {
                return ResponseWrapper<TokenResponse>.Fail("User not active. Please contact the administrator");
            }
            // Check email if email confirmed
            if (!userInDb.EmailConfirmed)
            {
                return ResponseWrapper<TokenResponse>.Fail("Email not confirmed.", statusCode: 403);
            }
            // Check if locked out before verifying password so the right message is shown
            if (await _userManager.IsLockedOutAsync(userInDb))
            {
                return ResponseWrapper<TokenResponse>.Fail("Account is locked. Please try again later or contact support.");
            }
            // Check password
            var isPasswordValid = await _userManager.CheckPasswordAsync(userInDb, tokenRequest.Password);

            if (!isPasswordValid)
            {
                await _userManager.AccessFailedAsync(userInDb);
                return ResponseWrapper<TokenResponse>.Fail("Invalid Credentials.");
            }

            #endregion

            // 2FA branch â€” defer ResetAccessFailedCount to Phase 2
            if (userInDb.TwoFactorEnabled)
            {
                var jti = Guid.NewGuid().ToString();
                var challenge = GenerateChallengeToken(userInDb, jti);
                return ResponseWrapper<TokenResponse>.Success(
                    new TokenResponse
                    {
                        RequiresTwoFactor = true,
                        TwoFactorChallengeToken = challenge
                    },
                    "Two-factor authentication required.");
            }

            // Reset failed access count after successful login
            await _userManager.ResetAccessFailedCountAsync(userInDb);

            // Generate token
            userInDb.RefreshToken = GenerateRefreshToken();
            userInDb.RefreshTokenExpiryDate = _dateTimeService.NowUtc.AddDays(_tokenSettings.RefreshTokenExpiryInDays);

            await _userManager.UpdateAsync(userInDb);

            var token = await GenerateJwtAsync(userInDb);

            var tokenResponse = new TokenResponse
            {
                Token = token,
                RefreshToken = userInDb.RefreshToken,
                RefreshTokenExpiryTime = userInDb.RefreshTokenExpiryDate
            };

            return ResponseWrapper<TokenResponse>.Success(data: tokenResponse);
        }

        public async Task<IResponseWrapper<TokenResponse>> LoginWith2FAAsync(TwoFactorLoginRequest request, CancellationToken ct = default)
        {
            // Step A â€” Validate the challenge token
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = ChallengeIssuer,
                ValidAudience = ChallengeAudience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_tokenSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };

            ClaimsPrincipal principal;
            try
            {
                principal = new JwtSecurityTokenHandler()
                    .ValidateToken(request.TwoFactorChallengeToken, validationParams, out _);
            }
            catch (Exception)
            {
                return ResponseWrapper<TokenResponse>.Fail("Invalid or expired challenge token.");
            }

            // Step B â€” Verify the 2fa_challenge claim is present
            if (principal.FindFirstValue(ChallengeClaim) is null)
                return ResponseWrapper<TokenResponse>.Fail("Invalid or expired challenge token.");

            // Step C â€” Replay check (distributed so it works across multiple instances)
            var jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (await _cache.GetAsync($"2fa_jti:{jti}", ct) is not null)
                return ResponseWrapper<TokenResponse>.Fail("Challenge token has already been used.");

            // Step D â€” Load and validate user
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null || !user.IsActive)
                return ResponseWrapper<TokenResponse>.Fail("Invalid credentials.");
            if (!user.EmailConfirmed)
                return ResponseWrapper<TokenResponse>.Fail("Email not confirmed.", statusCode: 403);
            if (await _userManager.IsLockedOutAsync(user))
                return ResponseWrapper<TokenResponse>.Fail("Account is locked. Please try again later.");
            if (!user.TwoFactorEnabled)
                return ResponseWrapper<TokenResponse>.Fail("Two-factor authentication is not enabled.");

            // Step E â€” Verify the code (TOTP first, then recovery code)
            bool success = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                request.Code);

            if (!success)
            {
                var recoveryResult = await _userManager.RedeemTwoFactorRecoveryCodeAsync(
                    user, request.Code);
                success = recoveryResult.Succeeded;
            }

            // Step F â€” Handle failure
            if (!success)
            {
                await _userManager.AccessFailedAsync(user);
                if (await _userManager.IsLockedOutAsync(user))
                    return ResponseWrapper<TokenResponse>.Fail(
                        "Account locked due to multiple failed attempts.");
                return ResponseWrapper<TokenResponse>.Fail("Invalid authenticator code.");
            }

            // Step G â€” Handle success (Phase 2 complete)
            await _userManager.ResetAccessFailedCountAsync(user);

            await _cache.SetAsync(
                $"2fa_jti:{jti}",
                [1],
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes(_tokenSettings.TwoFactorChallengeTokenExpiryInMinutes)
                },
                ct);

            user.RefreshToken = GenerateRefreshToken();
            user.RefreshTokenExpiryDate = _dateTimeService.NowUtc
                .AddDays(_tokenSettings.RefreshTokenExpiryInDays);
            await _userManager.UpdateAsync(user);

            var token = await GenerateJwtAsync(user);

            return ResponseWrapper<TokenResponse>.Success(new TokenResponse
            {
                Token = token,
                RefreshToken = user.RefreshToken,
                RefreshTokenExpiryTime = user.RefreshTokenExpiryDate
            });
        }

        private string GenerateChallengeToken(ApplicationUser user, string jti)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ChallengeClaim, "true"),
                new(JwtRegisteredClaimNames.Jti, jti)
            };

            var token = new JwtSecurityToken(
                issuer: ChallengeIssuer,
                audience: ChallengeAudience,
                claims: claims,
                expires: _dateTimeService.NowUtc.AddMinutes(
                    _tokenSettings.TwoFactorChallengeTokenExpiryInMinutes),
                signingCredentials: GetSigningCredentials());

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rnd = RandomNumberGenerator.Create();
            rnd.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private async Task<IEnumerable<Claim>> GetClaimsAsync(ApplicationUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roleNames = await _userManager.GetRolesAsync(user);

            var roleClaims = roleNames.Select(r => new Claim(ClaimTypes.Role, r)).ToList();

            List<Claim> permissionClaims;
            if (_dbContext is not null)
            {
                // Single batch query: join roles â†’ role claims in two round-trips instead of N+1
                var roleIds = await _roleManager.Roles
                    .Where(r => roleNames.Contains(r.Name!))
                    .Select(r => r.Id)
                    .ToListAsync();

                permissionClaims = await _dbContext.RoleClaims
                    .Where(rc => roleIds.Contains(rc.RoleId) && rc.ClaimValue != null)
                    .Select(rc => new Claim(rc.ClaimType!, rc.ClaimValue!))
                    .ToListAsync();
            }
            else
            {
                // Fallback path used in unit tests where DbContext is not injected
                var roleClaimSets = await Task.WhenAll(
                    roleNames.Select(async role =>
                    {
                        var roleEntity = await _roleManager.FindByNameAsync(role);
                        return roleEntity is null
                            ? []
                            : await _roleManager.GetClaimsAsync(roleEntity);
                    }));

                permissionClaims = roleClaimSets
                    .SelectMany(claims => claims.Select(c => new Claim(c.Type, c.Value)))
                    .ToList();
            }

            var allClaims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, user.FullName ?? string.Empty),
                new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
            }
            .Union(roleClaims)
            .Union(userClaims)
            .Union(permissionClaims);

            return allClaims
                .GroupBy(c => new { c.Type, c.Value })
                .Select(g => g.First())
                .ToList();
        }

        private SigningCredentials GetSigningCredentials()
        {
            var secret = Encoding.UTF8.GetBytes(_tokenSettings.Secret);
            return new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256);
        }

        private string GenerateEncryptedToken(SigningCredentials signingCredentials, IEnumerable<Claim> claims)
        {
            var token = new JwtSecurityToken(
                issuer: _tokenSettings.Issuer,
                audience: _tokenSettings.Audience,
                claims: claims,
                expires: _dateTimeService.NowUtc.AddMinutes(_tokenSettings.TokenExpiryInMinutes),
                signingCredentials: signingCredentials);
            var tokenHandler = new JwtSecurityTokenHandler();
            var encryptedToken = tokenHandler.WriteToken(token);
            return encryptedToken;
        }

        private async Task<string> GenerateJwtAsync(ApplicationUser user)
        {
            var token = GenerateEncryptedToken(GetSigningCredentials(), await GetClaimsAsync(user));
            return token;
        }

        public async Task<IResponseWrapper<TokenResponse>> GetRefreshTokenAsync(RefreshTokenRequest refreshTokenRequest)
        {
            try
            {
                var userPrincipal = GetClaimPrincipalFromExpiredToken(refreshTokenRequest.Token);
                var userEmail = userPrincipal.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return ResponseWrapper<TokenResponse>.Fail(message: "Invalid token provided.");
                }

                var userInDb = await _userManager.FindByEmailAsync(userEmail);
                if (userInDb is not null)
                {
                    if (userInDb.RefreshToken != refreshTokenRequest.RefreshToken
                        || userInDb.RefreshTokenExpiryDate <= _dateTimeService.NowUtc)
                    {
                        return ResponseWrapper<TokenResponse>.Fail(message: "Invalid token provided.");
                    }

                    var token = GenerateEncryptedToken(GetSigningCredentials(), await GetClaimsAsync(userInDb));
                    userInDb.RefreshToken = GenerateRefreshToken();
                    userInDb.RefreshTokenExpiryDate = _dateTimeService.NowUtc.AddDays(_tokenSettings.RefreshTokenExpiryInDays);

                    await _userManager.UpdateAsync(userInDb);

                    var tokenResponse = new TokenResponse
                    {
                        Token = token,
                        RefreshToken = userInDb.RefreshToken,
                        RefreshTokenExpiryTime = userInDb.RefreshTokenExpiryDate
                    };

                    return ResponseWrapper<TokenResponse>.Success(tokenResponse);
                }
                return ResponseWrapper<TokenResponse>.Fail(message: "User does not exist.");
            }
            catch (Exception)
            {
                return ResponseWrapper<TokenResponse>.Fail(message: "Invalid token provided.");
            }
        }

        private ClaimsPrincipal GetClaimPrincipalFromExpiredToken(string expiredToken)
        {
            var tokenValidationParms = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidIssuer = _tokenSettings.Issuer,
                ValidAudience = _tokenSettings.Audience,
                RoleClaimType = ClaimTypes.Role,
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenSettings.Secret)),
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(expiredToken, tokenValidationParms, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken
                || !jwtSecurityToken.Header.Alg
                .Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
    }
}