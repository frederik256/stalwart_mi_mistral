// <copyright file="VandelayResultParser.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace StalwartMigration.Infrastructure.Vandelay;

/// <summary>
/// Parses output from Vandelay operations to extract useful information.
/// </summary>
public class VandelayResultParser
{
    private readonly ILogger<VandelayResultParser> _logger;

    public VandelayResultParser(ILogger<VandelayResultParser>? logger = null)
    {
        _logger = logger ?? NullLogger<VandelayResultParser>.Instance;
    }

    public VandelayResult ParseResult(VandelayResult result)
    {
        if (result == null || string.IsNullOrWhiteSpace(result.StandardOutput))
            return result ?? new VandelayResult();

        try
        {
            ParseItemsProcessed(result);
            ParseVersion(result);
            ParseErrors(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing Vandelay output");
        }

        return result;
    }

    private void ParseItemsProcessed(VandelayResult result)
    {
        var output = result.StandardOutput ?? string.Empty;
        var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (trimmedLine.Contains("Messages processed", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.Contains("messages processed", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(ExtractNumber(trimmedLine), out int count))
                    result.ItemsProcessed = count;
            }
            else if (trimmedLine.Contains("Messages succeeded", StringComparison.OrdinalIgnoreCase) ||
                     trimmedLine.Contains("messages succeeded", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(ExtractNumber(trimmedLine), out int count))
                    result.ItemsSucceeded = count;
            }
            else if (trimmedLine.Contains("Messages failed", StringComparison.OrdinalIgnoreCase) ||
                     trimmedLine.Contains("messages failed", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(ExtractNumber(trimmedLine), out int count))
                    result.ItemsFailed = count;
            }
            else if (trimmedLine.Contains("Messages skipped", StringComparison.OrdinalIgnoreCase) ||
                     trimmedLine.Contains("messages skipped", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(ExtractNumber(trimmedLine), out int count))
                    result.ItemsSkipped = count;
            }
        }
    }

    private void ParseVersion(VandelayResult result)
    {
        var output = result.StandardOutput ?? string.Empty;
        var error = result.StandardError ?? string.Empty;
        var combined = output + "\n" + error;

        var lines = combined.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Look for version patterns like "v1.2.3" or "version 1.2.3"
            if (trimmedLine.StartsWith("v", StringComparison.OrdinalIgnoreCase) && 
                trimmedLine.Length > 1 && char.IsDigit(trimmedLine[1]))
            {
                result.VandelayVersion = trimmedLine;
                break;
            }
            else if (trimmedLine.Contains("version", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].Equals("version", StringComparison.OrdinalIgnoreCase) && 
                        i + 1 < parts.Length && IsVersionNumber(parts[i + 1]))
                    {
                        result.VandelayVersion = parts[i + 1];
                        break;
                    }
                }
                if (!string.IsNullOrWhiteSpace(result.VandelayVersion))
                    break;
            }
        }
    }

    private void ParseErrors(VandelayResult result)
    {
        var error = result.StandardError ?? string.Empty;
        var lines = error.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedLine) && 
                !trimmedLine.StartsWith("[", StringComparison.Ordinal) &&
                !trimmedLine.StartsWith("WARNING", StringComparison.OrdinalIgnoreCase))
            {
                result.ErrorMessage = trimmedLine;
                break;
            }
        }
    }

    private string ExtractNumber(string text)
    {
        var match = Regex.Match(text, @"\d+\.?\d*");
        return match.Success ? match.Value : string.Empty;
    }

    private bool IsVersionNumber(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var parts = text.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return false;

        foreach (var part in parts)
        {
            if (!int.TryParse(part, out _))
                return false;
        }

        return true;
    }

    public ProgressInfo? ParseProgress(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;

        var progress = new ProgressInfo();
        var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Look for progress indicators
            if (trimmedLine.Contains('%', StringComparison.Ordinal))
            {
                if (double.TryParse(ExtractNumber(trimmedLine), out double percent))
                    progress.Percentage = percent;
            }
            else if (trimmedLine.Contains("Processing", StringComparison.OrdinalIgnoreCase))
            {
                progress.CurrentItem = ExtractItemName(trimmedLine);
            }
            else if (trimmedLine.Contains("completed", StringComparison.OrdinalIgnoreCase) ||
                     trimmedLine.Contains("done", StringComparison.OrdinalIgnoreCase) ||
                     trimmedLine.Contains("finished", StringComparison.OrdinalIgnoreCase))
            {
                progress.IsComplete = true;
            }
        }

        return progress;
    }

    private string ExtractItemName(string text)
    {
        // Look for patterns like "Processing: Inbox" or "Processing Inbox"
        var match = Regex.Match(text, @"(?:Processing|Processing:|Processed|Processed:)\s*([^\s:]+)", 
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}

/// <summary>
/// Represents progress information from Vandelay operations.
/// </summary>
public class ProgressInfo
{
    /// <summary>
    /// Gets or sets the current progress percentage (0-100).
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Gets or sets the current item being processed.
    /// </summary>
    public string? CurrentItem { get; set; }

    /// <summary>
    /// Gets or sets whether the operation is complete.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Gets or sets the total number of items to process.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the number of items processed so far.
    /// </summary>
    public int ItemsProcessed { get; set; }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    public override string ToString()
    {
        if (IsComplete)
            return "Progress: 100% - Complete";
        else if (Percentage > 0)
            return $"Progress: {Percentage:F1}% - {CurrentItem ?? "Unknown"}";
        else
            return $"Progress: Processing {CurrentItem ?? "Unknown"}";
    }
}
