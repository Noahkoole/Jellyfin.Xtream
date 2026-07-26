// Copyright (C) 2022  Kevin Jilissen

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Xtream.Client.Models;

namespace Jellyfin.Xtream.Service;

/// <summary>
/// Selects one stable provider record for each normalized VOD title.
/// </summary>
internal static class VodTitleDeduplicator
{
    /// <summary>
    /// Removes duplicate non-empty normalized VOD titles, preferring 4K, then Dutch, then the lowest provider ID.
    /// </summary>
    /// <param name="streams">The provider VOD records to consider.</param>
    /// <param name="names">The naming snapshot used for the current operation.</param>
    /// <returns>The deterministic preferred record for every distinct title.</returns>
    public static List<StreamInfo> Deduplicate(
        IEnumerable<StreamInfo> streams,
        NameNormalizationSnapshot names)
    {
        ArgumentNullException.ThrowIfNull(streams);
        ArgumentNullException.ThrowIfNull(names);

        return streams
            .GroupBy(item => GetKey(item, names), StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderBy(item => GetPriority(item.Name).QualityRank)
                .ThenBy(item => GetPriority(item.Name).LanguageRank)
                .ThenBy(item => item.StreamId)
                .ThenBy(item => item.Name, StringComparer.Ordinal)
                .First())
            .OrderBy(item => names.Normalize(item.Name, NameScope.Vod).Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.StreamId)
            .ToList();
    }

    private static string GetKey(StreamInfo stream, NameNormalizationSnapshot names)
    {
        string title = names.Normalize(stream.Name, NameScope.Vod | NameScope.Filesystem).Title;
        return string.IsNullOrWhiteSpace(title) ? $"\u0000{stream.StreamId}" : title;
    }

    private static DuplicatePriority GetPriority(string name)
    {
        string prefix = name.Split(" - ", 2, StringSplitOptions.None)[0];
        string[] tokens = prefix.Split(['-', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        bool is4K = tokens.Any(token => string.Equals(token, "4K", StringComparison.OrdinalIgnoreCase));
        bool isNl = tokens.Any(token => string.Equals(token, "NL", StringComparison.OrdinalIgnoreCase));
        return new(is4K ? 0 : 1, isNl ? 0 : 1);
    }

    private readonly record struct DuplicatePriority(int QualityRank, int LanguageRank);
}
