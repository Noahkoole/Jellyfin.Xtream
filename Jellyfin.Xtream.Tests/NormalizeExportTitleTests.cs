using Jellyfin.Xtream.Service;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Xtream.Tests;

public class NormalizeExportTitleTests
{
    private static NameNormalizationSnapshot SnapshotWith(string rules)
    {
        NameNormalizationService service = new(NullLogger<NameNormalizationService>.Instance);
        service.UpdateRules(rules);
        return service.CreateSnapshot();
    }

    [Fact]
    public void KeepsOriginalWhenCleanupEmptiesTitle()
    {
        // ".*NL.*" with no replacement deletes any title that contains "NL",
        // which would otherwise export as a literal "Unknown" folder.
        NameNormalizationSnapshot snapshot = SnapshotWith("[Series,Season,Episode] .*NL.*");

        string result = StrmExportService.NormalizeExportTitle(snapshot, "NL - 9-1-1 (2018)", NameScope.Series);

        Assert.Equal("NL - 9-1-1 (2018)", result);
    }

    [Fact]
    public void AppliesCleanupWhenResultIsNonEmpty()
    {
        NameNormalizationSnapshot snapshot = SnapshotWith(@"[Series,Season,Episode] ^NL\s*[-|:]\s* =>");

        string result = StrmExportService.NormalizeExportTitle(snapshot, "NL - 9-1-1 (2018)", NameScope.Series);

        Assert.Equal("9-1-1 (2018)", result);
    }

    [Fact]
    public void ReturnsEmptyWhenInputItselfIsEmpty()
    {
        NameNormalizationSnapshot snapshot = SnapshotWith(string.Empty);

        string result = StrmExportService.NormalizeExportTitle(snapshot, "   ", NameScope.Series);

        Assert.Equal(string.Empty, result);
    }
}
