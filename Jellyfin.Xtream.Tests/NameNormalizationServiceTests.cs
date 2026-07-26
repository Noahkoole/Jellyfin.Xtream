using System.Diagnostics;
using Jellyfin.Xtream.Configuration;
using Jellyfin.Xtream.Service;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Xtream.Tests;

public class NameNormalizationServiceTests
{
    [Fact]
    public void SeparateEditorRulesAreAutomaticallyScoped()
    {
        PluginConfiguration configuration = new()
        {
            CategoryNameCleanupRules = "^CAT\\s*=>",
            LiveTvNameCleanupRules = "^LIVE\\s*=>",
            VodNameCleanupRules = "^MOVIE\\s*=>",
            SeriesNameCleanupRules = "^SHOW\\s*=>",
        };
        NameNormalizationService service = CreateService();

        Assert.Empty(service.UpdateRules(configuration.GetEffectiveNameCleanupRules()));
        Assert.Equal("Title", service.Normalize("CAT Title", NameScope.Category).Title);
        Assert.Equal("Title", service.Normalize("LIVE Title", NameScope.LiveChannel).Title);
        Assert.Equal("Title", service.Normalize("MOVIE Title", NameScope.Vod).Title);
        Assert.Equal("Title", service.Normalize("SHOW Title", NameScope.Series).Title);
        Assert.Equal("SHOW Title", service.Normalize("SHOW Title", NameScope.Vod).Title);
    }

    [Fact]
    public void LegacyRuleAppliesToEveryScope()
    {
        NameNormalizationService service = CreateService();
        Assert.Empty(service.UpdateRules(@"^PREFIX\s*-\s* =>"));

        Assert.Equal("Title", service.Normalize("PREFIX - Title", NameScope.LiveChannel).Title);
        Assert.Equal("Title", service.Normalize("PREFIX - Title", NameScope.Vod).Title);
        Assert.Equal("Title", service.Normalize("PREFIX - Title", NameScope.Filesystem).Title);
    }

    [Fact]
    public void ScopedRuleOnlyAppliesToSelectedScope()
    {
        NameNormalizationService service = CreateService();
        Assert.Empty(service.UpdateRules(@"[LiveChannel] ^PREFIX\s*-\s* =>"));

        Assert.Equal("Title", service.Normalize("PREFIX - Title", NameScope.LiveChannel).Title);
        Assert.Equal("PREFIX - Title", service.Normalize("PREFIX - Title", NameScope.Vod).Title);
    }

    [Fact]
    public void ExportNamesApplyBothContentAndFilesystemScopesOnce()
    {
        NameNormalizationService service = CreateService();
        Assert.Empty(service.UpdateRules("[Vod] ^VOD\\s*=>\n[Filesystem] \\s*FS$ =>"));
        NameNormalizationSnapshot snapshot = service.CreateSnapshot();

        Assert.Equal(
            "Title",
            StrmExportService.NormalizeExportTitle(snapshot, "VOD Title FS", NameScope.Vod));
        Assert.Equal(
            "VOD Title",
            StrmExportService.NormalizeExportTitle(snapshot, "VOD Title FS", NameScope.Series));
    }

    [Fact]
    public void SeriesTitleDeduplicationUsesCleanedTitleAndKeepsLowestProviderId()
    {
        NameNormalizationService service = CreateService();
        Assert.Empty(service.UpdateRules("[Series] ^(?:NL|EN)\\s*-\\s* =>"));

        List<Client.Models.Series> result = SeriesTitleDeduplicator.Deduplicate(
        [
            new() { SeriesId = 44, Name = "EN - Example Show" },
            new() { SeriesId = 12, Name = "NL - Example Show" },
            new() { SeriesId = 99, Name = "Different Show" },
        ],
        service.CreateSnapshot());

        Assert.Equal([99, 12], result.Select(item => item.SeriesId));
    }

    [Fact]
    public void VodTitleDeduplicationUsesCleanedTitleAndPrefers4KNl()
    {
        NameNormalizationService service = CreateService();
        Assert.Empty(service.UpdateRules("[Vod] (?i)^\\s*(?:(?:NL|AMZ|4K)(?:[-\\s]+(?:NL|AMZ|4K))*\\s*-\\s*)+(.+?)\\s*$ => $1"));

        List<Client.Models.StreamInfo> result = VodTitleDeduplicator.Deduplicate(
        [
            new() { StreamId = 44, Name = "AMZ - Example Movie" },
            new() { StreamId = 12, Name = "NL - Example Movie" },
            new() { StreamId = 99, Name = "4K-NL - Example Movie" },
        ],
        service.CreateSnapshot());

        Assert.Equal([99], result.Select(item => item.StreamId));
    }

    [Theory]
    [InlineData("DO - The Old Guard 2 (2025)", "The Old Guard 2 (2025)")]
    [InlineData("AR-SUBS - Love in 39 Degrees (2024)", "Love in 39 Degrees (2024)")]
    [InlineData("4M-AMZ - The Neverending Wedding (2025)", "The Neverending Wedding (2025)")]
    [InlineData("BE-NL - Django", "Django")]
    public void CompoundProviderPrefixRuleCleansObservedNames(string name, string expected)
    {
        NameNormalizationService service = CreateService();
        Assert.Empty(service.UpdateRules("[Vod,Series] (?i)^\\s*(?:(?:EN|NL|NF|TOP|AMZ|UNV|D\\+|PRMT|VP|A\\+|MRVL|DWA|SC|AL|ËN|4K|4M|UHD|FHD|HD|SD|AR|SUBS|DO|CAM|BE|DSC\\+|P\\+|SKY|SHWT|NICK)(?:[-\\s]+(?:EN|NL|NF|TOP|AMZ|UNV|D\\+|PRMT|VP|A\\+|MRVL|DWA|SC|AL|ËN|4K|4M|UHD|FHD|HD|SD|AR|SUBS|DO|CAM|BE|DSC\\+|P\\+|SKY|SHWT|NICK))*\\s*-\\s*)+(.+?)\\s*$ => $1"));

        Assert.Equal(expected, service.Normalize(name, NameScope.Vod).Title);
    }

    [Fact]
    public void CharacterClassAtStartRemainsALegacyRegex()
    {
        NameNormalizationService service = CreateService();
        Assert.Empty(service.UpdateRules("[A-Z]+ => lower"));

        Assert.Equal("lower", service.Normalize("TITLE", NameScope.Series).Title);
    }

    [Fact]
    public void ScopeNamedCharacterClassRemainsALegacyRegexWithoutFollowingWhitespace()
    {
        NameNormalizationService service = CreateService();
        Assert.Empty(service.UpdateRules("[LiveChannel]+ => x"));

        Assert.Equal("x", service.Normalize("Live", NameScope.Vod).Title);
    }

    [Fact]
    public void InvalidScopeAndRegexAreReportedWhileValidRulesRemainActive()
    {
        NameNormalizationService service = CreateService();
        IReadOnlyList<string> errors = service.UpdateRules(
            "[LiveChannel,Missing] ^PREFIX =>\n(?<broken =>\n^GOOD\\s*-\\s* =>");

        Assert.Equal(2, errors.Count);
        Assert.Equal("Title", service.Normalize("GOOD - Title", NameScope.Vod).Title);
    }

    [Fact]
    public void SnapshotDoesNotChangeAfterRulesAreUpdated()
    {
        NameNormalizationService service = CreateService();
        Assert.Empty(service.UpdateRules("^OLD => New"));
        NameNormalizationSnapshot oldSnapshot = service.CreateSnapshot();

        Assert.Empty(service.UpdateRules("^OLD => Latest"));
        NameNormalizationSnapshot latestSnapshot = service.CreateSnapshot();

        Assert.Equal("New Title", oldSnapshot.Normalize("OLD Title", NameScope.Vod).Title);
        Assert.Equal("Latest Title", latestSnapshot.Normalize("OLD Title", NameScope.Vod).Title);
        Assert.NotEqual(oldSnapshot.Version, latestSnapshot.Version);
    }

    [Fact]
    public void ConservativeTagParsingPreservesSemanticSuffixes()
    {
        NameNormalizationService service = CreateService();

        ParsedName parsed = service.Normalize("[NL] Dune [Extended Edition]", NameScope.Vod);

        Assert.Equal("Dune [Extended Edition]", parsed.Title);
        Assert.Equal(new[] { "NL" }, parsed.Tags);
    }

    [Fact]
    public void UnicodePipePrefixIsExtracted()
    {
        NameNormalizationService service = CreateService();

        ParsedName parsed = service.Normalize("┃NL┃ Juliet", NameScope.Series);

        Assert.Equal("Juliet", parsed.Title);
        Assert.Equal(new[] { "NL" }, parsed.Tags);
    }

    [Fact]
    public void UppercaseBlockPrefixesAreExtractedConservatively()
    {
        NameNormalizationService service = CreateService();

        ParsedName parsed = service.Normalize("NL ▉ SPORTS ▉ HBO", NameScope.LiveChannel);

        Assert.Equal("HBO", parsed.Title);
        Assert.Equal(new[] { "NL", "SPORTS" }, parsed.Tags);
    }

    [Fact]
    public void CatastrophicRegexTimesOutAndIsDisabled()
    {
        NameNormalizationService service = CreateService();
        Assert.Empty(service.UpdateRules("^(a+)+$ => removed"));
        string input = new string('a', 10_000) + "!";
        Stopwatch stopwatch = Stopwatch.StartNew();

        ParsedName parsed = service.Normalize(input, NameScope.LiveProgram);
        stopwatch.Stop();

        Assert.Equal(input, parsed.Title);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Regex took {stopwatch.Elapsed}.");
        Assert.Equal(input, service.Normalize(input, NameScope.LiveProgram).Title);
    }

    private static NameNormalizationService CreateService()
    {
        return new NameNormalizationService(NullLogger<NameNormalizationService>.Instance);
    }
}
