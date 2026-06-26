using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents a typed enumerable collection of WCF behaviour configurations.
/// </summary>
/// <remarks>
/// Phase 2 Stage 4 intentionally provides enumeration, count, indexer, and LINQ support only.
/// Targeted lookup helpers are planned for a later retrieval API phase.
/// </remarks>
public sealed class LegacyWcfBehaviorCollection : IReadOnlyList<LegacyWcfBehavior>
{
    private readonly IReadOnlyList<LegacyWcfBehavior> _behaviors;

    /// <summary>
    /// Gets an empty behaviour collection.
    /// </summary>
    public static LegacyWcfBehaviorCollection Empty { get; } = new LegacyWcfBehaviorCollection(Array.Empty<LegacyWcfBehavior>());

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfBehaviorCollection"/> class.
    /// </summary>
    /// <param name="behaviors">The behaviours to expose through the collection.</param>
    public LegacyWcfBehaviorCollection(IEnumerable<LegacyWcfBehavior>? behaviors)
    {
        _behaviors = behaviors?.ToList() ?? new List<LegacyWcfBehavior>();
    }

    /// <summary>
    /// Gets the number of behaviours in the collection.
    /// </summary>
    public int Count => _behaviors.Count;

    /// <summary>
    /// Gets the behaviour at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based behaviour index.</param>
    /// <returns>The behaviour at the specified index.</returns>
    public LegacyWcfBehavior this[int index] => _behaviors[index];

    /// <summary>
    /// Returns an enumerator that iterates through the behaviour collection.
    /// </summary>
    /// <returns>A behaviour enumerator.</returns>
    public IEnumerator<LegacyWcfBehavior> GetEnumerator()
    {
        return _behaviors.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the behaviour collection.
    /// </summary>
    /// <returns>A behaviour enumerator.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
