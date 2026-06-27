using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents a typed enumerable collection of WCF services.
/// </summary>
/// <remarks>
/// Phase 3 adds targeted lookup helpers on top of the enumerable service collection.
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
    /// Finds the first service with the specified service name.
    /// </summary>
    /// <param name="name">The WCF service name to find.</param>
    /// <returns>The first matching service, or <see langword="null"/> when no service matches.</returns>
    /// <remarks>
    /// Matching is case-insensitive. Blank lookup values do not match. If duplicate service
    /// names exist, the first matching service is returned and duplicate diagnostics are left
    /// to later validation phases.
    /// </remarks>
    public LegacyWcfService? Find(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _services.FirstOrDefault(
            service => string.Equals(service.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the first service with the specified service name.
    /// </summary>
    /// <param name="name">The WCF service name to retrieve.</param>
    /// <returns>The first matching service.</returns>
    /// <exception cref="InvalidOperationException">No matching service exists.</exception>
    public LegacyWcfService GetRequired(string name)
    {
        var service = Find(name);

        if (service is null)
        {
            throw new InvalidOperationException(
                "A WCF service named '" + FormatLookupValue(name) + "' was not found.");
        }

        return service;
    }

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

    private static string FormatLookupValue(string? value)
    {
        return value ?? "<null>";
    }
}
