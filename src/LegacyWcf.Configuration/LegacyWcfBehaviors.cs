namespace LegacyWcf.Configuration;

/// <summary>
/// Represents typed WCF behaviour configuration groups read from legacy
/// <c>&lt;behaviors&gt;</c> or <c>&lt;behaviours&gt;</c> elements.
/// </summary>
/// <remarks>
/// Phase 2 Stage 4 exposes service and endpoint behaviour collections. Unknown behaviour
/// groups remain available through the raw XML model.
/// </remarks>
public sealed class LegacyWcfBehaviors
{
    /// <summary>
    /// Gets an empty behaviour container.
    /// </summary>
    public static LegacyWcfBehaviors Empty { get; } = new LegacyWcfBehaviors(
        serviceBehaviors: LegacyWcfBehaviorCollection.Empty,
        endpointBehaviors: LegacyWcfBehaviorCollection.Empty);

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfBehaviors"/> class.
    /// </summary>
    /// <param name="serviceBehaviors">The typed service behaviour configurations.</param>
    /// <param name="endpointBehaviors">The typed endpoint behaviour configurations.</param>
    public LegacyWcfBehaviors(
        LegacyWcfBehaviorCollection? serviceBehaviors = null,
        LegacyWcfBehaviorCollection? endpointBehaviors = null)
    {
        ServiceBehaviors = serviceBehaviors ?? LegacyWcfBehaviorCollection.Empty;
        EndpointBehaviors = endpointBehaviors ?? LegacyWcfBehaviorCollection.Empty;
    }

    /// <summary>
    /// Gets the typed service behaviour configurations.
    /// </summary>
    public LegacyWcfBehaviorCollection ServiceBehaviors { get; }

    /// <summary>
    /// Gets the typed endpoint behaviour configurations.
    /// </summary>
    public LegacyWcfBehaviorCollection EndpointBehaviors { get; }
}
