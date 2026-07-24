using Microsoft.EntityFrameworkCore;
using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Categories;
using WFAI.Application.Features.Categories.Queries.GetAllCategories;
using WFAI.Application.Features.Categories.Queries.GetAllCategoriesForList;
using WFAI.Application.Features.Categories.Queries.GetCategoriesAdmin;
using WFAI.Application.Features.Categories.Queries.GetCategoriesPaged;
using WFAI.Application.Features.Categories.Queries.GetCategoriesPagedAdmin;
using WFAI.Application.Features.Categories.Queries.GetCategoryById;
using WFAI.Application.Features.Categories.Queries.GetCategoryByIdAdmin;
using Moq;
using WFAI.Application.Interfaces.Common;
using WFAI.Application.Tests.Support.Categories;
using WFAI.Application.Features.Categories.Queries.GetCategoriesList;

namespace WFAI.Application.Tests.Handlers.Categories;

public class GetAllCategoriesQueryHandlerTests
{
    [Fact]
    public async Task Handle_should_return_cached_categories_when_cache_contains_value()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var cachedCategories = new List<CategoryListDto>
        {
            new(4, "Cached", "cached", null, 7)
        };
        scope.Cache.Set(CategoryCacheKeys.GetAll(true), cachedCategories);
        var handler = new GetAllCategoriesQueryHandler(scope.DbContext, scope.Cache);

        var result = await handler.Handle(new GetAllCategoriesQuery(true), CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(cachedCategories);
        scope.Cache.SetKeys.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_should_filter_sort_and_cache_categories_when_cache_misses()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        await scope.SeedCategoryAsync("Gamma", "gamma", 3, isActive: true);
        await scope.SeedCategoryAsync("Alpha", "alpha", 1, isActive: true);
        await scope.SeedCategoryAsync("Beta", "beta", 2, isActive: false);
        await scope.SeedCategoryAsync("Deleted", "deleted", 4, isActive: true, softDeleted: true);
        var handler = new GetAllCategoriesQueryHandler(scope.DbContext, scope.Cache);

        var result = await handler.Handle(new GetAllCategoriesQuery(true), CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Select(x => x.Name).Should().Equal("Alpha", "Gamma");
        scope.Cache.SetKeys.Should().Contain(CategoryCacheKeys.GetAll(true));
    }
}

public class GetAllCategoriesForListQueryHandlerTests
{
    [Fact]
    public async Task Handle_should_return_cached_lookup_list_when_cache_contains_value()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var cachedCategories = new List<CategoryLookupDto>
        {
            new(4, "Cached")
        };
        scope.Cache.Set(CategoryCacheKeys.GetAllForList, cachedCategories);
        var handler = new GetAllCategoriesForListQueryHandler(scope.DbContext, scope.Cache);

        var result = await handler.Handle(new GetAllCategoriesForListQuery(), CancellationToken.None);

        result.Data.Should().BeEquivalentTo(cachedCategories);
    }

    [Fact]
    public async Task Handle_should_return_only_active_categories_sorted_for_lookup_when_cache_misses()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        await scope.SeedCategoryAsync("Gamma", "gamma", 2, isActive: true);
        await scope.SeedCategoryAsync("Alpha", "alpha", 1, isActive: true);
        await scope.SeedCategoryAsync("Disabled", "disabled", 0, isActive: false);
        var handler = new GetAllCategoriesForListQueryHandler(scope.DbContext, scope.Cache);

        var result = await handler.Handle(new GetAllCategoriesForListQuery(), CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Select(x => x.Name).Should().Equal("Alpha", "Gamma");
        scope.Cache.SetKeys.Should().Contain(CategoryCacheKeys.GetAllForList);
    }
}

public class GetCategoryByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_should_return_category_with_parent_name_when_match_exists()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var parent = await scope.SeedCategoryAsync("Parent", "parent", 1);
        var child = await scope.SeedCategoryAsync("Child", "child", 2, parentId: parent.Id);
        var handler = new GetCategoryByIdQueryHandler(scope.DbContext);

        var result = await handler.Handle(new GetCategoryByIdQuery(child.Id), CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(new CategoryDto(child.Id, "Child", "child", "Parent"));
    }

    [Fact]
    public async Task Handle_should_return_failure_when_category_is_inactive_or_missing()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var inactive = await scope.SeedCategoryAsync("Inactive", "inactive", 1, isActive: false);
        var handler = new GetCategoryByIdQueryHandler(scope.DbContext);

        var result = await handler.Handle(new GetCategoryByIdQuery(inactive.Id), CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("Category not found.");
    }
}

public class GetCategoriesPagedQueryHandlerTests
{
    [Fact]
    public async Task Handle_should_filter_sort_and_page_active_categories()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();

        await scope.SeedCategoryAsync("Alpha", "alpha", 1, isActive: false);
        await scope.SeedCategoryAsync("Beta", "beta", 2, isActive: true);
        await scope.SeedCategoryAsync("Gamma", "gamma", 3, isActive: true);

        var query = new GetCategoriesPagedQuery
        {
            PagedFilterRequest = new()
            {
                SearchTerm = "a",
                IsActive = true,
                SortBy = "name",
                SortDirection = "desc",
                PageNumber = 1,
                PageSize = 2
            }
        };

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(u => u.IsAuthenticated()).Returns(true);
        mockCurrentUserService.Setup(u => u.HasClaim(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var handler = new GetCategoriesPagedQueryHandler(scope.DbContext, mockCurrentUserService.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Data.Select(x => x.Name).Should().Equal("Gamma", "Beta");
    }
}

public class GetCategoriesListQueryHandlerTests
{
    [Fact]
    public async Task Handle_should_filter_and_sort_categories()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();

        await scope.SeedCategoryAsync("Alpha", "alpha", 1, isActive: false);
        await scope.SeedCategoryAsync("Beta", "beta", 2, isActive: true);
        await scope.SeedCategoryAsync("Gamma", "gamma", 3, isActive: true);

        var query = new GetCategoriesListQuery("a", true, "name", "desc");

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(u => u.IsAuthenticated()).Returns(true);
        mockCurrentUserService.Setup(u => u.HasClaim(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var handler = new GetCategoriesListQueryHandler(scope.DbContext, mockCurrentUserService.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Count.Should().Be(2);
        result.Data.Select(x => x.Name).Should().Equal("Gamma", "Beta");
    }
}

public class GetCategoryByIdAdminQueryHandlerTests
{
    [Fact]
    public async Task Handle_should_return_admin_category_details_when_match_exists()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var parent = await scope.SeedCategoryAsync("Parent", "parent", 1);
        var child = await scope.SeedCategoryAsync("Child", "child", 2, isActive: false, parentId: parent.Id, rowVersion: [4]);
        var handler = new GetCategoryByIdAdminQueryHandler(scope.DbContext);

        var result = await handler.Handle(new GetCategoryByIdAdminQuery(child.Id), CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data!.ParentName.Should().Be("Parent");
        result.Data.IsActive.Should().BeFalse();
        result.Data.RowVersion.Should().Equal([4]);
    }

    [Fact]
    public async Task Handle_should_return_failure_when_category_is_soft_deleted_or_missing()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var deleted = await scope.SeedCategoryAsync("Deleted", "deleted", 1, softDeleted: true);
        var handler = new GetCategoryByIdAdminQueryHandler(scope.DbContext);

        var result = await handler.Handle(new GetCategoryByIdAdminQuery(deleted.Id), CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("Category not found or has been deleted.");
    }
}

public class GetAllCategoriesAdminQueryHandlerTests
{
    [Fact]
    public async Task Handle_should_return_cached_admin_categories_when_cache_contains_value()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var cached = new List<CategoryListAdminDto>
        {
            new(1, "Cached", "cached", null, true, 5)
        };
        scope.Cache.Set(CategoryCacheKeys.GetAllAdmin, cached);
        var handler = new GetAllCategoriesAdminQueryHandler(scope.DbContext, scope.Cache);

        var result = await handler.Handle(new GetAllCategoriesAdminQuery(), CancellationToken.None);

        result.Data.Should().BeEquivalentTo(cached);
    }

    [Fact]
    public async Task Handle_should_return_admin_categories_sorted_and_excluding_soft_deleted_records()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        await scope.SeedCategoryAsync("Gamma", "gamma", 2, isActive: false);
        await scope.SeedCategoryAsync("Alpha", "alpha", 1, isActive: true);
        await scope.SeedCategoryAsync("Deleted", "deleted", 3, isActive: true, softDeleted: true);
        var handler = new GetAllCategoriesAdminQueryHandler(scope.DbContext, scope.Cache);

        var result = await handler.Handle(new GetAllCategoriesAdminQuery(), CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Select(x => x.Name).Should().Equal("Alpha", "Gamma");
        scope.Cache.SetKeys.Should().Contain(CategoryCacheKeys.GetAllAdmin);
    }
}

public class GetCategoriesPagedAdminQueryHandlerTests
{
    [Fact]
    public async Task Handle_should_filter_sort_and_page_admin_categories()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var parent = await scope.SeedCategoryAsync("Parent", "parent", 0);
        await scope.SeedCategoryAsync("Gamma", "gamma", 3, isActive: true, parentId: parent.Id);
        await scope.SeedCategoryAsync("Beta", "beta", 2, isActive: true);
        await scope.SeedCategoryAsync("Alpha", "alpha", 1, isActive: false);
        var handler = new GetCategoriesPagedAdminQueryHandler(scope.DbContext);
        var query = new GetCategoriesPagedAdminQuery
        {
            PagedFilterRequest = new()
            {
                SearchTerm = "a",
                IsActive = true,
                SortBy = "sortorder",
                SortDirection = "desc",
                PageNumber = 1,
                PageSize = 2
            }
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(3);
        result.Data.Data.Select(x => x.Name).Should().Equal("Gamma", "Beta");
    }
}