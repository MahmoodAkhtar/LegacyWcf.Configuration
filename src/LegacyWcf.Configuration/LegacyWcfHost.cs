using System;
using System.Collections.Generic;
using System.Linq;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents typed WCF service host configuration read from a legacy <c>&lt;host&gt;</c> element.
/// </summary>
/// <remarks>
/// Phase 2 Stage 2 models host base addresses while preserving the original raw XML for
/// unsupported or unknown host child elements.
/// </remarks>
public sealed class LegacyWcfHost
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfHost"/> class.
    /// </summary>
    /// <param name="baseAddresses">The host base addresses declared under <c>&lt;baseAddresses&gt;</c>.</param>
    /// <param name="timeouts">The optional typed host timeout settings.</param>
    /// <param name="rawElement">The preserved raw <c>&lt;host&gt;</c> element.</param>
    public LegacyWcfHost(
        IEnumerable<string>? baseAddresses,
        LegacyWcfHostTimeouts? timeouts,
        LegacyWcfElement rawElement)
    {
        BaseAddresses = baseAddresses?.ToList() ?? new List<string>();
        Timeouts = timeouts;
        RawElement = rawElement ?? throw new ArgumentNullException(nameof(rawElement));
    }

    /// <summary>
    /// Gets the host base addresses in the order they appeared in the source XML.
    /// </summary>
    /// <remarks>
    /// Entries without a <c>baseAddress</c> attribute are ignored by the typed model but remain
    /// available through <see cref="RawElement"/>.
    /// </remarks>
    public IReadOnlyList<string> BaseAddresses { get; }

    /// <summary>
    /// Gets the optional typed host timeout settings.
    /// </summary>
    public LegacyWcfHostTimeouts? Timeouts { get; }

    /// <summary>
    /// Gets the preserved raw <c>&lt;host&gt;</c> element that produced this typed host model.
    /// </summary>
    public LegacyWcfElement RawElement { get; }
}
