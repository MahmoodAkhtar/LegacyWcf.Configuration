using System;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents typed WCF service host timeout settings read from a legacy <c>&lt;timeouts&gt;</c> element.
/// </summary>
/// <remarks>
/// Phase 2 Stage 2 keeps timeout values as strings so the original configuration values are
/// preserved without enforcing WCF runtime parsing rules.
/// </remarks>
public sealed class LegacyWcfHostTimeouts
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfHostTimeouts"/> class.
    /// </summary>
    /// <param name="closeTimeout">The optional <c>closeTimeout</c> attribute value.</param>
    /// <param name="openTimeout">The optional <c>openTimeout</c> attribute value.</param>
    /// <param name="rawElement">The preserved raw <c>&lt;timeouts&gt;</c> element.</param>
    public LegacyWcfHostTimeouts(
        string? closeTimeout,
        string? openTimeout,
        LegacyWcfElement rawElement)
    {
        CloseTimeout = closeTimeout;
        OpenTimeout = openTimeout;
        RawElement = rawElement ?? throw new ArgumentNullException(nameof(rawElement));
    }

    /// <summary>
    /// Gets the optional <c>closeTimeout</c> attribute value.
    /// </summary>
    public string? CloseTimeout { get; }

    /// <summary>
    /// Gets the optional <c>openTimeout</c> attribute value.
    /// </summary>
    public string? OpenTimeout { get; }

    /// <summary>
    /// Gets the preserved raw <c>&lt;timeouts&gt;</c> element that produced this typed timeout model.
    /// </summary>
    public LegacyWcfElement RawElement { get; }
}
