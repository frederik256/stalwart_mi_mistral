// <copyright file="HMailServerException.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Runtime.Serialization;

namespace StalwartMigration.Infrastructure.HMailServer;

/// <summary>
/// Exception thrown when there is an error communicating with hMailServer via COM API or database.
/// </summary>
[Serializable]
public class HMailServerException : Exception
{
    public HMailServerException() { }
    public HMailServerException(string message) : base(message) { }
    public HMailServerException(string message, Exception innerException) : base(message, innerException) { }
    protected HMailServerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    public string? HMailServerVersion { get; set; }
    public int? ComErrorCode { get; set; }
    public int? HMailServerErrorCode { get; set; }
    public string? FailedOperation { get; set; }
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public virtual string? Remediation { get; set; }
    public override string ToString()
    {
        var baseString = base.ToString();
        if (FailedOperation != null) baseString += $"\nOperation: {FailedOperation}";
        if (ResourceType != null && ResourceId != null) baseString += $"\nResource: {ResourceType}/{ResourceId}";
        if (HMailServerErrorCode.HasValue) baseString += $"\nhMailServer Error Code: {HMailServerErrorCode.Value}";
        if (ComErrorCode.HasValue) baseString += $"\nCOM Error Code: 0x{ComErrorCode.Value:X8}";
        if (HMailServerVersion != null) baseString += $"\nhMailServer Version: {HMailServerVersion}";
        if (Remediation != null) baseString += $"\nRemediation: {Remediation}";
        return baseString;
    }
    public static HMailServerException ForConnectionError(string message, Exception innerException) =>
        new(message, innerException) { FailedOperation = "Connection", HMailServerErrorCode = -1, Remediation = "Check that hMailServer is running and accessible." };
    public static HMailServerException ForComInitializationError(string message, int comErrorCode) =>
        new(message) { FailedOperation = "COM Initialization", ComErrorCode = comErrorCode, HMailServerErrorCode = -1, Remediation = "Ensure hMailServer COM components are properly registered." };
    public static HMailServerException ForNotFound(string resourceType, string resourceId) =>
        new($"{resourceType} with identifier '{resourceId}' was not found in hMailServer.") { FailedOperation = "Lookup", ResourceType = resourceType, ResourceId = resourceId, HMailServerErrorCode = -2, Remediation = "Verify the resource exists in hMailServer." };
    public static HMailServerException ForAuthenticationError(string message = "Authentication to hMailServer failed.") =>
        new(message) { FailedOperation = "Authentication", HMailServerErrorCode = -3, Remediation = "Verify the hMailServer administrator password is correct." };
    public static HMailServerException ForDatabaseError(string message, Exception innerException) =>
        new(message, innerException) { FailedOperation = "Database Access", HMailServerErrorCode = -4, Remediation = "Check database connectivity. Consider using COM API instead." };
    public static HMailServerException ForUnsupportedVersion(string detectedVersion, string requiredVersion) =>
        new($"hMailServer v{detectedVersion} is not supported. Required version: {requiredVersion}.")
        {
            FailedOperation = "Version Check",
            HMailServerErrorCode = -5,
            HMailServerVersion = detectedVersion,
            Remediation = "Upgrade or downgrade hMailServer to a supported version."
        };
}