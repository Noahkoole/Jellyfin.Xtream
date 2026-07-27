// Copyright (C) 2022  Kevin Jilissen

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Xtream.Service;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Xtream.Tasks;

/// <summary>
/// Scheduled task for rebuilding configured STRM exports with title deduplication.
/// </summary>
public class StrmDeduplicationScheduledTask(StrmExportService strmExportService) : IScheduledTask
{
    /// <inheritdoc />
    public string Name => "Deduplicate Xtream STRM files";

    /// <inheritdoc />
    public string Key => "XtreamStrmDeduplication";

    /// <inheritdoc />
    public string Description => "Rebuilds selected Xtream STRM exports, retaining one preferred source per canonical title.";

    /// <inheritdoc />
    public string Category => "Xtream";

    /// <inheritdoc />
    public bool IsHidden => false;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => true;

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        return strmExportService.DeduplicateAsync(progress, cancellationToken);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];
}
