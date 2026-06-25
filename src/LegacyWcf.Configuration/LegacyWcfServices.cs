using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents a typed enumerable collection of WCF services.
/// </summary>
/// <remarks>
/// Phase 2 Stage 1 intentionally provides enumeration, count, indexer, and LINQ support only.
/// Targeted lookup helpers are planned for a later retrieval API phase.
/// </remarks>
public sealed class LegacyWcfServices : IReadOnlyList<LegacyWcfService>
{
    private readonly IReadOnlyList<LegacyWcfService> _services;

    /// <summary>
    /// Gets an empty service collection.
    /// </summary>
    public static LegacyWcfServices Empty { get; } = new LegacyWcfServices(Array.Empty<LegacyWcfService>());

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfServices"/> class.
    /// </summary>
    /// <param name="services">The services to expose through the collection.</param>
    public LegacyWcfServices(IEnumerable<LegacyWcfService>? services)
    {
        _services = services?.ToList() ?? new List<LegacyWcfService>();
    }

    /// <summary>
    /// Gets the number of services in the collection.
    /// </summary>
    public int Count => _services.Count;

    /// <summary>
    /// Gets the service at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based service index.</param>
    /// <returns>The service at the specified index.</returns>
    public LegacyWcfService this[int index] => _services[index];

    /// <summary>
    /// Returns an enumerator that iterates through the service collection.
    /// </summary>
    /// <returns>A service enumerator.</returns>
    public IEnumerator<LegacyWcfService> GetEnumerator()
    {
        return _services.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the service collection.
    /// </summary>
    /// <returns>A service enumerator.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
