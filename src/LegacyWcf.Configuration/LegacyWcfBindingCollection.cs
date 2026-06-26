using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents a typed enumerable collection of WCF binding configurations.
/// </summary>
/// <remarks>
/// Phase 2 Stage 3 intentionally provides enumeration, count, indexer, and LINQ support only.
/// Targeted lookup helpers are planned for a later retrieval API phase.
/// </remarks>
public sealed class LegacyWcfBindingCollection : IReadOnlyList<LegacyWcfBinding>
{
    private readonly IReadOnlyList<LegacyWcfBinding> _bindings;

    /// <summary>
    /// Gets an empty binding collection.
    /// </summary>
    public static LegacyWcfBindingCollection Empty { get; } = new LegacyWcfBindingCollection(Array.Empty<LegacyWcfBinding>());

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfBindingCollection"/> class.
    /// </summary>
    /// <param name="bindings">The bindings to expose through the collection.</param>
    public LegacyWcfBindingCollection(IEnumerable<LegacyWcfBinding>? bindings)
    {
        _bindings = bindings?.ToList() ?? new List<LegacyWcfBinding>();
    }

    /// <summary>
    /// Gets the number of bindings in the collection.
    /// </summary>
    public int Count => _bindings.Count;

    /// <summary>
    /// Gets the binding at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based binding index.</param>
    /// <returns>The binding at the specified index.</returns>
    public LegacyWcfBinding this[int index] => _bindings[index];

    /// <summary>
    /// Returns an enumerator that iterates through the binding collection.
    /// </summary>
    /// <returns>A binding enumerator.</returns>
    public IEnumerator<LegacyWcfBinding> GetEnumerator()
    {
        return _bindings.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the binding collection.
    /// </summary>
    /// <returns>A binding enumerator.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
