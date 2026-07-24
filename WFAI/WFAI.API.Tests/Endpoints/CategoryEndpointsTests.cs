using System.Net;
using System.Net.Http.Json;
using WFAI.Application.Authorization;
using WFAI.API.Tests.Contracts;
using WFAI.API.Tests.Fixtures;
using WFAI.API.Tests.Support;

namespace WFAI.API.Tests.Endpoints;

[Collection("API collection")]
public class CategoryEndpointsTests : ApiTestBase
{
    public CategoryEndpointsTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Get_all_categories_should_return_seeded_categories_for_requested_state()
    {
        var activeName = $"Active-{Guid.NewGuid():N}";
        var activeSlug = $"active-{Guid.NewGuid():N}";
        var inactiveName = $"Inactive-{Guid.NewGuid():N}";
        var inactiveSlug = $"inactive-{Guid.NewGuid():N}";

        await Seeder.SeedCategoryAsync(activeName, activeSlug, isActive: true, sortOrder: 1);
        await Seeder.SeedCategoryAsync(inactiveName, inactiveSlug, isActive: false, sortOrder: 2);
        Seeder.ClearCategoryCaches();

        var response = await Client.GetAsync("/api/v1/categories?isActive=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<List<CategoryResponseContract>>>();

        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data.Should().Contain(category => category.Name == activeName && category.Slug == activeSlug);
        payload.Data.Should().NotContain(category => category.Name == inactiveName);
    }

    [Fact]
    public async Task Get_category_by_id_should_return_seeded_category()
    {
        var name = $"Details-{Guid.NewGuid():N}";
        var slug = $"details-{Guid.NewGuid():N}";
        var seededCategory = await Seeder.SeedCategoryAsync(name, slug, isActive: true, sortOrder: 3);

        var response = await Client.GetAsync($"/api/v1/categories/{seededCategory.Id}");
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<CategoryDetailsResponseContract>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Id.Should().Be(seededCategory.Id);
        payload.Data.Name.Should().Be(name);
        payload.Data.Slug.Should().Be(slug);
    }

    [Fact]
    public async Task Get_category_by_id_should_return_unsuccessful_payload_when_id_is_invalid()
    {
        var response = await Client.GetAsync("/api/v1/categories/99999");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<CategoryResponseContract>>();

        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task GetCategoriesPaged_DefaultRequest_ReturnsSuccessfulPayload()
    {
        var firstName = $"Paged-A-{Guid.NewGuid():N}";
        var secondName = $"Paged-B-{Guid.NewGuid():N}";
        var inactiveName = $"Paged-Inactive-{Guid.NewGuid():N}";

        await Seeder.SeedCategoryAsync(firstName, $"paged-a-{Guid.NewGuid():N}", isActive: true, sortOrder: 1);
        await Seeder.SeedCategoryAsync(secondName, $"paged-b-{Guid.NewGuid():N}", isActive: true, sortOrder: 2);
        await Seeder.SeedCategoryAsync(inactiveName, $"paged-inactive-{Guid.NewGuid():N}", isActive: false, sortOrder: 3);
        Seeder.ClearCategoryCaches();

        const string route = "/api/v1/categories/paged?pageNumber=1&pageSize=10&sortBy=sortorder&sortDirection=asc";

        var response = await Client.GetAsync(route);
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<PagedResultContract<CategoryResponseContract>>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.PageSize.Should().Be(10);
        payload.Data.CurrentPage.Should().Be(1);
        payload.Data.Data.Should().NotBeNull();
        payload.Data.Data.Should().Contain(category => category.Name == firstName);
        payload.Data.Data.Should().Contain(category => category.Name == secondName);
        payload.Data.Data.Should().NotContain(category => category.Name == inactiveName);
        payload.Data.TotalCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetCategoriesForList_AuthorizedRequest_ReturnsLookupItems()
    {
        // Arrange
        var listName = $"Lookup-{Guid.NewGuid():N}";
        var listSlug = $"lookup-{Guid.NewGuid():N}";
        await Seeder.SeedCategoryAsync(listName, listSlug, isActive: true, sortOrder: 1);
        Seeder.ClearCategoryCaches();

        UsePrivilegedClient(AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Read));

        // Act
        var response = await Client.GetAsync("/api/v1/categories/for-list");
        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<List<CategoryLookupContract>>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data.Should().OnlyContain(item => item.Id >= 0);
        payload.Data.Should().Contain(item => item.Name == listName);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("low-privilege", HttpStatusCode.Forbidden)]
    [InlineData("privileged", HttpStatusCode.OK)]
    public async Task Create_category_should_follow_authorization_matrix(string authMode, HttpStatusCode expectedStatusCode)
    {
        var requiredPermission = AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Create);
        switch (authMode)
        {
            case "anonymous":
                UseAnonymousClient();
                break;
            case "low-privilege":
                UseLowPrivilegeClient(requiredPermission);
                break;
            case "privileged":
                UsePrivilegedClient(requiredPermission);
                break;
            default:
                throw new InvalidOperationException($"Unsupported auth mode '{authMode}'.");
        }

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new
        {
            Name = $"Created {suffix}",
            Slug = $"created-{suffix}",
            ParentId = (int?)null,
            IsActive = true,
            SortOrder = 1
        };

        var response = await Client.PostAsJsonAsync("/api/v1/categories", request);

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<int>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        payload.Data.Should().BeGreaterThan(0);

        var createdCategory = await Verifier.GetCategoryByIdAsync(payload.Data);
        createdCategory.Should().NotBeNull();
        createdCategory!.Name.Should().Be(request.Name);
        createdCategory.Slug.Should().Be(request.Slug);
        createdCategory.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("low-privilege", HttpStatusCode.Forbidden)]
    [InlineData("privileged", HttpStatusCode.OK)]
    public async Task Update_category_should_follow_authorization_matrix(string authMode, HttpStatusCode expectedStatusCode)
    {
        var existingCategory = await Seeder.SeedCategoryAsync(
            $"Update-{Guid.NewGuid():N}",
            $"update-{Guid.NewGuid():N}",
            isActive: true,
            sortOrder: 5);

        var requiredPermission = AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Update);
        switch (authMode)
        {
            case "anonymous":
                UseAnonymousClient();
                break;
            case "low-privilege":
                UseLowPrivilegeClient(requiredPermission);
                break;
            case "privileged":
                UsePrivilegedClient(requiredPermission);
                break;
            default:
                throw new InvalidOperationException($"Unsupported auth mode '{authMode}'.");
        }

        var persistedCategory = await Verifier.GetCategoryByIdIncludingSoftDeletedAsync(existingCategory.Id);
        persistedCategory.Should().NotBeNull();

        var request = new
        {
            Id = existingCategory.Id,
            Name = $"Updated-{Guid.NewGuid():N}",
            Slug = $"updated-{Guid.NewGuid():N}",
            ParentId = (int?)null,
            IsActive = false,
            SortOrder = 9,
            RowVersion = persistedCategory!.RowVersion
        };

        var response = await Client.PutAsJsonAsync("/api/v1/categories", request);

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();

        var updatedCategory = await Verifier.GetCategoryByIdIncludingSoftDeletedAsync(existingCategory.Id);
        updatedCategory.Should().NotBeNull();
        updatedCategory!.Name.Should().Be(request.Name);
        updatedCategory.Slug.Should().Be(request.Slug);
        updatedCategory.IsActive.Should().BeFalse();
        updatedCategory.SortOrder.Should().Be(9);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("low-privilege", HttpStatusCode.Forbidden)]
    [InlineData("privileged", HttpStatusCode.OK)]
    public async Task Delete_category_should_follow_authorization_matrix(string authMode, HttpStatusCode expectedStatusCode)
    {
        var categoryToDelete = await Seeder.SeedCategoryAsync(
            $"Delete-{Guid.NewGuid():N}",
            $"delete-{Guid.NewGuid():N}",
            isActive: true,
            sortOrder: 7);

        var requiredPermission = AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Delete);
        switch (authMode)
        {
            case "anonymous":
                UseAnonymousClient();
                break;
            case "low-privilege":
                UseLowPrivilegeClient(requiredPermission);
                break;
            case "privileged":
                UsePrivilegedClient(requiredPermission);
                break;
            default:
                throw new InvalidOperationException($"Unsupported auth mode '{authMode}'.");
        }

        var response = await Client.DeleteAsync($"/api/v1/categories/{categoryToDelete.Id}");

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode != HttpStatusCode.OK)
        {
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<object>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();

        var visibleCategory = await Verifier.GetCategoryByIdAsync(categoryToDelete.Id);
        visibleCategory.Should().BeNull();

        var deletedCategory = await Verifier.GetCategoryByIdIncludingSoftDeletedAsync(categoryToDelete.Id);
        deletedCategory.Should().NotBeNull();
        deletedCategory!.SoftDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Export_categories_excel_should_return_file_bytes()
    {
        UsePrivilegedClient(AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Read));
        
        var response = await Client.GetAsync("/api/v1/categories/export?exportFormat=excel");
        var errorContent = await response.Content.ReadAsStringAsync();
        
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: $"Error content was: {errorContent}");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task Export_categories_pdf_should_return_file_bytes()
    {
        UsePrivilegedClient(AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Read));
        
        var response = await Client.GetAsync("/api/v1/categories/export?exportFormat=pdf");
        var errorContent = await response.Content.ReadAsStringAsync();
        
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: $"Error content was: {errorContent}");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task Create_category_should_generate_audit_log_with_correct_id_not_zero()
    {
        UsePrivilegedClient(AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Create));

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new
        {
            Name = $"AuditTest {suffix}",
            Slug = $"audit-test-{suffix}",
            ParentId = (int?)null,
            IsActive = true,
            SortOrder = 1
        };

        var response = await Client.PostAsJsonAsync("/api/v1/categories", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<int>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        var categoryId = payload.Data;
        categoryId.Should().BeGreaterThan(0);

        // Fetch the generated audit trail from database using the Verifier
        var auditTrail = await Verifier.GetLastAuditTrailForTableAsync("Category");
        auditTrail.Should().NotBeNull();
        auditTrail!.PrimaryKey.Should().Be($"{{\"Id\":{categoryId}}}");
    }

    [Fact]
    public async Task Restore_category_should_restore_soft_deleted_category()
    {
        var name = $"RestoreTest-{Guid.NewGuid():N}";
        var slug = $"restore-test-{Guid.NewGuid():N}";
        var category = await Seeder.SeedCategoryAsync(name, slug, isActive: true, sortOrder: 8, softDeleted: true);
        Seeder.ClearCategoryCaches();

        UsePrivilegedClient(AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Update));

        var response = await Client.PostAsync($"/api/v1/categories/{category.Id}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ResponseContract<int>>();
        payload.Should().NotBeNull();
        payload!.IsSuccessful.Should().BeTrue();
        payload.Data.Should().Be(category.Id);

        var restoredCategory = await Verifier.GetCategoryByIdIncludingSoftDeletedAsync(category.Id);
        restoredCategory.Should().NotBeNull();
        restoredCategory!.SoftDeleted.Should().BeFalse();
        restoredCategory.DeletedAt.Should().BeNull();
        restoredCategory.DeletedBy.Should().BeNull();
    }
}