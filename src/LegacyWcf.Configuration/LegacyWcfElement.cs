using System.Collections.Generic;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents a preserved XML element in the raw legacy WCF configuration tree.
/// </summary>
/// <remarks>
/// Phase 1 uses this type to preserve the XML below
/// <c>&lt;configuration&gt;/&lt;system.serviceModel&gt;</c>. Unknown elements and
/// attributes may still be represented by this type even when they are not recognised by
/// the current library version.
/// </remarks>
public sealed class LegacyWcfElement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfElement"/> class.
    /// </summary>
    /// <param name="name">
    /// The element name. Namespaced elements are represented using an expanded name format.
    /// </param>
    /// <param name="path">
    /// The preserved path to the element within the raw tree, such as
    /// <c>configuration/system.serviceModel/services/service</c>.
    /// </param>
    /// <param name="attributes">
    /// Optional element attributes keyed by attribute name. When <see langword="null"/>,
    /// an empty attribute collection is used.
    /// </param>
    /// <param name="children">
    /// Optional child elements. When <see langword="null"/>, an empty child collection is used.
    /// </param>
    /// <param name="value">
    /// Optional direct text value for the element. This may be <see langword="null"/> when
    /// the element has no meaningful direct text value or contains only child elements.
    /// </param>
    /// <param name="rawXml">
    /// Optional preserved raw XML for this element. This may be <see langword="null"/> when
    /// raw XML is unavailable.
    /// </param>
    /// <param name="sourceFilePath">
    /// Optional path to the source configuration file. This may be <see langword="null"/>
    /// when the source file path is unavailable.
    /// </param>
    /// <param name="lineNumber">
    /// Optional one-based line number for the element in the source file. This may be
    /// <see langword="null"/> when line information is unavailable.
    /// </param>
    /// <param name="isKnownElement">
    /// <see langword="true"/> when the element name is recognised by the current raw reader;
    /// otherwise, <see langword="false"/>. Unknown elements are still preserved.
    /// </param>
    public LegacyWcfElement(
        string name,
        string path,
        IReadOnlyDictionary<string, string>? attributes = null,
        IReadOnlyList<LegacyWcfElement>? children = null,
        string? value = null,
        string? rawXml = null,
        string? sourceFilePath = null,
        int? lineNumber = null,
        bool isKnownElement = false)
    {
        Name = name;
        Path = path;
        Attributes = attributes ?? new Dictionary<string, string>();
        Children = children ?? new List<LegacyWcfElement>();
        Value = value;
        RawXml = rawXml;
        SourceFilePath = sourceFilePath;
        LineNumber = lineNumber;
        IsKnownElement = isKnownElement;
    }

    /// <summary>
    /// Gets the element name.
    /// </summary>
    /// <remarks>
    /// For elements without an XML namespace, this is the local name. Namespaced elements
    /// are represented using an expanded name format.
    /// </remarks>
    public string Name { get; }

    /// <summary>
    /// Gets the preserved path to the element within the raw tree.
    /// </summary>
    /// <remarks>
    /// For example, the root WCF element path is
    /// <c>configuration/system.serviceModel</c>.
    /// </remarks>
    public string Path { get; }

    /// <summary>
    /// Gets the attributes preserved from the XML element.
    /// </summary>
    /// <remarks>
    /// Unknown attributes are preserved here. The collection is empty when the element has
    /// no attributes.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>
    /// Gets the child elements preserved below this element.
    /// </summary>
    /// <remarks>
    /// Unknown child elements are preserved here. The collection is empty when the element
    /// has no child elements.
    /// </remarks>
    public IReadOnlyList<LegacyWcfElement> Children { get; }

    /// <summary>
    /// Gets the optional direct text value of the element.
    /// </summary>
    /// <remarks>
    /// This value may be <see langword="null"/> when the element has no meaningful direct
    /// text value or when the element contains only child elements.
    /// </remarks>
    public string? Value { get; }

    /// <summary>
    /// Gets the optional preserved raw XML for this element.
    /// </summary>
    /// <remarks>
    /// This value may be <see langword="null"/> when raw XML is unavailable.
    /// </remarks>
    public string? RawXml { get; }

    /// <summary>
    /// Gets the optional path to the source configuration file.
    /// </summary>
    /// <remarks>
    /// This value may be <see langword="null"/> when the source file path is unavailable.
    /// </remarks>
    public string? SourceFilePath { get; }

    /// <summary>
    /// Gets the optional one-based line number for this element in the source file.
    /// </summary>
    /// <remarks>
    /// This value may be <see langword="null"/> when line information is unavailable.
    /// </remarks>
    public int? LineNumber { get; }

    /// <summary>
    /// Gets a value indicating whether the element name is recognised by the current raw reader.
    /// </summary>
    /// <remarks>
    /// A value of <see langword="false"/> does not mean the XML is invalid. It means the
    /// element was not in the reader's current known-element list. Unknown elements are
    /// still preserved in the raw tree.
    /// </remarks>
    public bool IsKnownElement { get; }
}