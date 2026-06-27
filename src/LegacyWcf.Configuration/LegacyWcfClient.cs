using System;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents typed WCF client configuration read from a legacy <c>&lt;client&gt;</c> element.
/// </summary>
/// <remarks>
/// Phase 2 Stage 5 models client endpoint entries while preserving the original raw XML
/// for unsupported or unknown client child elements.
/// </remarks>
public sealed class LegacyWcfClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfClient"/> class.
    /// </summary>
    /// <param name="endpoints">The typed client endpoints declared directly below <c>&lt;client&gt;</c>.</param>
    /// <param name="rawElement">The preserved raw <c>&lt;client&gt;</c> element.</param>
    public LegacyWcfClient(
        LegacyWcfClientEndpoints? endpoints,
        LegacyWcfElement rawElement)
    {
        Endpoints = endpoints ?? LegacyWcfClientEndpoints.Empty;
        RawElement = rawElement ?? throw new ArgumentNullException(nameof(rawElement));
    }

    /// <summary>
    /// Gets the typed client endpoints declared directly below the source <c>&lt;client&gt;</c> element.
    /// </summary>
    public LegacyWcfClientEndpoints Endpoints { get; }

    /// <summary>
    /// Gets the preserved raw <c>&lt;client&gt;</c> element that produced this typed client model.
    /// </summary>
    public LegacyWcfElement RawElement { get; }
}
