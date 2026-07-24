#pragma warning disable CS8600 // Moq out-parameter setup triggers nullable false-positives
using Microsoft.AspNetCore.Http;
using System.Text;
using WFAI.Infrastructure.Common;

namespace WFAI.Infrastructure.Tests.Services.Common;

public class InMemorySessionWrapperTests
{
    private static (InMemorySessionWrapper Sut, Mock<ISession> Session) Build()
    {
        var session = new Mock<ISession>();
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(c => c.Session).Returns(session.Object);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
        return (new InMemorySessionWrapper(accessor.Object), session);
    }

    [Fact]
    public void GetFromSession_WhenKeyAbsent_ReturnsDefault()
    {
        var (sut, session) = Build();
        byte[] absent = [];
        session.Setup(s => s.TryGetValue("missing", out absent)).Returns(false);

        var result = sut.GetFromSession<string>("missing");

        result.Should().BeNull();
    }

    [Fact]
    public void GetFromSession_WhenKeyPresent_ReturnsDeserializedValue()
    {
        var (sut, session) = Build();
        var json = "{\"name\":\"alice\",\"score\":42}";
        byte[] stored = Encoding.UTF8.GetBytes(json);
        session.Setup(s => s.TryGetValue("key", out stored)).Returns(true);

        var result = sut.GetFromSession<SessionPayload>("key");

        result.Should().NotBeNull();
        result!.Name.Should().Be("alice");
        result.Score.Should().Be(42);
    }

    [Fact]
    public void SetInSession_WhenValueIsNull_NeverCallsSet()
    {
        var (sut, session) = Build();

        sut.SetInSession<string?>("key", null);

        session.Verify(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public void SetInSession_WhenValueIsNotNull_StoresSerializedBytes()
    {
        var (sut, session) = Build();
        byte[]? captured = null;
        session.Setup(s => s.Set("key", It.IsAny<byte[]>()))
               .Callback<string, byte[]>((_, b) => captured = b);

        sut.SetInSession("key", new SessionPayload { Name = "bob", Score = 7 });

        session.Verify(s => s.Set("key", It.IsAny<byte[]>()), Times.Once);
        var decoded = Encoding.UTF8.GetString(captured!);
        decoded.Should().Contain("bob");
    }

    [Fact]
    public void RemoveFromSession_DelegatesToSession()
    {
        var (sut, session) = Build();

        sut.RemoveFromSession("old-key");

        session.Verify(s => s.Remove("old-key"), Times.Once);
    }

    private sealed class SessionPayload
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
    }
}