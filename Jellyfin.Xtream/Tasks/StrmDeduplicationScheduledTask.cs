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
    /// <summary>Gets the task name shown in Jellyfin.</summary>
    public string Name => "Deduplicate Xtream STRM files";

    /// <summary>Gets the stable scheduled-task key.</summary>
    public string Key => "XtreamStrmDeduplication";

    /// <inheritdoc />
    public string Description => "Rebuilds selected Xtream STRM exports, retaining one preferred source per canonical title.";

    /// <inheritdoc />
    public string Category => "Xtream";

    /// <summary>Gets whether this task is hidden from the Jellyfin task list.</summary>
    public bool IsHidden => false;

    /// <summary>Gets whether this task can run.</summary>
    public bool IsEnabled => true;

    /// <summary>Gets whether Jellyfin records this task's execution.</summary>
    public bool IsLogged => true;

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        return strmExportService.DeduplicateAsync(progress, cancellationToken);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];
}
