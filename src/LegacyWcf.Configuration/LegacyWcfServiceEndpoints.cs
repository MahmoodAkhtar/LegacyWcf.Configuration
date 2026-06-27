using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents a typed enumerable collection of WCF service endpoints.
/// </summary>
/// <remarks>
/// Phase 3 adds targeted lookup helpers on top of the enumerable service endpoint collection.
/// </remarks>
public sealed class LegacyWcfServiceEndpoints : IReadOnlyList<LegacyWcfServiceEndpoint>
{
    private readonly IReadOnlyList<LegacyWcfServiceEndpoint> _endpoints;

    /// <summary>
    /// Gets an empty service endpoint collection.
    /// </summary>
    public static LegacyWcfServiceEndpoints Empty { get; } = new LegacyWcfServiceEndpoints(Array.Empty<LegacyWcfServiceEndpoint>());

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfServiceEndpoints"/> class.
    /// </summary>
    /// <param name="endpoints">The endpoints to expose through the collection.</param>
    public LegacyWcfServiceEndpoints(IEnumerable<LegacyWcfServiceEndpoint>? endpoints)
    {
        _endpoints = endpoints?.ToList() ?? new List<LegacyWcfServiceEndpoint>();
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
    public LegacyWcfServiceEndpoint this[int index] => _endpoints[index];

    /// <summary>
    /// Finds the first service endpoint with the specified endpoint name.
    /// </summary>
    /// <param name="name">The endpoint name to find.</param>
    /// <returns>The first matching endpoint, or <see langword="null"/> when no endpoint matches.</returns>
    public LegacyWcfServiceEndpoint? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _endpoints.FirstOrDefault(
            endpoint => string.Equals(endpoint.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the first service endpoint with the specified endpoint name.
    /// </summary>
    /// <param name="name">The endpoint name to retrieve.</param>
    /// <returns>The first matching endpoint.</returns>
    /// <exception cref="InvalidOperationException">No matching service endpoint exists.</exception>
    public LegacyWcfServiceEndpoint GetRequiredByName(string name)
    {
        var endpoint = FindByName(name);

        if (endpoint is null)
        {
            throw new InvalidOperationException(
                "A WCF service endpoint named '" + FormatLookupValue(name) + "' was not found.");
        }

        return endpoint;
    }

    /// <summary>
    /// Finds the first service endpoint with the specified contract.
    /// </summary>
    /// <param name="contract">The endpoint contract to find.</param>
    /// <returns>The first matching endpoint, or <see langword="null"/> when no endpoint matches.</returns>
    public LegacyWcfServiceEndpoint? FindByContract(string contract)
    {
        if (string.IsNullOrWhiteSpace(contract))
        {
            return null;
        }

        return _endpoints.FirstOrDefault(
            endpoint => string.Equals(endpoint.Contract, contract, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the first service endpoint with the specified contract.
    /// </summary>
    /// <param name="contract">The endpoint contract to retrieve.</param>
    /// <returns>The first matching endpoint.</returns>
    /// <exception cref="InvalidOperationException">No matching service endpoint exists.</exception>
    public LegacyWcfServiceEndpoint GetRequiredByContract(string contract)
    {
        var endpoint = FindByContract(contract);

        if (endpoint is null)
        {
            throw new InvalidOperationException(
                "A WCF service endpoint with contract '" + FormatLookupValue(contract) + "' was not found.");
        }

        return endpoint;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the endpoint collection.
    /// </summary>
    /// <returns>An endpoint enumerator.</returns>
    public IEnumerator<LegacyWcfServiceEndpoint> GetEnumerator()
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
