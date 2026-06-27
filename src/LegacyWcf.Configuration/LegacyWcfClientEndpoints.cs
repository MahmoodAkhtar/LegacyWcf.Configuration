using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents a typed enumerable collection of WCF client endpoints.
/// </summary>
/// <remarks>
/// Phase 2 Stage 5 intentionally provides enumeration, count, indexer, and LINQ support only.
/// Targeted lookup helpers are planned for a later retrieval API phase.
/// </remarks>
public sealed class LegacyWcfClientEndpoints : IReadOnlyList<LegacyWcfClientEndpoint>
{
    private readonly IReadOnlyList<LegacyWcfClientEndpoint> _endpoints;

    /// <summary>
    /// Gets an empty client endpoint collection.
    /// </summary>
    public static LegacyWcfClientEndpoints Empty { get; } = new LegacyWcfClientEndpoints(Array.Empty<LegacyWcfClientEndpoint>());

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfClientEndpoints"/> class.
    /// </summary>
    /// <param name="endpoints">The endpoints to expose through the collection.</param>
    public LegacyWcfClientEndpoints(IEnumerable<LegacyWcfClientEndpoint>? endpoints)
    {
        _endpoints = endpoints?.ToList() ?? new List<LegacyWcfClientEndpoint>();
    }

    /// <summary>
    /// Gets the number of endpoints in the collection.
    /// </summary>
    public int Count => _endpoints.Count;

    /// <summary>
    /// Gets the endpoint at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based endpoint index.</param>
    /// <returns>The endpoint at the specified index.</returns>
    public LegacyWcfClientEndpoint this[int index] => _endpoints[index];

    /// <summary>
    /// Returns an enumerator that iterates through the endpoint collection.
    /// </summary>
    /// <returns>An endpoint enumerator.</returns>
    public IEnumerator<LegacyWcfClientEndpoint> GetEnumerator()
    {
        return _endpoints.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the endpoint collection.
    /// </summary>
    /// <returns>An endpoint enumerator.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
