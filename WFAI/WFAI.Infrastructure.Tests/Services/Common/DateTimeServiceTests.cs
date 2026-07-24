using WFAI.Infrastructure.Services.Common;

namespace WFAI.Infrastructure.Tests.Services.Common;

public class DateTimeServiceTests
{
    [Fact]
    public void NowUtc_ReturnsValueWithinCurrentSecond()
    {
        var before = DateTime.UtcNow;
        var sut = new DateTimeService();

        var result = sut.NowUtc;

        var after = DateTime.UtcNow;
        result.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void NowUtc_KindIsUtc()
    {
        var sut = new DateTimeService();

        sut.NowUtc.Kind.Should().Be(DateTimeKind.Utc);
    }
}