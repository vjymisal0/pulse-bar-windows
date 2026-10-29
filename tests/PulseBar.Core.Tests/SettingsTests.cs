using PulseBar.Core;

namespace PulseBar.Core.Tests;

public sealed class SettingsTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("pulsebar-tests").FullName;

    private string FilePath => Path.Combine(_dir, "sub", "settings.json");

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void MissingFileGivesDefaults() =>
        Assert.Equal(1500, new SettingsStore(FilePath).Load().RefreshMs);

    [Fact]
    public void RoundTrip()
    {
        var store = new SettingsStore(FilePath);
        store.Save(new Settings { ShowNet = true, TempUnit = TempUnit.Fahrenheit, Dock = DockMode.Floating, FloatX = 10 });
        var loaded = store.Load();
        Assert.True(loaded.ShowNet);
        Assert.Equal(TempUnit.Fahrenheit, loaded.TempUnit);
        Assert.Equal(DockMode.Floating, loaded.Dock);
        Assert.Equal(10, loaded.FloatX);
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact]
    public void EnumsAreStoredAsReadableStrings()
    {
        new SettingsStore(FilePath).Save(new Settings { Dock = DockMode.TaskbarLeft });
        Assert.Contains("\"TaskbarLeft\"", File.ReadAllText(FilePath));
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("{\"Dock\": \"Sideways\"}")]
    [InlineData("null")]
    public void CorruptFileGivesDefaults(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, content);
        Assert.Equal(DockMode.TaskbarRight, new SettingsStore(FilePath).Load().Dock);
    }

    [Fact]
    public void OversizedFileIsIgnored()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, "{\"RefreshMs\": 3000," + new string(' ', 70_000) + "}");
        Assert.Equal(1500, new SettingsStore(FilePath).Load().RefreshMs);
    }

    [Fact]
    public void OutOfRangeValuesAreClamped()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, "{\"RefreshMs\": 1, \"TempWarn\": 90, \"TempCrit\": 50, \"Dock\": 99}");
        var s = new SettingsStore(FilePath).Load();
        Assert.Equal(Settings.MinRefreshMs, s.RefreshMs);
        Assert.Equal(90, s.TempWarn);
        Assert.Equal(90, s.TempCrit);
        Assert.Equal(DockMode.TaskbarRight, s.Dock);
    }
}
