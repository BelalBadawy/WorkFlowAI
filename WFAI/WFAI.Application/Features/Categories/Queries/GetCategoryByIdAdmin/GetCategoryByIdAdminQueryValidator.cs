namespace WFAI.Application.Features.Categories.Queries.GetCategoryByIdAdmin
{
    public class GetCategoryByIdAdminQueryValidator : AbstractValidator<GetCategoryByIdAdminQuery>
    {
        public GetCategoryByIdAdminQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Category ID must be greater than 0.");
        }
    }
}