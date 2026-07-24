using FluentAssertions;
using Xunit;
using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Features.AuditTrails.Queries.GetAuditTrailsPaged;
using WFAI.Application.Tests.Fixtures;

namespace WFAI.Application.Tests.Validation.AuditTrails;

public class GetAuditTrailsPagedQueryValidatorTests
{
    private readonly GetAuditTrailsPagedQueryValidator _validator = new();

    [Fact]
    public void Validate_should_pass_for_well_formed_request_with_datetime()
    {
        var query = new GetAuditTrailsPagedQuery
        {
            PagedFilterRequest = new PagedFilterRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = "datetime",
                SortDirection = "desc"
            }
        };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_pass_for_empty_sortby()
    {
        var query = new GetAuditTrailsPagedQuery
        {
            PagedFilterRequest = new PagedFilterRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = "",
                SortDirection = "desc"
            }
        };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_should_fail_for_invalid_sortby()
    {
        var query = new GetAuditTrailsPagedQuery
        {
            PagedFilterRequest = new PagedFilterRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = "invalidField",
                SortDirection = "desc"
            }
        };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "PagedFilterRequest.SortBy");
    }
}