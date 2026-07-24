using WFAI.Domain.Tests.Builders;

namespace WFAI.Domain.Tests.Entities;

public class CategoryTests
{
    [Fact]
    public void New_category_should_start_with_expected_defaults()
    {
        var category = new CategoryBuilder().Build();

        category.Name.Should().Be("Accessories");
        category.Slug.Should().Be("accessories");
        category.IsActive.Should().BeTrue();
        category.Children.Should().NotBeNull().And.BeEmpty();
        category.RowVersion.Should().NotBeNull().And.BeEmpty();
        category.SoftDeleted.Should().BeFalse();
    }

    [Fact]
    public void Category_should_support_parent_child_relationships()
    {
        var parent = new CategoryBuilder().WithId(12).Build();
        var child = new CategoryBuilder().WithId(30).WithParent(parent).Build();

        parent.Children.Add(child);

        child.ParentId.Should().Be(12);
        child.Parent.Should().BeSameAs(parent);
        parent.Children.Should().ContainSingle().Which.Should().BeSameAs(child);
    }

    [Fact]
    public void Category_should_preserve_soft_delete_and_concurrency_metadata()
    {
        var category = new CategoryBuilder()
            .Deleted()
            .WithConcurrencyToken(1, 2, 3, 4)
            .Build();

        category.SoftDeleted.Should().BeTrue();
        category.DeletedBy.Should().Be(7);
        category.DeletedAt.Should().HaveValue();
        category.RowVersion.Should().Equal(1, 2, 3, 4);
    }
}