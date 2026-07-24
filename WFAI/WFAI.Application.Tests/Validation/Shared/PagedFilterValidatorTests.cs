using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.Shared;

public class PagedFilterValidatorTests
{
    private readonly PagedFilterValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request()
    {
        var request = TestData.PagedFilterRequest();

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_should_fail_when_page_number_is_not_positive(int pageNumber)
    {
        var request = TestData.PagedFilterRequest();
        request.PageNumber = pageNumber;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(PagedFilterRequest.PageNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_should_fail_when_page_size_is_out_of_range(int pageSize)
    {
        var request = TestData.PagedFilterRequest();
        request.PageSize = pageSize;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(PagedFilterRequest.PageSize));
    }

    [Fact]
    public void Validate_should_fail_when_sort_direction_is_invalid()
    {
        var request = TestData.PagedFilterRequest();
        request.SortDirection = "sideways";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(PagedFilterRequest.SortDirection));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("asc")]
    [InlineData("desc")]
    public void Validate_should_pass_when_sort_direction_is_supported(string? sortDirection)
    {
        var request = TestData.PagedFilterRequest();
        request.SortDirection = sortDirection;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}