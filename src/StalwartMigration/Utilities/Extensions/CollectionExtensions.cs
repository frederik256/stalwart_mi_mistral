// <copyright file="CollectionExtensions.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

namespace StalwartMigration.Utilities.Extensions;

/// <summary>
/// Extension methods for collections and enumerables.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Checks if a collection is null or empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <returns>True if the collection is null or empty; otherwise, false.</returns>
    public static bool IsNullOrEmpty<T>(this ICollection<T>? collection)
    {
        return collection == null || collection.Count == 0;
    }

    /// <summary>
    /// Checks if an enumerable is null or empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="enumerable">The enumerable to check.</param>
    /// <returns>True if the enumerable is null or empty; otherwise, false.</returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? enumerable)
    {
        return enumerable == null || !enumerable.Any();
    }

    /// <summary>
    /// Adds a range of items to a collection.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to add to.</param>
    /// <param name="items">The items to add.</param>
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        if (items == null)
        {
            return;
        }

        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    /// <summary>
    /// Splits a collection into batches of a specified size.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="batchSize">The size of each batch.</param>
    /// <returns>An enumerable of batches.</returns>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (batchSize <= 0)
        {
            throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));
        }

        using var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var batch = new List<T> { enumerator.Current };

            for (int i = 1; i < batchSize && enumerator.MoveNext(); i++)
            {
                batch.Add(enumerator.Current);
            }

            yield return batch;
        }
    }

    /// <summary>
    /// Splits a collection into batches of a specified size asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="batchSize">The size of each batch.</param>
    /// <returns>An async enumerable of batches.</returns>
    public static async IAsyncEnumerable<IEnumerable<T>> BatchAsync<T>(
        this IAsyncEnumerable<T> source,
        int batchSize)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (batchSize <= 0)
        {
            throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));
        }

        await using var enumerator = source.GetAsyncEnumerator();
        while (await enumerator.MoveNextAsync())
        {
            var batch = new List<T> { enumerator.Current };

            for (int i = 1; i < batchSize && await enumerator.MoveNextAsync(); i++)
            {
                batch.Add(enumerator.Current);
            }

            yield return batch;
        }
    }

    /// <summary>
    /// ForEach extension method for IEnumerable.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="action">The action to perform on each element.</param>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        foreach (var item in source)
        {
            action(item);
        }
    }

    /// <summary>
    /// ForEach extension method for IEnumerable with index.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="action">The action to perform on each element with its index.</param>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        int index = 0;
        foreach (var item in source)
        {
            action(item, index);
            index++;
        }
    }

    /// <summary>
    /// Converts an enumerable to a hash set.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <returns>A HashSet containing the elements.</returns>
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return new HashSet<T>(source);
    }

    /// <summary>
    /// Converts an enumerable to a hash set with a custom comparer.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="comparer">The equality comparer to use.</param>
    /// <returns>A HashSet containing the elements.</returns>
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return new HashSet<T>(source, comparer);
    }

    /// <summary>
    /// Gets the first element of a sequence, or null if the sequence is empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>The first element, or null if empty.</returns>
    public static T? FirstOrNull<T>(this IEnumerable<T> source) where T : class
    {
        return source.FirstOrDefault();
    }

    /// <summary>
    /// Distinct by a key selector.
    /// </summary>
    /// <typeparam name="TSource">The type of source elements.</typeparam>
    /// <typeparam name="TKey">The type of key.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="keySelector">The key selector function.</param>
    /// <returns>An enumerable of distinct elements based on the key.</returns>
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (keySelector == null)
        {
            throw new ArgumentNullException(nameof(keySelector));
        }

        var seenKeys = new HashSet<TKey>();
        foreach (var item in source)
        {
            var key = keySelector(item);
            if (seenKeys.Add(key))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Gets elements that are not null.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <returns>An enumerable of non-null elements.</returns>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return source.Where(x => x != null)!;
    }
}
