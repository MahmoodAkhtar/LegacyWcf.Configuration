using System;
using System.Collections.Generic;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents the WCF configuration data read from a legacy configuration file.
/// </summary>
/// <remarks>
/// The raw <c>&lt;system.serviceModel&gt;</c> XML tree remains the source of truth.
/// Phase 2 Stage 1 adds typed services and service endpoints, and Phase 2 Stage 2 adds
/// typed service host and host base address support on top of that preserved raw tree.
/// </remarks>
public sealed class LegacyWcfConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfConfiguration"/> class.
    /// </summary>
    /// <param name="rawSystemServiceModel">
    /// The preserved raw <c>&lt;system.serviceModel&gt;</c> element.
    /// </param>
    /// <param name="diagnostics">
    /// Optional diagnostics associated with the configuration. When <see langword="null"/>,
    /// an empty diagnostics collection is used.
    /// </param>
    /// <param name="services">
    /// Optional typed WCF services. When <see langword="null"/>, an empty service collection is used.
    /// </param>
    public LegacyWcfConfiguration(
        LegacyWcfElement rawSystemServiceModel,
        IReadOnlyList<LegacyWcfDiagnostic>? diagnostics = null,
        LegacyWcfServices? services = null)
    {
        RawSystemServiceModel = rawSystemServiceModel ?? throw new ArgumentNullException(nameof(rawSystemServiceModel));
        Diagnostics = diagnostics ?? Array.Empty<LegacyWcfDiagnostic>();
        Services = services ?? LegacyWcfServices.Empty;
    }

    /// <summary>
    /// Gets the typed WCF services declared under <c>&lt;services&gt;</c>.
    /// </summary>
    /// <remarks>
    /// The collection is empty when no <c>&lt;services&gt;</c> element exists. Stage 2 includes
    /// service host/base address models, but does not include lookup helpers, bindings,
    /// behaviours, or client endpoints.
    /// </remarks>
    public LegacyWcfServices Services { get; }

    /// <summary>
    /// Gets the preserved raw <c>&lt;system.serviceModel&gt;</c> element.
    /// </summary>
    /// <remarks>
    /// This element is the root of the preserved WCF configuration tree. Unknown descendant
    /// elements and attributes may still be present in this raw model even when they are not
    /// recognised by the current library version.
    /// </remarks>
    public LegacyWcfElement RawSystemServiceModel { get; }

    /// <summary>
    /// Gets diagnostics associated with the configuration.
    /// </summary>
    /// <remarks>
    /// The collection is empty when no diagnostics were produced. Diagnostics may describe
    /// warnings or informational issues in later phases without preventing access to the
    /// preserved raw configuration.
    /// </remarks>
    public IReadOnlyList<LegacyWcfDiagnostic> Diagnostics { get; }
}
