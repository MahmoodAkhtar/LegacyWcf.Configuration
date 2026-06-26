using System;
using System.Collections.Generic;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents a typed WCF behaviour configuration read from a legacy <c>&lt;behavior&gt;</c>
/// or <c>&lt;behaviour&gt;</c> element.
/// </summary>
/// <remarks>
/// Phase 2 Stage 4 models service and endpoint behaviour entries while preserving the
/// original raw XML for behaviour-specific child elements and unsupported configuration details.
/// </remarks>
public sealed class LegacyWcfBehavior
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfBehavior"/> class.
    /// </summary>
    /// <param name="behaviorType">
    /// The normalized behaviour kind. Stage 4 uses <c>serviceBehavior</c> and
    /// <c>endpointBehavior</c>.
    /// </param>
    /// <param name="name">The optional behaviour configuration name.</param>
    /// <param name="attributes">
    /// The attributes preserved from the source <c>&lt;behavior&gt;</c> or
    /// <c>&lt;behaviour&gt;</c> element.
    /// </param>
    /// <param name="rawElement">
    /// The preserved raw <c>&lt;behavior&gt;</c> or <c>&lt;behaviour&gt;</c> element.
    /// </param>
    public LegacyWcfBehavior(
        string behaviorType,
        string? name,
        IReadOnlyDictionary<string, string>? attributes,
        LegacyWcfElement rawElement)
    {
        if (string.IsNullOrWhiteSpace(behaviorType))
        {
            throw new ArgumentException("A behaviour type must be provided.", nameof(behaviorType));
        }

        BehaviorType = behaviorType;
        Name = name;
        Attributes = attributes ?? new Dictionary<string, string>();
        RawElement = rawElement ?? throw new ArgumentNullException(nameof(rawElement));
    }

    /// <summary>
    /// Gets the normalized behaviour kind, such as <c>serviceBehavior</c> or <c>endpointBehavior</c>.
    /// </summary>
    public string BehaviorType { get; }

    /// <summary>
    /// Gets the optional behaviour configuration name.
    /// </summary>
    /// <remarks>
    /// This value is <see langword="null"/> when the source behaviour element has no
    /// <c>name</c> attribute.
    /// </remarks>
    public string? Name { get; }

    /// <summary>
    /// Gets all attributes preserved from the source behaviour element.
    /// </summary>
    /// <remarks>
    /// The collection includes the <c>name</c> attribute when it exists. Unknown attributes
    /// are preserved here without validation.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>
    /// Gets the preserved raw behaviour element that produced this typed behaviour.
    /// </summary>
    public LegacyWcfElement RawElement { get; }
}
