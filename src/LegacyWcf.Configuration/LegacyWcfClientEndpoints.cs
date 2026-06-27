using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents a typed enumerable collection of WCF client endpoints.
/// </summary>
/// <remarks>
/// Phase 3 adds targeted lookup helpers on top of the enumerable client endpoint collection.
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
    /// Finds the first client endpoint with the specified endpoint name.
    /// </summary>
    /// <param name="name">The endpoint name to find.</param>
    /// <returns>The first matching endpoint, or <see langword="null"/> when no endpoint matches.</returns>
    public LegacyWcfClientEndpoint? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _endpoints.FirstOrDefault(
            endpoint => string.Equals(endpoint.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the first client endpoint with the specified endpoint name.
    /// </summary>
    /// <param name="name">The endpoint name to retrieve.</param>
    /// <returns>The first matching endpoint.</returns>
    /// <exception cref="InvalidOperationException">No matching client endpoint exists.</exception>
    public LegacyWcfClientEndpoint GetRequiredByName(string name)
    {
        var endpoint = FindByName(name);

        if (endpoint is null)
        {
            throw new InvalidOperationException(
                "A WCF client endpoint named '" + FormatLookupValue(name) + "' was not found.");
        }

        return endpoint;
    }

    /// <summary>
    /// Finds the first client endpoint with the specified contract.
    /// </summary>
    /// <param name="contract">The endpoint contract to find.</param>
    /// <returns>The first matching endpoint, or <see langword="null"/> when no endpoint matches.</returns>
    public LegacyWcfClientEndpoint? FindByContract(string contract)
    {
        if (string.IsNullOrWhiteSpace(contract))
        {
            return null;
        }

        return _endpoints.FirstOrDefault(
            endpoint => string.Equals(endpoint.Contract, contract, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the first client endpoint with the specified contract.
    /// </summary>
    /// <param name="contract">The endpoint contract to retrieve.</param>
    /// <returns>The first matching endpoint.</returns>
    /// <exception cref="InvalidOperationException">No matching client endpoint exists.</exception>
    public LegacyWcfClientEndpoint GetRequiredByContract(string contract)
    {
        var endpoint = FindByContract(contract);

        if (endpoint is null)
        {
            throw new InvalidOperationException(
                "A WCF client endpoint with contract '" + FormatLookupValue(contract) + "' was not found.");
        }

        return endpoint;
    }

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

    private static string FormatLookupValue(string? value)
    {
        return value ?? "<null>";
    }
}
