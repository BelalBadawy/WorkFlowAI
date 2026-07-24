using WFAI.Application.Features.Categories.Queries.GetCategoriesPagedAdmin;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.Categories;

public class GetCategoriesPagedAdminQueryValidatorTests
{
    private readonly GetCategoriesPagedAdminQueryValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_supported_sort_field()
    {
        var query = new GetCategoriesPagedAdminQuery
        {
            PagedFilterRequest = TestData.PagedFilterRequest()
        };
        query.PagedFilterRequest.SortBy = "id";

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_when_sort_by_is_not_supported()
    {
        var query = new GetCategoriesPagedAdminQuery
        {
            PagedFilterRequest = TestData.PagedFilterRequest()
        };
        query.PagedFilterRequest.SortBy = "parent";

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "PagedFilterRequest.SortBy");
    }

    [Fact]
    public void Validate_should_fail_when_nested_paged_filter_is_invalid()
    {
        var query = new GetCategoriesPagedAdminQuery
        {
            PagedFilterRequest = TestData.PagedFilterRequest()
        };
        query.PagedFilterRequest.PageNumber = 0;

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "PagedFilterRequest.PageNumber");
    }
}