// <copyright file="ArchiveManagerException.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Runtime.Serialization;

namespace StalwartMigration.Infrastructure.FileSystem;

#pragma warning disable SYSLIB0051 // Suppress Serializable exception constructor warning

/// <summary>
/// Exception thrown when there is an error with archive operations.
/// </summary>
[Serializable]
public class ArchiveManagerException : Exception
{
    public ArchiveManagerException() { }
    public ArchiveManagerException(string message) : base(message) { }
    public ArchiveManagerException(string message, Exception innerException) : base(message, innerException) { }
    protected ArchiveManagerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    public string? ArchivePath { get; set; }
    public string? FailedOperation { get; set; }
    public string? FileEntry { get; set; }
    public string? Remediation { get; set; }
    public override string ToString()
    {
        var baseString = base.ToString();
        if (ArchivePath != null) baseString += $"\nArchive: {ArchivePath}";
        if (FailedOperation != null) baseString += $"\nOperation: {FailedOperation}";
        if (FileEntry != null) baseString += $"\nFile: {FileEntry}";
        if (Remediation != null) baseString += $"\nRemediation: {Remediation}";
        return baseString;
    }
    public static ArchiveManagerException ForFileNotFound(string archivePath) =>
        new($"Archive not found: {archivePath}") { ArchivePath = archivePath, FailedOperation = "FileNotFound", Remediation = "Check the archive path exists and is accessible." };
    public static ArchiveManagerException ForFileExists(string archivePath) =>
        new($"Archive already exists: {archivePath}") { ArchivePath = archivePath, FailedOperation = "FileExists", Remediation = "Use overwrite option or specify a different path." };
    public static ArchiveManagerException ForInvalidArchive(string archivePath) =>
        new($"Invalid archive format: {archivePath}") { ArchivePath = archivePath, FailedOperation = "InvalidFormat", Remediation = "Ensure the file is a valid ZIP archive." };
    public static ArchiveManagerException ForFileInUse(string archivePath) =>
        new($"Archive is in use: {archivePath}") { ArchivePath = archivePath, FailedOperation = "FileInUse", Remediation = "Close any programs using the archive and try again." };
    public static ArchiveManagerException ForPermissionDenied(string archivePath) =>
        new($"Permission denied: {archivePath}") { ArchivePath = archivePath, FailedOperation = "PermissionDenied", Remediation = "Check file permissions and run with elevated privileges if needed." };
    public static ArchiveManagerException ForEntryNotFound(string archivePath, string entry) =>
        new($"Entry not found in archive: {entry}") { ArchivePath = archivePath, FileEntry = entry, FailedOperation = "EntryNotFound", Remediation = "Verify the entry name is correct and exists in the archive." };
}
