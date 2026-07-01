// <copyright file="PathSanitizer.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.IO;

namespace StalwartMigration.Utilities.Helpers;

/// <summary>
/// Provides path sanitization to prevent directory traversal attacks.
/// </summary>
public static class PathSanitizer
{
    /// <summary>
    /// Characters that are not allowed in file names.
    /// </summary>
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    /// <summary>
    /// Characters that are not allowed in path names.
    /// </summary>
    private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();

    /// <summary>
    /// Sanitizes a file name by removing invalid characters.
    /// </summary>
    /// <param name="fileName">The file name to sanitize.</param>
    /// <param name="replacement">The character to use as replacement. Defaults to underscore.</param>
    /// <returns>A sanitized file name.</returns>
    public static string SanitizeFileName(string fileName, char replacement = '_')
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return string.Empty;
        }

        // Replace invalid characters
        foreach (char invalidChar in InvalidFileNameChars)
        {
            fileName = fileName.Replace(invalidChar.ToString(), replacement.ToString());
        }

        // Remove leading and trailing whitespace and replacement characters
        fileName = fileName.Trim(replacement, ' ');

        // If the file name becomes empty after sanitization, use a default
        if (string.IsNullOrEmpty(fileName))
        {
            return "unnamed";
        }

        return fileName;
    }

    /// <summary>
    /// Sanitizes a path by removing invalid characters and preventing directory traversal.
    /// </summary>
    /// <param name="path">The path to sanitize.</param>
    /// <param name="replacement">The character to use as replacement. Defaults to underscore.</param>
    /// <returns>A sanitized path.</returns>
    public static string SanitizePath(string path, char replacement = '_')
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        // Replace invalid characters
        foreach (char invalidChar in InvalidPathChars)
        {
            path = path.Replace(invalidChar.ToString(), replacement.ToString());
        }

        // Prevent directory traversal
        path = PreventDirectoryTraversal(path);

        // Normalize path separators
        path = path.Replace('/', Path.DirectorySeparatorChar)
                   .Replace('\\', Path.DirectorySeparatorChar);

        // Collapse multiple consecutive separators
        while (path.Contains(Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar.ToString()))
        {
            path = path.Replace(Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar.ToString(),
                              Path.DirectorySeparatorChar.ToString());
        }

        // Remove leading and trailing separators
        path = path.Trim(Path.DirectorySeparatorChar);

        // If the path becomes empty after sanitization, use a default
        if (string.IsNullOrEmpty(path))
        {
            return ".";
        }

        return path;
    }

    /// <summary>
    /// Prevents directory traversal by removing parent directory references.
    /// </summary>
    /// <param name="path">The path to sanitize.</param>
    /// <returns>A path without parent directory references.</returns>
    private static string PreventDirectoryTraversal(string path)
    {
        // Remove all occurrences of ".." (parent directory)
        string result = path;
        string parentDir = ".." + Path.DirectorySeparatorChar;
        string parentDirAlt = ".." + Path.AltDirectorySeparatorChar;

        while (result.Contains(parentDir) || result.Contains(parentDirAlt))
        {
            result = result.Replace(parentDir, string.Empty);
            result = result.Replace(parentDirAlt, string.Empty);
        }

        // Also handle ".." at the end
        while (result.EndsWith(".."))
        {
            result = result.Substring(0, result.Length - 2);
        }

        // Remove leading separators that might allow absolute paths
        while (result.StartsWith(Path.DirectorySeparatorChar.ToString()) ||
               result.StartsWith(Path.AltDirectorySeparatorChar.ToString()))
        {
            result = result.Substring(1);
        }

        return result;
    }

    /// <summary>
    /// Validates that a path does not contain directory traversal attempts.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is safe; otherwise, false.</returns>
    public static bool IsSafePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        // Check for parent directory references
        if (path.Contains(".."))
        {
            return false;
        }

        // Check for absolute paths on Unix
        if (path.StartsWith("/"))
        {
            return false;
        }

        // Check for absolute paths on Windows
        if (path.StartsWith("\\") ||
            (path.Length >= 2 && path[1] == ':' && (path[2] == '\\' || path[2] == '/')))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates a path and throws an exception if it's unsafe.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <exception cref="ArgumentException">Thrown when the path is unsafe.</exception>
    public static void ValidatePath(string? path, string paramName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or whitespace.", paramName);
        }

        if (!IsSafePath(path))
        {
            throw new ArgumentException("Path contains unsafe characters or directory traversal attempts.", paramName);
        }
    }

    /// <summary>
    /// Creates a safe file name from any string.
    /// </summary>
    /// <param name="input">The input string to convert to a safe file name.</param>
    /// <param name="maxLength">The maximum length of the file name. Defaults to 255.</param>
    /// <param name="replacement">The character to use as replacement. Defaults to underscore.</param>
    /// <returns>A safe file name.</returns>
    public static string CreateSafeFileName(string input, int maxLength = 255, char replacement = '_')
    {
        if (string.IsNullOrEmpty(input))
        {
            return "unnamed";
        }

        // Sanitize the file name
        string result = SanitizeFileName(input, replacement);

        // Truncate if too long
        if (result.Length > maxLength)
        {
            // Try to preserve the extension if possible
            string extension = Path.GetExtension(result);
            if (!string.IsNullOrEmpty(extension))
            {
                int extensionLength = extension.Length;
                int nameLength = maxLength - extensionLength;
                if (nameLength > 0)
                {
                    result = result.Substring(0, nameLength) + extension;
                }
                else
                {
                    result = result.Substring(0, maxLength);
                }
            }
            else
            {
                result = result.Substring(0, maxLength);
            }
        }

        return result;
    }
}
