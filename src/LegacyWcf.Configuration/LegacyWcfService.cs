using System;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents a typed WCF service read from a legacy <c>&lt;service&gt;</c> element.
/// </summary>
/// <remarks>
/// This type models the common service attributes exposed during Phase 2 Stage 1.
/// Unknown child elements, host configuration, and other unsupported details remain available
/// through <see cref="RawElement"/>.
/// </remarks>
public sealed class LegacyWcfService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfService"/> class.
    /// </summary>
    /// <param name="name">
    /// The service name. When the source <c>&lt;service&gt;</c> element has no <c>name</c>
    /// attribute, pass an empty string.
    /// </param>
    /// <param name="behaviorConfiguration">The optional named service behaviour configuration reference.</param>
    /// <param name="endpoints">The typed service endpoints declared directly below the service.</param>
    /// <param name="rawElement">The preserved raw <c>&lt;service&gt;</c> element.</param>
    public LegacyWcfService(
        string name,
        string? behaviorConfiguration,
        LegacyWcfServiceEndpoints? endpoints,
        LegacyWcfElement rawElement)
    {
        Name = name ?? string.Empty;
        BehaviorConfiguration = behaviorConfiguration;
        Endpoints = endpoints ?? LegacyWcfServiceEndpoints.Empty;
        RawElement = rawElement ?? throw new ArgumentNullException(nameof(rawElement));
    }

    /// <summary>
    /// Gets the configured service name, or an empty string when the source attribute is missing.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the optional named service behaviour configuration reference.
    /// </summary>
    public string? BehaviorConfiguration { get; }

    /// <summary>
    /// Gets the typed service endpoints declared directly below this service.
    /// </summary>
    public LegacyWcfServiceEndpoints Endpoints { get; }

    /// <summary>
    /// Gets the preserved raw <c>&lt;service&gt;</c> element that produced this typed service.
    /// </summary>
    public LegacyWcfElement RawElement { get; }
}
