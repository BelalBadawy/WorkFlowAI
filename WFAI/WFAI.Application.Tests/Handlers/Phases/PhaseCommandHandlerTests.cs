using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WFAI.Application.Features.Phases;
using WFAI.Application.Features.Phases.Commands.ChangePhaseStatus;
using WFAI.Application.Features.Phases.Commands.Create;
using WFAI.Application.Features.Phases.Commands.Delete;
using WFAI.Application.Features.Phases.Commands.RestorePhase;
using WFAI.Application.Features.Phases.Commands.Update;
using WFAI.Application.Features.Phases.Events;
using WFAI.Application.Tests.Support.Categories;
using WFAI.Domain.Entities;
using Xunit;

namespace WFAI.Application.Tests.Handlers.Phases;

public class PhaseCommandHandlerTests
{
    [Fact]
    public async Task Handle_should_create_phase_add_outbox_message_and_clear_caches()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var handler = new CreatePhaseCommandHandler(scope.DbContext, scope.Cache);
        var command = new CreatePhaseCommand("  Pre-boarding  ", "Initial phase", true, 1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        var phase = await scope.DbContext.Phases.SingleAsync();
        phase.Title.Should().Be("Pre-boarding");
        phase.NormalizedTitle.Should().Be("PRE-BOARDING");
        phase.Description.Should().Be("Initial phase");
        phase.SortOrder.Should().Be(1);
        scope.Cache.RemovedKeys.Should().BeEquivalentTo(PhaseCacheKeys.All);
        var outbox = await scope.DbContext.OutboxMessages.SingleAsync();
        outbox.Type.Should().Contain(nameof(PhaseCreatedEvent));
    }

    [Fact]
    public async Task Handle_should_fail_when_phase_title_already_exists()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        scope.DbContext.Phases.Add(new Phase
        {
            Title = "Orientation",
            NormalizedTitle = "ORIENTATION",
            SortOrder = 1,
            IsActive = true
        });
        await scope.DbContext.SaveChangesAsync();

        var handler = new CreatePhaseCommandHandler(scope.DbContext, scope.Cache);
        var result = await handler.Handle(
            new CreatePhaseCommand(" orientation ", "Duplicate title", true, 2),
            CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.Messages.Should().Contain("Phase with this title already exists.");
    }

    [Fact]
    public async Task Handle_should_update_phase_successfully()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var phase = new Phase
        {
            Title = "Old Phase",
            NormalizedTitle = "OLD PHASE",
            Description = "Old description",
            SortOrder = 1,
            IsActive = true,
            RowVersion = new byte[] { 1 }
        };
        scope.DbContext.Phases.Add(phase);
        await scope.DbContext.SaveChangesAsync();

        var handler = new UpdatePhaseCommandHandler(scope.DbContext, scope.Cache);
        var command = new UpdatePhaseCommand(phase.Id, "Updated Phase", "New description", false, 10, new byte[] { 1 });

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        var updated = await scope.DbContext.Phases.FindAsync(phase.Id);
        updated!.Title.Should().Be("Updated Phase");
        updated.NormalizedTitle.Should().Be("UPDATED PHASE");
        updated.Description.Should().Be("New description");
        updated.IsActive.Should().BeFalse();
        updated.SortOrder.Should().Be(10);
    }

    [Fact]
    public async Task Handle_should_delete_phase_softly()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var phase = new Phase
        {
            Title = "Phase to delete",
            NormalizedTitle = "PHASE TO DELETE",
            IsActive = true
        };
        scope.DbContext.Phases.Add(phase);
        await scope.DbContext.SaveChangesAsync();

        var handler = new DeletePhaseCommandHandler(scope.DbContext, scope.Cache);
        var result = await handler.Handle(new DeletePhaseCommand(phase.Id), CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        var deleted = await scope.DbContext.Phases.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == phase.Id);
        deleted!.SoftDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_should_change_phase_status()
    {
        await using var scope = await CategoryHandlerTestScope.CreateAsync();
        var phase = new Phase
        {
            Title = "Active Phase",
            NormalizedTitle = "ACTIVE PHASE",
            IsActive = true
        };
        scope.DbContext.Phases.Add(phase);
        await scope.DbContext.SaveChangesAsync();

        var handler = new ChangePhaseStatusHandler(scope.DbContext, scope.Cache);
        var result = await handler.Handle(new ChangePhaseStatusCommand(phase.Id, false), CancellationToken.None);

        result.IsSuccessful.Should().BeTrue();
        var updated = await scope.DbContext.Phases.FindAsync(phase.Id);
        updated!.IsActive.Should().BeFalse();
    }
}
