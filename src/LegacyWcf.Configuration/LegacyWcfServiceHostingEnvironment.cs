using System;
using System.Collections.Generic;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents typed WCF service hosting environment configuration read from a legacy
/// <c>&lt;serviceHostingEnvironment&gt;</c> element.
/// </summary>
/// <remarks>
/// Phase 2 Stage 6 models common service hosting environment attributes while preserving
/// all source attributes and the original raw XML element.
/// </remarks>
public sealed class LegacyWcfServiceHostingEnvironment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfServiceHostingEnvironment"/> class.
    /// </summary>
    /// <param name="aspNetCompatibilityEnabled">
    /// The optional <c>aspNetCompatibilityEnabled</c> attribute value.
    /// </param>
    /// <param name="multipleSiteBindingsEnabled">
    /// The optional <c>multipleSiteBindingsEnabled</c> attribute value.
    /// </param>
    /// <param name="attributes">
    /// The attributes preserved from the source <c>&lt;serviceHostingEnvironment&gt;</c> element.
    /// </param>
    /// <param name="rawElement">
    /// The preserved raw <c>&lt;serviceHostingEnvironment&gt;</c> element.
    /// </param>
    public LegacyWcfServiceHostingEnvironment(
        string? aspNetCompatibilityEnabled,
        string? multipleSiteBindingsEnabled,
        IReadOnlyDictionary<string, string>? attributes,
        LegacyWcfElement rawElement)
    {
        AspNetCompatibilityEnabled = aspNetCompatibilityEnabled;
        MultipleSiteBindingsEnabled = multipleSiteBindingsEnabled;
        Attributes = attributes ?? new Dictionary<string, string>();
        RawElement = rawElement ?? throw new ArgumentNullException(nameof(rawElement));
    }

    /// <summary>
    /// Gets the optional <c>aspNetCompatibilityEnabled</c> attribute value.
    /// </summary>
    /// <remarks>
    /// The value is preserved as a string exactly as it appears in the source XML.
    /// </remarks>
    public string? AspNetCompatibilityEnabled { get; }

    /// <summary>
    /// Gets the optional <c>multipleSiteBindingsEnabled</c> attribute value.
    /// </summary>
    /// <remarks>
    /// The value is preserved as a string exactly as it appears in the source XML.
    /// </remarks>
    public string? MultipleSiteBindingsEnabled { get; }

    /// <summary>
    /// Gets all attributes preserved from the source service hosting environment element.
    /// </summary>
    /// <remarks>
    /// The collection includes known attributes such as <c>aspNetCompatibilityEnabled</c>
    /// and <c>multipleSiteBindingsEnabled</c> when they exist. Unknown attributes are
    /// preserved here without validation.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>
    /// Gets the preserved raw <c>&lt;serviceHostingEnvironment&gt;</c> element that produced this typed model.
    /// </summary>
    public LegacyWcfElement RawElement { get; }
}
