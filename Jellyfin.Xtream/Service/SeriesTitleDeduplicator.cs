// Copyright (C) 2022  Kevin Jilissen

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Xtream.Client.Models;

namespace Jellyfin.Xtream.Service;

/// <summary>
/// Selects one stable provider record for each normalized series title.
/// </summary>
internal static class SeriesTitleDeduplicator
{
    /// <summary>
    /// Removes duplicate non-empty normalized series titles, preferring the lowest provider ID.
    /// </summary>
    /// <param name="series">Provider series records to consider.</param>
    /// <param name="names">The naming snapshot used for the current operation.</param>
    /// <returns>The deterministic preferred record for every distinct title.</returns>
    public static List<Series> Deduplicate(
        IEnumerable<Series> series,
        NameNormalizationSnapshot names)
    {
        ArgumentNullException.ThrowIfNull(series);
        ArgumentNullException.ThrowIfNull(names);

        return series
            .GroupBy(item => GetKey(item, names), StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderBy(item => item.SeriesId)
                .ThenBy(item => item.Name, StringComparer.Ordinal)
                .First())
            .OrderBy(item => names.Normalize(item.Name, NameScope.Series).Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.SeriesId)
            .ToList();
    }

    private static string GetKey(Series series, NameNormalizationSnapshot names)
    {
        string title = names.Normalize(series.Name, NameScope.Series).Title;
        string key = MediaTitleKey.Create(title);
        return string.IsNullOrWhiteSpace(key)
            ? $"\u0000{series.SeriesId}"
            : key;
    }
}
