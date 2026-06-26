using System;
using System.Collections.Generic;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents a typed WCF binding configuration read from a legacy <c>&lt;binding&gt;</c> element.
/// </summary>
/// <remarks>
/// Phase 2 Stage 3 models common binding groups while preserving the original raw XML for
/// binding-specific child elements and unsupported configuration details.
/// </remarks>
public sealed class LegacyWcfBinding
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfBinding"/> class.
    /// </summary>
    /// <param name="bindingType">
    /// The parent binding group name, such as <c>basicHttpBinding</c>, <c>wsHttpBinding</c>,
    /// <c>netTcpBinding</c>, or <c>customBinding</c>.
    /// </param>
    /// <param name="name">The optional binding configuration name.</param>
    /// <param name="attributes">The attributes preserved from the source <c>&lt;binding&gt;</c> element.</param>
    /// <param name="rawElement">The preserved raw <c>&lt;binding&gt;</c> element.</param>
    public LegacyWcfBinding(
        string bindingType,
        string? name,
        IReadOnlyDictionary<string, string>? attributes,
        LegacyWcfElement rawElement)
    {
        if (string.IsNullOrWhiteSpace(bindingType))
        {
            throw new ArgumentException("A binding type must be provided.", nameof(bindingType));
        }

        BindingType = bindingType;
        Name = name;
        Attributes = attributes ?? new Dictionary<string, string>();
        RawElement = rawElement ?? throw new ArgumentNullException(nameof(rawElement));
    }

    /// <summary>
    /// Gets the parent binding group name, such as <c>basicHttpBinding</c>.
    /// </summary>
    public string BindingType { get; }

    /// <summary>
    /// Gets the optional binding configuration name.
    /// </summary>
    /// <remarks>
    /// This value is <see langword="null"/> when the source <c>&lt;binding&gt;</c> element has
    /// no <c>name</c> attribute.
    /// </remarks>
    public string? Name { get; }

    /// <summary>
    /// Gets all attributes preserved from the source <c>&lt;binding&gt;</c> element.
    /// </summary>
    /// <remarks>
    /// The collection includes the <c>name</c> attribute when it exists. Unknown attributes
    /// are preserved here without validation.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>
    /// Gets the preserved raw <c>&lt;binding&gt;</c> element that produced this typed binding.
    /// </summary>
    public LegacyWcfElement RawElement { get; }
}
