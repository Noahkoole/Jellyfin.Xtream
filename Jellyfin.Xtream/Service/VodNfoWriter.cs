// Copyright (C) 2022  Kevin Jilissen

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Jellyfin.Xtream.Client.Models;

namespace Jellyfin.Xtream.Service;

/// <summary>
/// Writes minimal local movie metadata so Jellyfin does not infer an identity from an ambiguous filename.
/// </summary>
internal static class VodNfoWriter
{
    /// <summary>
    /// Builds a movie NFO using only provider-supplied values.
    /// </summary>
    /// <param name="title">The cleaned provider title.</param>
    /// <param name="detail">The optional provider detail response.</param>
    /// <returns>A complete XML NFO document.</returns>
    public static string Create(string title, VodInfo? detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        StringBuilder buffer = new();
        XmlWriterSettings settings = new()
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            OmitXmlDeclaration = false,
        };
        using (XmlWriter writer = XmlWriter.Create(buffer, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("movie");
            writer.WriteElementString("title", title);
            if (detail?.ReleaseDate is DateTime releaseDate)
            {
                writer.WriteElementString("year", releaseDate.Year.ToString(CultureInfo.InvariantCulture));
            }

            if (detail?.TmdbId is int tmdbId && tmdbId > 0)
            {
                writer.WriteStartElement("uniqueid");
                writer.WriteAttributeString("type", "tmdb");
                writer.WriteAttributeString("default", "true");
                writer.WriteString(tmdbId.ToString(CultureInfo.InvariantCulture));
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return buffer.ToString();
    }

    /// <summary>
    /// Converts a STRM relative path into its sibling NFO path.
    /// </summary>
    /// <param name="strmRelativePath">A generated STRM relative path.</param>
    /// <returns>The sibling NFO relative path.</returns>
    public static string GetRelativePath(string strmRelativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strmRelativePath);
        return Path.ChangeExtension(strmRelativePath, ".nfo");
    }
}
