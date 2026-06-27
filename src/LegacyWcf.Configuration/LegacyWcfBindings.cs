using System;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents typed WCF binding configuration groups read from a legacy <c>&lt;bindings&gt;</c> element.
/// </summary>
/// <remarks>
/// Phase 3 adds top-level lookup helpers for the known binding groups
/// <c>basicHttpBinding</c>, <c>wsHttpBinding</c>, <c>netTcpBinding</c>, and <c>customBinding</c>.
/// Unknown binding groups remain available through the raw XML model.
/// </remarks>
public sealed class LegacyWcfBindings
{
    /// <summary>
    /// Gets an empty binding container.
    /// </summary>
    public static LegacyWcfBindings Empty { get; } = new LegacyWcfBindings(
        basicHttp: LegacyWcfBindingCollection.Empty,
        wsHttp: LegacyWcfBindingCollection.Empty,
        netTcp: LegacyWcfBindingCollection.Empty,
        custom: LegacyWcfBindingCollection.Empty);

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfBindings"/> class.
    /// </summary>
    /// <param name="basicHttp">The typed <c>basicHttpBinding</c> configurations.</param>
    /// <param name="wsHttp">The typed <c>wsHttpBinding</c> configurations.</param>
    /// <param name="netTcp">The typed <c>netTcpBinding</c> configurations.</param>
    /// <param name="custom">The typed <c>customBinding</c> configurations.</param>
    public LegacyWcfBindings(
        LegacyWcfBindingCollection? basicHttp = null,
        LegacyWcfBindingCollection? wsHttp = null,
        LegacyWcfBindingCollection? netTcp = null,
        LegacyWcfBindingCollection? custom = null)
    {
        BasicHttp = basicHttp ?? LegacyWcfBindingCollection.Empty;
        WsHttp = wsHttp ?? LegacyWcfBindingCollection.Empty;
        NetTcp = netTcp ?? LegacyWcfBindingCollection.Empty;
        Custom = custom ?? LegacyWcfBindingCollection.Empty;
    }

    /// <summary>
    /// Gets the typed <c>basicHttpBinding</c> configurations.
    /// </summary>
    public LegacyWcfBindingCollection BasicHttp { get; }

    /// <summary>
    /// Gets the typed <c>wsHttpBinding</c> configurations.
    /// </summary>
    public LegacyWcfBindingCollection WsHttp { get; }

    /// <summary>
    /// Gets the typed <c>netTcpBinding</c> configurations.
    /// </summary>
    public LegacyWcfBindingCollection NetTcp { get; }

    /// <summary>
    /// Gets the typed <c>customBinding</c> configurations.
    /// </summary>
    public LegacyWcfBindingCollection Custom { get; }

    /// <summary>
    /// Finds the first binding for the specified WCF binding type and configuration name.
    /// </summary>
    /// <param name="bindingType">The WCF binding type, such as <c>basicHttpBinding</c>.</param>
    /// <param name="name">The binding configuration name to find, or <see langword="null"/> for an unnamed binding.</param>
    /// <returns>The first matching binding, or <see langword="null"/> when no binding matches.</returns>
    public LegacyWcfBinding? Find(string? bindingType, string? name)
    {
        var collection = GetCollection(bindingType);
        return collection?.Find(name);
    }

    /// <summary>
    /// Gets the first binding for the specified WCF binding type and configuration name.
    /// </summary>
    /// <param name="bindingType">The WCF binding type, such as <c>basicHttpBinding</c>.</param>
    /// <param name="name">The binding configuration name to retrieve, or <see langword="null"/> for an unnamed binding.</param>
    /// <returns>The first matching binding.</returns>
    /// <exception cref="InvalidOperationException">No matching binding exists, or the binding type is missing or unsupported.</exception>
    public LegacyWcfBinding GetRequired(string? bindingType, string? name)
    {
        var collection = GetCollection(bindingType);

        if (collection is null)
        {
            throw new InvalidOperationException(
                "A WCF binding type '" + FormatLookupValue(bindingType) + "' is not supported by this lookup. " +
                "Supported binding types are basicHttpBinding, wsHttpBinding, netTcpBinding, and customBinding.");
        }

        var binding = collection.Find(name);

        if (binding is null)
        {
            throw new InvalidOperationException(
                "A WCF binding of type '" + bindingType + "' named '" + FormatLookupValue(name) + "' was not found.");
        }

        return binding;
    }

    private LegacyWcfBindingCollection? GetCollection(string? bindingType)
    {
        if (string.IsNullOrWhiteSpace(bindingType))
        {
            return null;
        }

        if (string.Equals(bindingType, "basicHttpBinding", StringComparison.OrdinalIgnoreCase))
        {
            return BasicHttp;
        }

        if (string.Equals(bindingType, "wsHttpBinding", StringComparison.OrdinalIgnoreCase))
        {
            return WsHttp;
        }

        if (string.Equals(bindingType, "netTcpBinding", StringComparison.OrdinalIgnoreCase))
        {
            return NetTcp;
        }

        if (string.Equals(bindingType, "customBinding", StringComparison.OrdinalIgnoreCase))
        {
            return Custom;
        }

        return null;
    }

    private static string FormatLookupValue(string? value)
    {
        return value ?? "<null>";
    }
}
