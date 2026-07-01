// <copyright file="FileSystemExtensions.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.IO;

namespace StalwartMigration.Utilities.Extensions;

/// <summary>
/// Extension methods for file system operations.
/// </summary>
public static class FileSystemExtensions
{
    /// <summary>
    /// Combines path segments with proper path separator.
    /// </summary>
    /// <param name="path">The base path.</param>
    /// <param name="parts">Additional path parts to combine.</param>
    /// <returns>The combined path.</returns>
    public static string CombinePath(this string path, params string[] parts)
    {
        if (path.IsNullOrEmpty())
        {
            return Path.Combine(parts);
        }

        var allParts = new[] { path }.Concat(parts.Where(p => !p.IsNullOrEmpty()));
        return Path.Combine(allParts.ToArray());
    }

    /// <summary>
    /// Gets the directory name from a file path, ensuring it's not null.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The directory name, or empty string if none.</returns>
    public static string GetDirectoryName(this string filePath)
    {
        if (filePath.IsNullOrEmpty())
        {
            return string.Empty;
        }

        return Path.GetDirectoryName(filePath) ?? string.Empty;
    }

    /// <summary>
    /// Gets the file name from a file path, ensuring it's not null.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The file name, or empty string if none.</returns>
    public static string GetFileName(this string filePath)
    {
        if (filePath.IsNullOrEmpty())
        {
            return string.Empty;
        }

        return Path.GetFileName(filePath) ?? string.Empty;
    }

    /// <summary>
    /// Gets the file name without extension.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The file name without extension.</returns>
    public static string GetFileNameWithoutExtension(this string filePath)
    {
        if (filePath.IsNullOrEmpty())
        {
            return string.Empty;
        }

        return Path.GetFileNameWithoutExtension(filePath) ?? string.Empty;
    }

    /// <summary>
    /// Gets the file extension.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The file extension, or empty string if none.</returns>
    public static string GetExtension(this string filePath)
    {
        if (filePath.IsNullOrEmpty())
        {
            return string.Empty;
        }

        return Path.GetExtension(filePath) ?? string.Empty;
    }

    /// <summary>
    /// Checks if a path is a directory path (ends with directory separator).
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is a directory path; otherwise, false.</returns>
    public static bool IsDirectoryPath(this string path)
    {
        if (path.IsNullOrEmpty())
        {
            return false;
        }

        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// Ensures a directory path ends with a directory separator.
    /// </summary>
    /// <param name="path">The path to ensure.</param>
    /// <returns>The path with directory separator at the end.</returns>
    public static string EnsureTrailingSeparator(this string path)
    {
        if (path.IsNullOrEmpty())
        {
            return path;
        }

        if (!path.IsDirectoryPath())
        {
            return path + Path.DirectorySeparatorChar;
        }

        return path;
    }

    /// <summary>
    /// Removes trailing directory separators from a path.
    /// </summary>
    /// <param name="path">The path to clean.</param>
    /// <returns>The path without trailing directory separators.</returns>
    public static string RemoveTrailingSeparator(this string path)
    {
        if (path.IsNullOrEmpty())
        {
            return path;
        }

        while (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
        {
            path = path.Substring(0, path.Length - 1);
        }

        return path;
    }

    /// <summary>
    /// Sanitizes a path to prevent directory traversal attacks.
    /// </summary>
    /// <param name="path">The path to sanitize.</param>
    /// <returns>A sanitized path, or empty string if the path is invalid.</returns>
    public static string SanitizePath(this string path)
    {
        if (path.IsNullOrEmpty())
        {
            return string.Empty;
        }

        // Normalize path separators
        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        // Remove any attempt to traverse up
        while (normalized.Contains(".." + Path.DirectorySeparatorChar))
        {
            normalized = normalized.Replace(".." + Path.DirectorySeparatorChar, string.Empty);
        }

        // Remove leading directory separators
        while (normalized.StartsWith(Path.DirectorySeparatorChar.ToString()))
        {
            normalized = normalized.Substring(1);
        }

        return normalized;
    }

    /// <summary>
    /// Checks if a path is relative.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is relative; otherwise, false.</returns>
    public static bool IsRelativePath(this string path)
    {
        if (path.IsNullOrEmpty())
        {
            return false;
        }

        return !Path.IsPathRooted(path);
    }

    /// <summary>
    /// Gets a relative path from a base directory to a target path.
    /// </summary>
    /// <param name="baseDirectory">The base directory.</param>
    /// <param name="targetPath">The target path.</param>
    /// <returns>The relative path from base to target.</returns>
    public static string GetRelativePath(this string baseDirectory, string targetPath)
    {
        if (baseDirectory.IsNullOrEmpty() || targetPath.IsNullOrEmpty())
        {
            return targetPath ?? string.Empty;
        }

        var baseUri = new Uri(baseDirectory.EnsureTrailingSeparator());
        var targetUri = new Uri(targetPath);

        var relativeUri = baseUri.MakeRelativeUri(targetUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        // Convert forward slashes to platform-specific separators
        if (Path.DirectorySeparatorChar != '/')
        {
            relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        return relativePath;
    }
}
