using FluentAssertions;
using WFAI.Domain.Entities;
using Xunit;

namespace WFAI.Domain.Tests.Entities;

public class PhaseTests
{
    [Fact]
    public void New_phase_should_start_with_expected_defaults()
    {
        var phase = new Phase
        {
            Title = "Pre-boarding",
            NormalizedTitle = "PRE-BOARDING",
            Description = "Preparation phase before employee arrives",
            SortOrder = 1,
            IsActive = true
        };

        phase.Title.Should().Be("Pre-boarding");
        phase.NormalizedTitle.Should().Be("PRE-BOARDING");
        phase.Description.Should().Be("Preparation phase before employee arrives");
        phase.SortOrder.Should().Be(1);
        phase.IsActive.Should().BeTrue();
        phase.RowVersion.Should().NotBeNull().And.BeEmpty();
        phase.SoftDeleted.Should().BeFalse();
    }

    [Fact]
    public void Phase_should_preserve_soft_delete_and_concurrency_metadata()
    {
        var now = DateTime.UtcNow;
        var phase = new Phase
        {
            Title = "Orientation",
            NormalizedTitle = "ORIENTATION",
            SoftDeleted = true,
            DeletedBy = 5,
            DeletedAt = now,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        phase.SoftDeleted.Should().BeTrue();
        phase.DeletedBy.Should().Be(5);
        phase.DeletedAt.Should().Be(now);
        phase.RowVersion.Should().Equal(1, 2, 3, 4);
    }
}
