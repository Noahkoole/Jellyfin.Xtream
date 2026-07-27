using Jellyfin.Xtream.Service;

namespace Jellyfin.Xtream.Tests;

public sealed class StrmExportManifestStoreTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(
        Path.GetTempPath(),
        "jellyfin-xtream-manifest-tests",
        Guid.NewGuid().ToString("N"));

    public StrmExportManifestStoreTests()
    {
        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public async Task FirstSuccessfulRunNeverDeletesUntrackedStrmFiles()
    {
        string manualPath = Path.Combine(_rootPath, "manual.strm");
        string managedRelativePath = "Movie [xtream-vod-1]/Movie [xtream-vod-1].strm";
        string managedPath = StrmExportPathPolicy.ResolveGeneratedPath(_rootPath, managedRelativePath);
        await WriteFileAsync(manualPath, "manual");
        await WriteFileAsync(managedPath, "managed");
        StrmExportManifestStore store = new(_rootPath, "vod");

        StrmExportManifestLoadResult previous = await store.LoadAsync(CancellationToken.None);
        int deleted = await store.ReconcileAndCommitAsync(
            previous,
            [new("vod:1", managedRelativePath)],
            CancellationToken.None);

        Assert.Equal(StrmExportManifestState.Missing, previous.State);
        Assert.Equal(0, deleted);
        Assert.True(File.Exists(manualPath));
        Assert.True(File.Exists(managedPath));
        Assert.True(File.Exists(Path.Combine(_rootPath, StrmExportManifestStore.ManifestFileName)));
    }

    [Fact]
    public async Task LaterRunDeletesOnlyFilesOwnedByPreviousManifest()
    {
        string retainedRelativePath = "One [xtream-vod-1]/One [xtream-vod-1].strm";
        string staleRelativePath = "Two [xtream-vod-2]/Two [xtream-vod-2].strm";
        string retainedPath = StrmExportPathPolicy.ResolveGeneratedPath(_rootPath, retainedRelativePath);
        string stalePath = StrmExportPathPolicy.ResolveGeneratedPath(_rootPath, staleRelativePath);
        string manualPath = Path.Combine(_rootPath, "manual.strm");
        await WriteFileAsync(retainedPath, "one");
        await WriteFileAsync(stalePath, "two");
        await WriteFileAsync(manualPath, "manual");
        StrmExportManifestStore store = new(_rootPath, "vod");
        StrmExportManifestLoadResult missing = await store.LoadAsync(CancellationToken.None);
        await store.ReconcileAndCommitAsync(
            missing,
            [new("vod:1", retainedRelativePath), new("vod:2", staleRelativePath)],
            CancellationToken.None);

        StrmExportManifestLoadResult previous = await store.LoadAsync(CancellationToken.None);
        int deleted = await store.ReconcileAndCommitAsync(
            previous,
            [new("vod:1", retainedRelativePath)],
            CancellationToken.None);

        Assert.Equal(1, deleted);
        Assert.True(File.Exists(retainedPath));
        Assert.False(File.Exists(stalePath));
        Assert.True(File.Exists(manualPath));
    }

    [Fact]
    public async Task LaterRunDeletesOwnedNfoSidecarsTogetherWithStaleStrmFiles()
    {
        string retainedStrm = "One [xtream-vod-1]/One [xtream-vod-1].strm";
        string retainedNfo = "One [xtream-vod-1]/One [xtream-vod-1].nfo";
        string staleStrm = "Two [xtream-vod-2]/Two [xtream-vod-2].strm";
        string staleNfo = "Two [xtream-vod-2]/Two [xtream-vod-2].nfo";
        StrmExportManifestStore store = new(_rootPath, "vod");
        await WriteFileAsync(StrmExportPathPolicy.ResolveGeneratedPath(_rootPath, retainedStrm), "one");
        await WriteFileAsync(StrmExportPathPolicy.ResolveGeneratedPath(_rootPath, retainedNfo), "one nfo");
        string staleStrmPath = StrmExportPathPolicy.ResolveGeneratedPath(_rootPath, staleStrm);
        string staleNfoPath = StrmExportPathPolicy.ResolveGeneratedPath(_rootPath, staleNfo);
        await WriteFileAsync(staleStrmPath, "two");
        await WriteFileAsync(staleNfoPath, "two nfo");

        await store.ReconcileAndCommitAsync(
            await store.LoadAsync(CancellationToken.None),
            [new("vod:1", retainedStrm), new("vod:1:nfo", retainedNfo), new("vod:2", staleStrm), new("vod:2:nfo", staleNfo)],
            CancellationToken.None);

        int deleted = await store.ReconcileAndCommitAsync(
            await store.LoadAsync(CancellationToken.None),
            [new("vod:1", retainedStrm), new("vod:1:nfo", retainedNfo)],
            CancellationToken.None);

        Assert.Equal(2, deleted);
        Assert.False(File.Exists(staleStrmPath));
        Assert.False(File.Exists(staleNfoPath));
    }

    [Fact]
    public async Task SuccessfulRunRemovesLegacyTaggedPathsForReturnedIdentities()
    {
        string currentRelativePath = "Clean Title [xtream-vod-1]/Clean Title [xtream-vod-1].strm";
        string legacyRelativePath = "NL - Clean Title [xtream-vod-1]/NL - Clean Title [xtream-vod-1].strm";
        string duplicateRelativePath = "Duplicate Title [xtream-vod-2]/Duplicate Title [xtream-vod-2].strm";
        string unrelatedRelativePath = "Other [xtream-vod-3]/Other [xtream-vod-3].strm";
        await WriteFileAsync(StrmExportPathPolicy.ResolveGeneratedPath(_rootPath, currentRelativePath), "current");
        string legacyPath = StrmExportPathPolicy.ResolveGeneratedPath(_rootPath, legacyRelativePath);
        string duplicatePath = StrmExportPathPolicy.ResolveGeneratedPath(_rootPath, duplicateRelativePath);
        string unrelatedPath = StrmExportPathPolicy.ResolveGeneratedPath(_rootPath, unrelatedRelativePath);
        await WriteFileAsync(legacyPath, "legacy");
        await WriteFileAsync(duplicatePath, "duplicate");
        await WriteFileAsync(unrelatedPath, "unrelated");
        StrmExportManifestStore store = new(_rootPath, "vod");

        int deleted = await store.ReconcileAndCommitAsync(
            await store.LoadAsync(CancellationToken.None),
            [new("vod:1", currentRelativePath)],
            ["xtream-vod-1", "xtream-vod-2"],
            CancellationToken.None);

        Assert.Equal(2, deleted);
        Assert.False(File.Exists(legacyPath));
        Assert.False(File.Exists(duplicatePath));
        Assert.True(File.Exists(unrelatedPath));
    }

    [Fact]
    public async Task InvalidManifestCannotTriggerCleanupOrBeOverwritten()
    {
        string manifestPath = Path.Combine(_rootPath, StrmExportManifestStore.ManifestFileName);
        string candidatePath = Path.Combine(_rootPath, "candidate.strm");
        await WriteFileAsync(manifestPath, "not-json");
        await WriteFileAsync(candidatePath, "keep");
        StrmExportManifestStore store = new(_rootPath, "vod");

        StrmExportManifestLoadResult previous = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(StrmExportManifestState.Invalid, previous.State);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ReconcileAndCommitAsync(
            previous,
            [new("vod:1", "managed.strm")],
            CancellationToken.None));
        Assert.True(File.Exists(candidatePath));
        Assert.Equal("not-json", await File.ReadAllTextAsync(manifestPath));
    }

    [Fact]
    public async Task ExplicitEmptyManifestRemovesPreviouslyManagedFiles()
    {
        string staleRelativePath = "Stale [xtream-vod-1]/Stale [xtream-vod-1].strm";
        string stalePath = StrmExportPathPolicy.ResolveGeneratedPath(_rootPath, staleRelativePath);
        await WriteFileAsync(stalePath, "stale");
        StrmExportManifestStore store = new(_rootPath, "vod");
        StrmExportManifestLoadResult missing = await store.LoadAsync(CancellationToken.None);
        await store.ReconcileAndCommitAsync(
            missing,
            [new("vod:1", staleRelativePath)],
            CancellationToken.None);
        StrmExportManifestLoadResult previous = await store.LoadAsync(CancellationToken.None);

        int deleted = await store.ReconcileAndCommitAsync(previous, [], CancellationToken.None);

        Assert.Equal(1, deleted);
        Assert.False(File.Exists(stalePath));
    }

    [Fact]
    public async Task CancellationBeforeReconciliationPreservesManagedFiles()
    {
        string staleRelativePath = "Stale [xtream-vod-1]/Stale [xtream-vod-1].strm";
        string stalePath = StrmExportPathPolicy.ResolveGeneratedPath(_rootPath, staleRelativePath);
        await WriteFileAsync(stalePath, "stale");
        StrmExportManifestStore store = new(_rootPath, "vod");
        StrmExportManifestLoadResult missing = await store.LoadAsync(CancellationToken.None);
        await store.ReconcileAndCommitAsync(
            missing,
            [new("vod:1", staleRelativePath)],
            CancellationToken.None);
        StrmExportManifestLoadResult previous = await store.LoadAsync(CancellationToken.None);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => store.ReconcileAndCommitAsync(
            previous,
            [new("vod:2", "Replacement [xtream-vod-2]/Replacement [xtream-vod-2].strm")],
            cancellation.Token));

        Assert.True(File.Exists(stalePath));
    }

    [Fact]
    public async Task AtomicWriteLeavesOnlyCompleteTarget()
    {
        string path = Path.Combine(_rootPath, "atomic.strm");
        await WriteFileAsync(path, "old");

        await StrmExportManifestStore.WriteTextAtomicallyAsync(path, "new", CancellationToken.None);

        Assert.Equal("new", await File.ReadAllTextAsync(path));
        Assert.Empty(Directory.EnumerateFiles(_rootPath, "*.tmp", SearchOption.AllDirectories));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, true);
        }
    }

    private static async Task WriteFileAsync(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content);
    }
}
