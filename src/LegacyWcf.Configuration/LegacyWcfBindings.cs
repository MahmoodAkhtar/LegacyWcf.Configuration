namespace LegacyWcf.Configuration;

/// <summary>
/// Represents typed WCF binding configuration groups read from a legacy <c>&lt;bindings&gt;</c> element.
/// </summary>
/// <remarks>
/// Phase 2 Stage 3 exposes the common binding groups <c>basicHttpBinding</c>,
/// <c>wsHttpBinding</c>, <c>netTcpBinding</c>, and <c>customBinding</c>. Unknown binding
/// groups remain available through the raw XML model.
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
}
