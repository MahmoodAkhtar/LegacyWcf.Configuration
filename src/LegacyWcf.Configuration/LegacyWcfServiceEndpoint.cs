using System;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents a typed WCF service endpoint read from a legacy
/// <c>&lt;service&gt;</c> element.
/// </summary>
/// <remarks>
/// This type models the common endpoint attributes exposed during Phase 2 Stage 1.
/// The original XML remains available through <see cref="RawElement"/> so that values
/// not yet strongly modelled are still preserved.
/// </remarks>
public sealed class LegacyWcfServiceEndpoint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfServiceEndpoint"/> class.
    /// </summary>
    /// <param name="name">The optional endpoint name.</param>
    /// <param name="address">The optional endpoint address.</param>
    /// <param name="binding">The optional WCF binding type.</param>
    /// <param name="bindingConfiguration">The optional named binding configuration reference.</param>
    /// <param name="contract">The optional service contract.</param>
    /// <param name="behaviorConfiguration">The optional named endpoint behaviour configuration reference.</param>
    /// <param name="rawElement">The preserved raw <c>&lt;endpoint&gt;</c> element.</param>
    public LegacyWcfServiceEndpoint(
        string? name,
        string? address,
        string? binding,
        string? bindingConfiguration,
        string? contract,
        string? behaviorConfiguration,
        LegacyWcfElement rawElement)
    {
        Name = name;
        Address = address;
        Binding = binding;
        BindingConfiguration = bindingConfiguration;
        Contract = contract;
        BehaviorConfiguration = behaviorConfiguration;
        RawElement = rawElement ?? throw new ArgumentNullException(nameof(rawElement));
    }

    /// <summary>
    /// Gets the optional endpoint name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the optional endpoint address.
    /// </summary>
    public string? Address { get; }

    /// <summary>
    /// Gets the optional WCF binding type, such as <c>basicHttpBinding</c>.
    /// </summary>
    public string? Binding { get; }

    /// <summary>
    /// Gets the optional named binding configuration reference.
    /// </summary>
    public string? BindingConfiguration { get; }

    /// <summary>
    /// Gets the optional service contract configured for the endpoint.
    /// </summary>
    public string? Contract { get; }

    /// <summary>
    /// Gets the optional named endpoint behaviour configuration reference.
    /// </summary>
    public string? BehaviorConfiguration { get; }

    /// <summary>
    /// Gets the preserved raw <c>&lt;endpoint&gt;</c> element that produced this typed endpoint.
    /// </summary>
    public LegacyWcfElement RawElement { get; }
}
