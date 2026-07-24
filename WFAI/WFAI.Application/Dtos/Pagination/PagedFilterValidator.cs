namespace WFAI.Application.Dtos.Pagination
{
    /// <summary>
    /// Validator for <see cref="PagedFilterRequest"/> ensuring paging and sorting
    /// parameters are within acceptable ranges/values.
    /// </summary>
    public class PagedFilterValidator : AbstractValidator<PagedFilterRequest>
    {
        public PagedFilterValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("PageNumber must be greater than 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("PageSize must be greater than 0")
                .LessThanOrEqualTo(100)
                .WithMessage("PageSize cannot exceed 100");

            RuleFor(x => x.SortDirection)
                .Must(x => string.IsNullOrWhiteSpace(x) || x == "asc" || x == "desc")
                .WithMessage("SortDirection must be 'asc' or 'desc'");
        }
    }
}