using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using WFAI.Application.Dtos.Cache;
using WFAI.Infrastructure.Services.Common;

namespace WFAI.Infrastructure.Tests.Services.Common;

public class DistributedCacheServiceTests
{
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly DistributedCacheService _sut;

    public DistributedCacheServiceTests()
    {
        var config = Options.Create(new CacheConfiguration { SlidingExpirationInMinutes = 10 });
        _sut = new DistributedCacheService(_cache.Object, config);
    }

    [Fact]
    public void TryGet_WhenKeyNotFound_ReturnsFalseAndDefaultValue()
    {
        _cache.Setup(c => c.Get("missing")).Returns((byte[]?)null);

        var found = _sut.TryGet<string>("missing", out var value);

        found.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGet_WhenKeyFound_ReturnsTrueAndDeserializedValue()
    {
        var payload = new { name = "Alice", score = 99 };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        _cache.Setup(c => c.Get("hit")).Returns(Encoding.UTF8.GetBytes(json));

        var found = _sut.TryGet<JsonElement>("hit", out var value);

        found.Should().BeTrue();
        value.GetProperty("name").GetString().Should().Be("Alice");
        value.GetProperty("score").GetInt32().Should().Be(99);
    }

    [Fact]
    public void Set_SerializesValueCachesItWithSlidingExpirationAndReturnsValue()
    {
        byte[]? capturedBytes = null;
        DistributedCacheEntryOptions? capturedOptions = null;
        _cache.Setup(c => c.Set(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()))
              .Callback<string, byte[], DistributedCacheEntryOptions>((_, b, o) =>
              {
                  capturedBytes = b;
                  capturedOptions = o;
              });

        var result = _sut.Set("k", "hello-world");

        result.Should().Be("hello-world");
        capturedBytes.Should().NotBeNull();
        Encoding.UTF8.GetString(capturedBytes!).Should().Contain("hello-world");
        capturedOptions!.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void Remove_DelegatesToUnderlyingCache()
    {
        _sut.Remove("stale-key");

        _cache.Verify(c => c.Remove("stale-key"), Times.Once);
    }
}