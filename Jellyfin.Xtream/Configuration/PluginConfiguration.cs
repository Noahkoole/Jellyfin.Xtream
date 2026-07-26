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
using MediaBrowser.Model.Plugins;

#pragma warning disable CA2227
namespace Jellyfin.Xtream.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the base url including protocol and trailing slash.
    /// </summary>
    public string BaseUrl { get; set; } = "https://example.com";

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user agent override.
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional public Jellyfin base URL used in proxy and STRM links.
    /// </summary>
    public string PublicServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets regex replacement rules applied to displayed stream names.
    /// </summary>
    public string NameCleanupRules { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets regex replacement rules applied to category names.
    /// </summary>
    public string CategoryNameCleanupRules { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets regex replacement rules applied to Live TV channel names.
    /// </summary>
    public string LiveTvNameCleanupRules { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets regex replacement rules applied to movie names.
    /// </summary>
    public string VodNameCleanupRules { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets regex replacement rules applied to series, season, and episode names.
    /// </summary>
    public string SeriesNameCleanupRules { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the Catch-up channel is visible.
    /// </summary>
    public bool IsCatchupVisible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Series channel is visible.
    /// </summary>
    public bool IsSeriesVisible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Video On-demand channel is visible.
    /// </summary>
    public bool IsVodVisible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Video On-demand channel is visible.
    /// </summary>
    public bool IsTmdbVodOverride { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether STRM export is enabled for VOD.
    /// </summary>
    public bool IsVodStrmExportEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the legacy VOD title-deduplication setting is enabled.
    /// This setting is ignored by v0.9 because provider IDs keep same-title items distinct.
    /// </summary>
    public bool IsVodStrmExportDeduplicationEnabled { get; set; }

    /// <summary>
    /// Gets or sets the folder where VOD STRM files are exported.
    /// </summary>
    public string VodStrmExportPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether STRM export is enabled for series.
    /// </summary>
    public bool IsSeriesStrmExportEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether duplicate normalized series titles are collapsed.
    /// The preferred provider record is used in the channel and STRM export.
    /// </summary>
    public bool IsSeriesStrmExportDeduplicationEnabled { get; set; }

    /// <summary>
    /// Gets or sets the folder where series STRM files are exported.
    /// </summary>
    public string SeriesStrmExportPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channels displayed in Live TV.
    /// </summary>
    public SerializableDictionary<int, HashSet<int>> LiveTv { get; set; } = [];

    /// <summary>
    /// Gets or sets the streams displayed in VOD.
    /// </summary>
    public SerializableDictionary<int, HashSet<int>> Vod { get; set; } = [];

    /// <summary>
    /// Gets or sets the streams displayed in Series.
    /// </summary>
    public SerializableDictionary<int, HashSet<int>> Series { get; set; } = [];

    /// <summary>
    /// Gets or sets the channel override configuration for Live TV.
    /// </summary>
    public SerializableDictionary<int, ChannelOverrides> LiveTvOverrides { get; set; } = [];

    /// <summary>
    /// Combines global rules with the automatically scoped editor rules.
    /// </summary>
    /// <returns>The complete line-oriented normalization configuration.</returns>
    public string GetEffectiveNameCleanupRules()
    {
        List<string> rules = [NameCleanupRules];
        AddScopedRules(rules, CategoryNameCleanupRules, "Category");
        AddScopedRules(rules, LiveTvNameCleanupRules, "LiveChannel");
        AddScopedRules(rules, VodNameCleanupRules, "Vod");
        AddScopedRules(rules, SeriesNameCleanupRules, "Series,Season,Episode");
        return string.Join(Environment.NewLine, rules);
    }

    private static void AddScopedRules(List<string> destination, string rules, string scopes)
    {
        foreach (string rawLine in rules.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length > 0 && line[0] != '#')
            {
                destination.Add($"[{scopes}] {line}");
            }
        }
    }
}
#pragma warning restore CA2227
