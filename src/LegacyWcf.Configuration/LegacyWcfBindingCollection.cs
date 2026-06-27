using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents a typed enumerable collection of WCF binding configurations.
/// </summary>
/// <remarks>
/// Phase 3 adds targeted lookup helpers on top of the enumerable binding collection.
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
    /// Finds the first binding with the specified configuration name.
    /// </summary>
    /// <param name="name">The binding configuration name to find, or <see langword="null"/> for an unnamed binding.</param>
    /// <returns>The first matching binding, or <see langword="null"/> when no binding matches.</returns>
    /// <remarks>
    /// Matching is case-insensitive. A <see langword="null"/> lookup value matches an
    /// unnamed binding whose <see cref="LegacyWcfBinding.Name"/> is <see langword="null"/>.
    /// Empty string is treated as an actual empty-string lookup value.
    /// </remarks>
    public LegacyWcfBinding? Find(string? name)
    {
        return _bindings.FirstOrDefault(
            binding => string.Equals(binding.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the first binding with the specified configuration name.
    /// </summary>
    /// <param name="name">The binding configuration name to retrieve, or <see langword="null"/> for an unnamed binding.</param>
    /// <returns>The first matching binding.</returns>
    /// <exception cref="InvalidOperationException">No matching binding exists.</exception>
    public LegacyWcfBinding GetRequired(string? name)
    {
        var binding = Find(name);

        if (binding is null)
        {
            throw new InvalidOperationException(
                "A WCF binding named '" + FormatLookupValue(name) + "' was not found in this binding collection.");
        }

        return binding;
    }

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

    private static string FormatLookupValue(string? value)
    {
        return value ?? "<null>";
    }
}
