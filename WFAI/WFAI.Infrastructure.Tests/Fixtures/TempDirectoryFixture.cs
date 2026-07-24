namespace WFAI.Infrastructure.Tests.Fixtures;

public sealed class TempDirectoryFixture : IDisposable
{
    public TempDirectoryFixture()
    {
        RootPath = Path.Combine(
            Path.GetTempPath(),
            "ums-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}