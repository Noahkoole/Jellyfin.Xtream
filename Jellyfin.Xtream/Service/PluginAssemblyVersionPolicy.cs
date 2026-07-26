// Copyright (C) 2022  Kevin Jilissen

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Jellyfin.Xtream.Service;

/// <summary>
/// Selects one assembly when Jellyfin has loaded more than one plugin version.
/// </summary>
internal static class PluginAssemblyVersionPolicy
{
    /// <summary>
    /// Gets a value indicating whether this assembly is the preferred loaded version.
    /// </summary>
    /// <returns><see langword="true"/> when no newer matching plugin assembly is loaded.</returns>
    public static bool IsCurrentAssemblyPreferred()
    {
        Assembly current = typeof(PluginAssemblyVersionPolicy).Assembly;
        return IsPreferred(current, AppDomain.CurrentDomain.GetAssemblies());
    }

    internal static bool IsPreferred(Assembly current, IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(assemblies);

        string? name = current.GetName().Name;
        if (string.IsNullOrEmpty(name))
        {
            return true;
        }

        Assembly preferred = assemblies
            .Where(assembly => !assembly.IsDynamic
                && string.Equals(assembly.GetName().Name, name, StringComparison.Ordinal))
            .OrderByDescending(assembly => assembly.GetName().Version ?? new Version(0, 0))
            .ThenBy(GetLocation, StringComparer.Ordinal)
            .FirstOrDefault() ?? current;
        return ReferenceEquals(current, preferred);
    }

    private static string GetLocation(Assembly assembly)
    {
        try
        {
            return assembly.Location;
        }
        catch (NotSupportedException)
        {
            return string.Empty;
        }
    }
}
