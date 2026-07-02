// <copyright file="ICompressionService.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

namespace StalwartMigration.Core.Services;

/// <summary>
/// Interface for compression/decompression services.
/// </summary>
public interface ICompressionService : IDisposable
{
    /// <summary>
    /// Compresses the given data.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The compressed data.</returns>
    Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decompresses the given data.
    /// </summary>
    /// <param name="compressedData">The compressed data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The decompressed data.</returns>
    Task<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default);
}
