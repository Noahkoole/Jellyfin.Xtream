// Copyright (C) 2022  Kevin Jilissen

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Jellyfin.Xtream.Service;

/// <summary>
/// Produces a stable comparison key for provider titles.
/// </summary>
internal static partial class MediaTitleKey
{
    /// <summary>
    /// Removes provider-only suffixes and punctuation differences without removing a release year.
    /// </summary>
    /// <param name="title">The already-normalized provider title.</param>
    /// <returns>A case-insensitive canonical key suitable for title comparison.</returns>
    public static string Create(string? title)
    {
        string value = (title ?? string.Empty).Normalize(NormalizationForm.FormKD);
        value = TrailingProviderMarkersRegex().Replace(value, string.Empty);

        StringBuilder result = new(value.Length);
        foreach (char character in value)
        {
            if (char.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                result.Append(char.ToUpperInvariant(character));
            }
        }

        return result.ToString();
    }

    [GeneratedRegex(@"(?i)(?:\s*(?:\[[A-Z]{2,8}\]|\((?:[A-Z]{2,3}|NL|EN|SUBS|DUBBED|4K|UHD|FHD|HD)\)))+\s*$")]
    private static partial Regex TrailingProviderMarkersRegex();
}
