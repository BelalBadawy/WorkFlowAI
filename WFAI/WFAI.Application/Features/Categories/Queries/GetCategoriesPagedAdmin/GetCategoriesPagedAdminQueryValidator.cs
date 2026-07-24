using WFAI.Application.Dtos.Pagination;

namespace WFAI.Application.Features.Categories.Queries.GetCategoriesPagedAdmin
{
    public class GetCategoriesPagedAdminQueryValidator : AbstractValidator<GetCategoriesPagedAdminQuery>
    {
        private static readonly string[] AllowedSortFields = ["name", "slug", "sortorder", "id"];

        public GetCategoriesPagedAdminQueryValidator()
        {
            RuleFor(x => x.PagedFilterRequest)
                .SetValidator(new PagedFilterValidator());

            RuleFor(x => x.PagedFilterRequest.SortBy)
                .Must(sortBy => string.IsNullOrWhiteSpace(sortBy) ||
                                AllowedSortFields.Contains(sortBy.Trim(), StringComparer.OrdinalIgnoreCase))
                .WithMessage("SortBy must be one of: name, slug, sortorder, id.");
        }
    }
}