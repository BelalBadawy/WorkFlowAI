using Microsoft.EntityFrameworkCore;
using WFAI.Application.Features.Categories;
using WFAI.Application.Features.Categories.Commands.Create;
using WFAI.Application.Features.Categories.Commands.Delete;
using WFAI.Application.Features.Categories.Commands.Update;
using WFAI.Application.Features.Categories.Commands.ChangeCategoryStatus;
using WFAI.Application.Features.Categories.Commands.RestoreCategory;
using WFAI.Application.Features.Categories.Events;
using WFAI.Application.Tests.Support.Categories;

namespace WFAI.Application.Tests.Handlers.Categories;

public class CreateCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_should_create_category_add_outbox_message_and_clear_category_caches()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var handler = new CreateCategoryCommandHandler(scope.DbContext, scope.Cache);
        var command = new CreateCategoryCommand("  New Category  ", "  new-category  ", null, true, 5);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        var category = await scope.DbContext.Categories.SingleAsync();
        category.Name.Should().Be("New Category");
        category.Slug.Should().Be("new-category");
        category.RowVersion.Should().Equal([0]);
        scope.Cache.RemovedKeys.Should().BeEquivalentTo(CategoryCacheKeys.All);
        var outbox = await scope.DbContext.OutboxMessages.SingleAsync();
        outbox.Type.Should().Contain(nameof(CategoryCreatedEvent));
        outbox.Payload.Should().Contain($"\"categoryId\":{category.Id}");
    }

    [Fact]
    public async Task Handle_should_fail_when_category_name_already_exists()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        await scope.SeedCategoryAsync("Existing", "existing", 1);
        var handler = new CreateCategoryCommandHandler(scope.DbContext, scope.Cache);

        var result = await handler.Handle(
            new CreateCategoryCommand(" existing ", "other-slug", null, true, 2),
            CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("Category with this name already exists.");
        scope.Cache.RemovedKeys.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_should_create_category_when_same_name_and_slug_only_exists_as_soft_deleted()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        await scope.SeedCategoryAsync("Electronics", "electronics", 1, softDeleted: true);
        
        var handler = new CreateCategoryCommandHandler(scope.DbContext, scope.Cache);
        var command = new CreateCategoryCommand("Electronics", "electronics", null, true, 5);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().BeGreaterThan(0);

        var activeCategory = await scope.DbContext.Categories.SingleAsync(c => c.Id == result.Data);
        activeCategory.Name.Should().Be("Electronics");
        activeCategory.Slug.Should().Be("electronics");
        activeCategory.SoftDeleted.Should().BeFalse();
    }
}

public class UpdateCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_should_update_category_add_outbox_message_and_clear_category_caches()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var category = await scope.SeedCategoryAsync("Existing", "existing", 1, rowVersion: [3]);
        var parent = await scope.SeedCategoryAsync("Parent", "parent", 2);
        var handler = new UpdateCategoryCommandHandler(scope.DbContext, scope.Cache);
        var command = new UpdateCategoryCommand(category.Id, " Updated ", " updated ", parent.Id, false, 9, [3]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        var updated = await scope.DbContext.Categories.SingleAsync(x => x.Id == category.Id);
        updated.Name.Should().Be("Updated");
        updated.Slug.Should().Be("updated");
        updated.ParentId.Should().Be(parent.Id);
        updated.IsActive.Should().BeFalse();
        updated.SortOrder.Should().Be(9);
        scope.Cache.RemovedKeys.Should().BeEquivalentTo(CategoryCacheKeys.All);
        var outbox = await scope.DbContext.OutboxMessages.SingleAsync();
        outbox.Type.Should().Contain(nameof(CategoryUpdatedEvent));
        outbox.Payload.Should().Contain($"\"categoryId\":{category.Id}");
    }

    [Fact]
    public async Task Handle_should_fail_when_category_does_not_exist()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var handler = new UpdateCategoryCommandHandler(scope.DbContext, scope.Cache);

        var result = await handler.Handle(
            new UpdateCategoryCommand(404, "Name", "slug", null, true, 1, [1]),
            CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("Category not found.");
    }

    [Fact]
    public async Task Handle_should_fail_when_update_hits_concurrency_conflict()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var category = await scope.SeedCategoryAsync("Existing", "existing", 1, rowVersion: [2]);
        scope.DbContext.ThrowConcurrencyOnSave = true;
        var handler = new UpdateCategoryCommandHandler(scope.DbContext, scope.Cache);

        var result = await handler.Handle(
            new UpdateCategoryCommand(category.Id, "Updated", "updated", null, true, 2, [1]),
            CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Messages.Should().Contain(message => message.Contains("Concurrency conflict"));
        scope.Cache.RemovedKeys.Should().BeEmpty();
    }
}

public class DeleteCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_should_fail_when_category_has_children()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var parent = await scope.SeedCategoryAsync("Parent", "parent", 1);
        await scope.SeedCategoryAsync("Child", "child", 2, parentId: parent.Id);
        var handler = new DeleteCategoryCommandHandler(scope.DbContext, scope.Cache);

        var result = await handler.Handle(new DeleteCategoryCommand(parent.Id), CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("Cannot delete category with children.");
    }

    [Fact]
    public async Task Handle_should_delete_category_add_outbox_message_and_clear_category_caches()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var category = await scope.SeedCategoryAsync("Delete Me", "delete-me", 1);
        var handler = new DeleteCategoryCommandHandler(scope.DbContext, scope.Cache);

        var result = await handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        (await scope.DbContext.Categories.CountAsync()).Should().Be(0);
        scope.Cache.RemovedKeys.Should().BeEquivalentTo(CategoryCacheKeys.All);
        var outbox = await scope.DbContext.OutboxMessages.SingleAsync();
        outbox.Type.Should().Contain(nameof(CategoryDeletedEvent));
        outbox.Payload.Should().Contain($"\"categoryId\":{category.Id}");
    }
}

public class ChangeCategoryStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_should_update_status_add_outbox_message_and_clear_category_caches()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var category = await scope.SeedCategoryAsync("Existing", "existing", 1);
        var handler = new ChangeCategoryStatusHandler(scope.DbContext, scope.Cache);
        var command = new ChangeCategoryStatusCommand(category.Id, false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().Be(category.Id);
        var updated = await scope.DbContext.Categories.SingleAsync(x => x.Id == category.Id);
        updated.IsActive.Should().BeFalse();
        scope.Cache.RemovedKeys.Should().BeEquivalentTo(CategoryCacheKeys.All);
        var outbox = await scope.DbContext.OutboxMessages.SingleAsync();
        outbox.Type.Should().Contain(nameof(CategoryUpdatedEvent));
        outbox.Payload.Should().Contain($"\"categoryId\":{category.Id}");
    }

    [Fact]
    public async Task Handle_should_fail_when_category_does_not_exist()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var handler = new ChangeCategoryStatusHandler(scope.DbContext, scope.Cache);

        var result = await handler.Handle(
            new ChangeCategoryStatusCommand(404, false),
            CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Messages.Should().Contain("Category not found.");
    }
}

public class RestoreCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_should_restore_deleted_category_add_outbox_message_and_clear_caches()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var category = await scope.SeedCategoryAsync("Deleted Category", "deleted-category", 1, softDeleted: true);
        var handler = new RestoreCategoryCommandHandler(scope.DbContext, scope.Cache);
        var command = new RestoreCategoryCommand(category.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        result.Data.Should().Be(category.Id);

        var restored = await scope.DbContext.Categories.FirstOrDefaultAsync(c => c.Id == category.Id);
        restored.Should().NotBeNull();
        restored!.SoftDeleted.Should().BeFalse();
        restored.DeletedAt.Should().BeNull();
        restored.DeletedBy.Should().BeNull();

        scope.Cache.RemovedKeys.Should().BeEquivalentTo(CategoryCacheKeys.All);
        var outbox = await scope.DbContext.OutboxMessages.SingleAsync();
        outbox.Type.Should().Contain(nameof(CategoryRestoredEvent));
        outbox.Payload.Should().Contain($"\"categoryId\":{category.Id}");
    }

    [Fact]
    public async Task Handle_should_fail_when_category_not_found()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var handler = new RestoreCategoryCommandHandler(scope.DbContext, scope.Cache);
        var command = new RestoreCategoryCommand(999);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Messages.Should().Contain("Category not found.");
    }

    [Fact]
    public async Task Handle_should_fail_when_active_category_with_same_name_or_slug_exists()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        await scope.SeedCategoryAsync("Conflict Name", "unique-slug", 1, softDeleted: false);
        var deletedCategory = await scope.SeedCategoryAsync("Conflict Name", "deleted-slug", 2, softDeleted: true);
        
        var handler = new RestoreCategoryCommandHandler(scope.DbContext, scope.Cache);
        var command = new RestoreCategoryCommand(deletedCategory.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Messages.Should().Contain("Cannot restore: An active category with the same name or slug already exists.");
    }
}