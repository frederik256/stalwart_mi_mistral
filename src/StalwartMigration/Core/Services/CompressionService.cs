// <copyright file="CompressionService.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace StalwartMigration.Core.Services;

/// <summary>
/// Service for handling compression and decompression operations.
/// </summary>
public class CompressionService : ICompressionService
{
    private readonly ILogger<CompressionService> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the CompressionService class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public CompressionService(ILogger<CompressionService>? logger = null)
    {
        _logger = logger ?? NullLogger<CompressionService>.Instance;
    }

    /// <summary>
    /// Compresses the given data using GZip compression.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The compressed data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    public async Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (data.Length == 0)
            return Array.Empty<byte>();

        _logger.LogDebug("Compressing {ByteCount} bytes", data.Length);

        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal, true))
        {
            await gzipStream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
        }

        // GZip appends a checksum/trailer, so we need to reset the position
        outputStream.Position = 0;
        var compressed = outputStream.ToArray();

        _logger.LogDebug("Compressed from {OriginalSize} to {CompressedSize} bytes", data.Length, compressed.Length);

        return compressed;
    }

    /// <summary>
    /// Decompresses the given GZip compressed data.
    /// </summary>
    /// <param name="compressedData">The compressed data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The decompressed data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when compressedData is null.</exception>
    public async Task<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default)
    {
        if (compressedData == null)
            throw new ArgumentNullException(nameof(compressedData));

        if (compressedData.Length == 0)
            return Array.Empty<byte>();

        _logger.LogDebug("Decompressing {ByteCount} bytes", compressedData.Length);

        using var inputStream = new MemoryStream(compressedData);
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress, true))
        {
            await gzipStream.CopyToAsync(outputStream, 81920, cancellationToken).ConfigureAwait(false);
        }

        outputStream.Position = 0;
        var decompressed = outputStream.ToArray();

        _logger.LogDebug("Decompressed from {CompressedSize} to {OriginalSize} bytes", compressedData.Length, decompressed.Length);

        return decompressed;
    }

    /// <summary>
    /// Disposes the service.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
