using System;
using System.Collections.Generic;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents a typed WCF client endpoint read from a legacy <c>&lt;client&gt;</c> element.
/// </summary>
/// <remarks>
/// Phase 2 Stage 5 models common client endpoint attributes while preserving all source
/// endpoint attributes and the raw XML element.
/// </remarks>
public sealed class LegacyWcfClientEndpoint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfClientEndpoint"/> class.
    /// </summary>
    /// <param name="name">The optional client endpoint name.</param>
    /// <param name="address">The optional client endpoint address.</param>
    /// <param name="binding">The optional WCF binding type.</param>
    /// <param name="bindingConfiguration">The optional named binding configuration reference.</param>
    /// <param name="contract">The optional client contract.</param>
    /// <param name="behaviorConfiguration">The optional named endpoint behaviour configuration reference.</param>
    /// <param name="attributes">The attributes preserved from the source <c>&lt;endpoint&gt;</c> element.</param>
    /// <param name="rawElement">The preserved raw <c>&lt;endpoint&gt;</c> element.</param>
    public LegacyWcfClientEndpoint(
        string? name,
        string? address,
        string? binding,
        string? bindingConfiguration,
        string? contract,
        string? behaviorConfiguration,
        IReadOnlyDictionary<string, string>? attributes,
        LegacyWcfElement rawElement)
    {
        Name = name;
        Address = address;
        Binding = binding;
        BindingConfiguration = bindingConfiguration;
        Contract = contract;
        BehaviorConfiguration = behaviorConfiguration;
        Attributes = attributes ?? new Dictionary<string, string>();
        RawElement = rawElement ?? throw new ArgumentNullException(nameof(rawElement));
    }

    /// <summary>
    /// Gets the optional client endpoint name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the optional client endpoint address.
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
    /// Gets the optional client contract configured for the endpoint.
    /// </summary>
    public string? Contract { get; }

    /// <summary>
    /// Gets the optional named endpoint behaviour configuration reference.
    /// </summary>
    public string? BehaviorConfiguration { get; }

    /// <summary>
    /// Gets all attributes preserved from the source client endpoint element.
    /// </summary>
    /// <remarks>
    /// The collection includes known attributes such as <c>name</c>, <c>address</c>,
    /// <c>binding</c>, <c>bindingConfiguration</c>, <c>contract</c>, and
    /// <c>behaviorConfiguration</c> when they exist. Unknown attributes are preserved here
    /// without validation.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>
    /// Gets the preserved raw <c>&lt;endpoint&gt;</c> element that produced this typed client endpoint.
    /// </summary>
    public LegacyWcfElement RawElement { get; }
}
