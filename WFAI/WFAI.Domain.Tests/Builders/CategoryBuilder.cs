using WFAI.Domain.Tests.Support;

namespace WFAI.Domain.Tests.Builders;

internal sealed class CategoryBuilder
{
    private readonly Category _category = new()
    {
        Name = "Accessories",
        Slug = "accessories",
        IsActive = true,
        SortOrder = 10
    };

    public CategoryBuilder WithId(int id)
    {
        _category.WithId(id);
        return this;
    }

    public CategoryBuilder WithParent(Category parent)
    {
        _category.Parent = parent;
        _category.ParentId = parent.Id;
        return this;
    }

    public CategoryBuilder Deleted()
    {
        _category.SoftDeleted = true;
        _category.DeletedBy = 7;
        _category.DeletedAt = new DateTime(2026, 4, 23, 10, 0, 0, DateTimeKind.Utc);
        return this;
    }

    public CategoryBuilder WithConcurrencyToken(params byte[] rowVersion)
    {
        _category.RowVersion = rowVersion;
        return this;
    }

    public Category Build() => _category;
}