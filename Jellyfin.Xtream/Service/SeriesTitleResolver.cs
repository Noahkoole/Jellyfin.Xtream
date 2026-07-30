// Copyright (C) 2022  Kevin Jilissen

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Jellyfin.Xtream.Client.Models;

namespace Jellyfin.Xtream.Service;

/// <summary>
/// Resolves a human-readable series title from the various, sometimes incomplete,
/// fields an Xtream provider returns for a series.
/// </summary>
internal static class SeriesTitleResolver
{
    // Matches a leading show name followed by a season/episode marker, e.g.
    // "9-1-1 (2018) - S01E01 - Pilot" captures "9-1-1 (2018)". A bounded match
    // timeout prevents a pathological title from stalling an export.
    private static readonly Regex _seasonEpisodeMarker = new(
        @"^\s*(?<name>.+?)(?:\s*[-–—:]\s*|\s+)S\d{1,3}\s*E\d{1,4}(?![0-9])",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    // Provider "names" that are really placeholders. When the series listing
    // (or detailed info) carries one of these, a better title is derived from
    // the episode titles instead.
    private static readonly HashSet<string> _placeholderTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Unknown",
        "N/A",
        "null",
        "undefined",
        "No name",
    };

    /// <summary>
    /// Resolves the best available raw (un-normalized) series title.
    /// </summary>
    /// <remarks>
    /// Priority: the series listing name, then the detailed series-info name, then a
    /// title derived from the common prefix of the episode titles. A listing/info name
    /// that is empty or a known placeholder (for example "Unknown") is skipped in favour
    /// of the derived title. The caller is responsible for applying name-cleanup and
    /// filesystem normalization to the result.
    /// </remarks>
    /// <param name="series">The series record from the series listing.</param>
    /// <param name="seriesInfo">The detailed series stream information.</param>
    /// <returns>
    /// A non-empty title when one can be determined; otherwise an empty string, which lets
    /// the path policy apply its own placeholder.
    /// </returns>
    public static string Resolve(Series series, SeriesStreamInfo seriesInfo)
    {
        ArgumentNullException.ThrowIfNull(series);
        ArgumentNullException.ThrowIfNull(seriesInfo);

        if (IsMeaningfulTitle(series.Name))
        {
            return series.Name.Trim();
        }

        if (IsMeaningfulTitle(seriesInfo.Info.Name))
        {
            return seriesInfo.Info.Name.Trim();
        }

        string? derived = DeriveFromEpisodeTitles(
            seriesInfo.Episodes.Values.SelectMany(episodes => episodes).Select(episode => episode.Title));
        if (derived is not null)
        {
            return derived;
        }

        // Nothing better is available: preserve whatever the provider sent so the
        // path policy can apply its own placeholder.
        return series.Name?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Determines whether a provider-supplied title is usable rather than empty or a placeholder.
    /// </summary>
    /// <param name="title">The provider title.</param>
    /// <returns><see langword="true"/> when the title is meaningful.</returns>
    internal static bool IsMeaningfulTitle(string? title)
    {
        return !string.IsNullOrWhiteSpace(title) && !_placeholderTitles.Contains(title.Trim());
    }

    /// <summary>
    /// Derives a series title from the shared prefix of episode titles that embed a
    /// season/episode marker (for example "Show Name - S01E01 - Episode Title").
    /// </summary>
    /// <param name="episodeTitles">The provider episode titles.</param>
    /// <returns>The most common derived title, or <see langword="null"/> when none can be derived.</returns>
    internal static string? DeriveFromEpisodeTitles(IEnumerable<string?> episodeTitles)
    {
        ArgumentNullException.ThrowIfNull(episodeTitles);

        // Count case-insensitively but keep the first-seen casing as the representative.
        Dictionary<string, (string Value, int Count)> candidates = new(StringComparer.OrdinalIgnoreCase);
        foreach (string? title in episodeTitles)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            string candidate;
            try
            {
                Match match = _seasonEpisodeMarker.Match(title);
                if (!match.Success)
                {
                    continue;
                }

                candidate = match.Groups["name"].Value.Trim().TrimEnd('-', '–', '—', ':', ' ').Trim();
            }
            catch (RegexMatchTimeoutException)
            {
                continue;
            }

            if (candidate.Length == 0)
            {
                continue;
            }

            if (candidates.TryGetValue(candidate, out (string Value, int Count) existing))
            {
                candidates[candidate] = (existing.Value, existing.Count + 1);
            }
            else
            {
                candidates[candidate] = (candidate, 1);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates.Values
            .OrderByDescending(entry => entry.Count)
            .ThenBy(entry => entry.Value, StringComparer.Ordinal)
            .First()
            .Value;
    }
}
