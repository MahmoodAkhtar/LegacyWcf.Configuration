using System;
using System.Collections.Generic;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents the WCF configuration data read from a legacy configuration file.
/// </summary>
/// <remarks>
/// The raw <c>&lt;system.serviceModel&gt;</c> XML tree remains the source of truth.
/// Phase 2 Stage 1 adds typed services and service endpoints, Phase 2 Stage 2 adds
/// typed service host and host base address support, Phase 2 Stage 3 adds initial
/// typed binding support, Phase 2 Stage 4 adds initial typed behaviour support, and
/// Phase 2 Stage 5 adds typed client endpoint support on top of that preserved raw tree.
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
    /// <param name="bindings">
    /// Optional typed WCF bindings. When <see langword="null"/>, an empty bindings container is used.
    /// </param>
    /// <param name="behaviors">
    /// Optional typed WCF behaviours. When <see langword="null"/>, an empty behaviours container is used.
    /// </param>
    /// <param name="client">
    /// Optional typed WCF client configuration. This remains <see langword="null"/> when no
    /// source <c>&lt;client&gt;</c> element exists.
    /// </param>
    public LegacyWcfConfiguration(
        LegacyWcfElement rawSystemServiceModel,
        IReadOnlyList<LegacyWcfDiagnostic>? diagnostics = null,
        LegacyWcfServices? services = null,
        LegacyWcfBindings? bindings = null,
        LegacyWcfBehaviors? behaviors = null,
        LegacyWcfClient? client = null)
    {
        RawSystemServiceModel = rawSystemServiceModel ?? throw new ArgumentNullException(nameof(rawSystemServiceModel));
        Diagnostics = diagnostics ?? Array.Empty<LegacyWcfDiagnostic>();
        Services = services ?? LegacyWcfServices.Empty;
        Bindings = bindings ?? LegacyWcfBindings.Empty;
        Behaviors = behaviors ?? LegacyWcfBehaviors.Empty;
        Client = client;
    }

    /// <summary>
    /// Gets the typed WCF services declared under <c>&lt;services&gt;</c>.
    /// </summary>
    /// <remarks>
    /// The collection is empty when no <c>&lt;services&gt;</c> element exists. Stage 4 includes
    /// service host/base address models, initial typed binding enumeration, and initial typed
    /// behaviour enumeration, but does not include lookup helpers or client endpoint lookup helpers.
    /// </remarks>
    public LegacyWcfServices Services { get; }

    /// <summary>
    /// Gets the typed WCF bindings declared under <c>&lt;bindings&gt;</c>.
    /// </summary>
    /// <remarks>
    /// The container is empty when no <c>&lt;bindings&gt;</c> element exists. Stage 3 exposes
    /// common binding groups for enumeration only; lookup helpers and cross-reference
    /// validation are planned for later phases.
    /// </remarks>
    public LegacyWcfBindings Bindings { get; }

    /// <summary>
    /// Gets the typed WCF behaviours declared under <c>&lt;behaviors&gt;</c> or
    /// <c>&lt;behaviours&gt;</c>.
    /// </summary>
    /// <remarks>
    /// The container is empty when no behaviour elements exist. Stage 4 exposes service
    /// and endpoint behaviour collections for enumeration only; lookup helpers and
    /// cross-reference validation are planned for later phases.
    /// </remarks>
    public LegacyWcfBehaviors Behaviors { get; }

    /// <summary>
    /// Gets the typed WCF client configuration declared under <c>&lt;client&gt;</c>.
    /// </summary>
    /// <remarks>
    /// This value is <see langword="null"/> when no source <c>&lt;client&gt;</c> element exists.
    /// If the source element exists but has no direct endpoint children, this value is non-null
    /// and its endpoint collection is empty. Stage 5 exposes enumeration only; lookup helpers
    /// and cross-reference validation are planned for later phases.
    /// </remarks>
    public LegacyWcfClient? Client { get; }

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
