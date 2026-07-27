using Jellyfin.Xtream.Client.Models;
using Jellyfin.Xtream.Service;

namespace Jellyfin.Xtream.Tests;

public sealed class VodNfoWriterTests
{
    [Fact]
    public void CreatesProviderTitleYearAndTmdbIdentity()
    {
        string nfo = VodNfoWriter.Create(
            "The Wave (2019)",
            new VodInfo
            {
                ReleaseDate = new DateTime(2019, 5, 16),
                TmdbId = 12345,
            });

        Assert.Contains("<title>The Wave (2019)</title>", nfo, StringComparison.Ordinal);
        Assert.Contains("<year>2019</year>", nfo, StringComparison.Ordinal);
        Assert.Contains("type=\"tmdb\"", nfo, StringComparison.Ordinal);
        Assert.Contains(">12345</uniqueid>", nfo, StringComparison.Ordinal);
    }

    [Fact]
    public void SidecarPathReplacesOnlyTheFinalExtension()
    {
        Assert.Equal(
            "Movie [xtream-vod-42]/Movie [xtream-vod-42].mkv.nfo",
            VodNfoWriter.GetRelativePath("Movie [xtream-vod-42]/Movie [xtream-vod-42].mkv.strm"));
    }
}
