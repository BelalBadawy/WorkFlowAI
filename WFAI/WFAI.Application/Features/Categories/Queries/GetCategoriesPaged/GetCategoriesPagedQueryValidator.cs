using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Features.Categories.Queries.GetCategoriesPaged;

namespace WFAI.Application.Features.Categories.Queries.GetCategoriesPaged
{
    public class GetCategoriesPagedQueryValidator : AbstractValidator<GetCategoriesPagedQuery>
    {
        private static readonly string[] AllowedSortFields = ["name", "slug", "sortorder", "id"];

        public GetCategoriesPagedQueryValidator()
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