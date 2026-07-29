using System.Collections.Generic;
using Jellyfin.Xtream.Client.Models;
using Jellyfin.Xtream.Service;

namespace Jellyfin.Xtream.Tests;

public class SeriesTitleResolverTests
{
    [Fact]
    public void ResolvePrefersSeriesListingName()
    {
        Series series = new() { SeriesId = 1, Name = "  Breaking Bad  " };
        SeriesStreamInfo info = new() { Info = new SeriesInfo { Name = "Ignored" } };

        Assert.Equal("Breaking Bad", SeriesTitleResolver.Resolve(series, info));
    }

    [Fact]
    public void ResolveFallsBackToSeriesInfoName()
    {
        Series series = new() { SeriesId = 1, Name = "   " };
        SeriesStreamInfo info = new() { Info = new SeriesInfo { Name = "The Wire" } };

        Assert.Equal("The Wire", SeriesTitleResolver.Resolve(series, info));
    }

    [Fact]
    public void ResolveDerivesFromEpisodesWhenNamesEmpty()
    {
        Series series = new() { SeriesId = 26332, Name = string.Empty };
        SeriesStreamInfo info = new()
        {
            Info = new SeriesInfo { Name = string.Empty },
            Episodes = new Dictionary<int, ICollection<Episode>>
            {
                [1] = new List<Episode>
                {
                    new() { EpisodeId = 1, Title = "9-1-1 (2018) - S01E01 - Pilot" },
                    new() { EpisodeId = 2, Title = "9-1-1 (2018) - S01E02 - Let Go" },
                },
            },
        };

        Assert.Equal("9-1-1 (2018)", SeriesTitleResolver.Resolve(series, info));
    }

    [Fact]
    public void ResolveReturnsEmptyWhenNothingAvailable()
    {
        Series series = new() { SeriesId = 1, Name = string.Empty };
        SeriesStreamInfo info = new() { Info = new SeriesInfo { Name = string.Empty } };

        Assert.Equal(string.Empty, SeriesTitleResolver.Resolve(series, info));
    }

    [Theory]
    [InlineData("9-1-1 (2018) - S01E01 - Pilot", "9-1-1 (2018)")]
    [InlineData("The Office (US) S02E05 Halloween", "The Office (US)")]
    [InlineData("Dark: S1E1: Secrets", "Dark")]
    public void DeriveExtractsNameBeforeMarker(string title, string expected)
    {
        Assert.Equal(expected, SeriesTitleResolver.DeriveFromEpisodeTitles(new[] { title }));
    }

    [Fact]
    public void DerivePicksMostCommonCandidate()
    {
        string[] titles =
        {
            "Show A - S01E01 - One",
            "Show A - S01E02 - Two",
            "Weird Bonus - S01E00 - Extra",
        };

        Assert.Equal("Show A", SeriesTitleResolver.DeriveFromEpisodeTitles(titles));
    }

    [Fact]
    public void DeriveReturnsNullWithoutMarkers()
    {
        string[] titles = { "Pilot", "Episode Two", string.Empty };

        Assert.Null(SeriesTitleResolver.DeriveFromEpisodeTitles(titles));
    }
}
