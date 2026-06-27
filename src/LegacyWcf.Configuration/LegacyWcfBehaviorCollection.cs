using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents a typed enumerable collection of WCF behaviour configurations.
/// </summary>
/// <remarks>
/// Phase 3 adds targeted lookup helpers on top of the enumerable behaviour collection.
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
    /// Finds the first behaviour with the specified configuration name.
    /// </summary>
    /// <param name="name">The behaviour configuration name to find, or <see langword="null"/> for an unnamed behaviour.</param>
    /// <returns>The first matching behaviour, or <see langword="null"/> when no behaviour matches.</returns>
    /// <remarks>
    /// Matching is case-insensitive. A <see langword="null"/> lookup value matches an
    /// unnamed behaviour whose <see cref="LegacyWcfBehavior.Name"/> is <see langword="null"/>.
    /// Empty string is treated as an actual empty-string lookup value.
    /// </remarks>
    public LegacyWcfBehavior? Find(string? name)
    {
        return _behaviors.FirstOrDefault(
            behavior => string.Equals(behavior.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the first behaviour with the specified configuration name.
    /// </summary>
    /// <param name="name">The behaviour configuration name to retrieve, or <see langword="null"/> for an unnamed behaviour.</param>
    /// <returns>The first matching behaviour.</returns>
    /// <exception cref="InvalidOperationException">No matching behaviour exists.</exception>
    public LegacyWcfBehavior GetRequired(string? name)
    {
        var behavior = Find(name);

        if (behavior is null)
        {
            throw new InvalidOperationException(
                "A WCF behaviour named '" + FormatLookupValue(name) + "' was not found in this behaviour collection.");
        }

        return behavior;
    }

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

    private static string FormatLookupValue(string? value)
    {
        return value ?? "<null>";
    }
}
