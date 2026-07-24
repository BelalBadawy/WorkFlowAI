using AutoFixture;
using Bogus;
using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Features.Roles;
using WFAI.Application.Features.Roles.Commands;
using WFAI.Application.Features.Token.Queries;
using WFAI.Application.Features.Users.Commands;
using WFAI.Application.Features.Users.Models.Requests;
using WFAI.Application.Features.Users.Models.Responses;

namespace WFAI.Application.Tests.Fixtures;

internal static class TestData
{
    private static readonly Fixture Fixture = new();
    private static readonly Faker Faker = new();

    public static UserRegistrationRequest UserRegistrationRequest() => new()
    {
        FullName = Faker.Name.FullName(),
        Email = Faker.Internet.Email(),
        Password = "Valid@123",
        ConfirmPassword = "Valid@123",
        PhoneNumber = "01012345678",
        AutoConfirmEmail = true,
        ActivateUser = true
    };

    public static TokenRequest TokenRequest() => new()
    {
        Email = Faker.Internet.Email(),
        Password = "Valid@123"
    };

    public static RefreshTokenRequest RefreshTokenRequest() => new()
    {
        Token = Fixture.Create<string>(),
        RefreshToken = Fixture.Create<string>()
    };

    public static ResetPasswordRequest ResetPasswordRequest() => new()
    {
        Email = Faker.Internet.Email(),
        Token = Fixture.Create<string>(),
        Password = "Valid@123",
        ConfirmPassword = "Valid@123"
    };

    public static ChangePasswordRequest ChangePasswordRequest(int? userId = null) => new()
    {
        CurrentPassword = "Current@123",
        NewPassword = "Valid@123",
        ConfirmedNewPassword = "Valid@123"
    };

    public static ChangeUserStatusRequest ChangeUserStatusRequest(int? userId = null) => new()
    {
        UserId = userId ?? Fixture.Create<int>(),
        ActivateOrDeactivate = true
    };

    public static UpdateUserRequest UpdateUserRequest(int? userId = null) => new()
    {
        UserId = userId ?? Fixture.Create<int>(),
        FullName = Faker.Name.FullName(),
        PhoneNumber = "01012345678"
    };

    public static UpdateUserRolesRequest UpdateUserRolesRequest(int? userId = null) => new()
    {
        UserId = userId ?? Fixture.Create<int>(),
        Roles = ["Admin", "Basic"]
    };

    public static PagedFilterRequest PagedFilterRequest() => new()
    {
        PageNumber = 2,
        PageSize = 5,
        SearchTerm = Faker.Lorem.Word(),
        SortBy = "FullName",
        SortDirection = "desc",
        IsActive = true
    };

    public static UserRoleViewModel UserRoleViewModel(string? roleName = null) => new()
    {
        RoleName = roleName ?? "Admin",
        RoleDescription = Faker.Lorem.Sentence()
    };

    public static CreateRoleRequest CreateRoleRequest() => new()
    {
        Name = Faker.Commerce.Department(),
        Description = Faker.Lorem.Sentence()
    };

    public static UpdateRoleRequest UpdateRoleRequest(int? roleId = null) => new()
    {
        RoleId = roleId ?? Fixture.Create<int>(),
        Name = Faker.Commerce.Department(),
        Description = Faker.Lorem.Sentence()
    };

    public static RoleResponse RoleResponse(int? id = null) => new()
    {
        Id = id ?? Fixture.Create<int>(),
        Name = Faker.Commerce.Department(),
        Description = Faker.Lorem.Sentence()
    };

    public static RoleClaimViewModel RoleClaimViewModel(string? claimValue = null) => new()
    {
        ClaimType = "Permission",
        ClaimValue = claimValue ?? Faker.Lorem.Word(),
        Description = Faker.Lorem.Sentence()
    };

    public static RoleClaimResponse RoleClaimResponse(int? roleId = null) => new()
    {
        Role = RoleResponse(roleId),
        RoleClaims =
        [
            RoleClaimViewModel("Permissions.Roles.View"),
            RoleClaimViewModel("Permissions.Roles.Update")
        ]
    };

    public static UpdateRoleClaimsRequest UpdateRoleClaimsRequest(int? roleId = null) => new()
    {
        RoleId = roleId ?? Fixture.Create<int>(),
        RoleClaims =
        [
            RoleClaimViewModel("Permissions.Roles.View"),
            RoleClaimViewModel("Permissions.Roles.Update")
        ]
    };

    public static UserResponse UserResponse(int? id = null) => new()
    {
        Id = id ?? Fixture.Create<int>(),
        FullName = Faker.Name.FullName(),
        Email = Faker.Internet.Email(),
        UserName = Faker.Internet.UserName(),
        IsActive = true,
        PhoneNumber = "01012345678"
    };

    public static ConfirmEmailRequest ConfirmEmailRequest(int? userId = null, string? token = null) => new()
    {
        UserId = userId ?? Fixture.Create<int>(),
        Token  = token  ?? Fixture.Create<string>()
    };

    public static ConfirmEmailChangeRequest ConfirmEmailChangeRequest(int? userId = null) => new()
    {
        UserId   = userId ?? Fixture.Create<int>(),
        NewEmail = Faker.Internet.Email(),
        Token    = Fixture.Create<string>()
    };

    public static ResendConfirmationEmailRequest ResendConfirmationEmailRequest(string? email = null) => new()
    {
        Email = email ?? Faker.Internet.Email()
    };

    public static GenerateChangeEmailTokenRequest GenerateChangeEmailTokenRequest(string? newEmail = null) => new()
    {
        NewEmail = newEmail ?? Faker.Internet.Email()
    };

    public static LockUserRequest LockUserRequest(int? userId = null) => new()
    {
        UserId = userId ?? Fixture.Create<int>()
    };

    public static UnlockUserRequest UnlockUserRequest(int? userId = null) => new()
    {
        UserId = userId ?? Fixture.Create<int>()
    };
}