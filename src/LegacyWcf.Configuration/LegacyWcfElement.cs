using System.Collections.Generic;

namespace LegacyWcf.Configuration;

public sealed class LegacyWcfElement
{
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

    public string Name { get; }

    public string Path { get; }

    public IReadOnlyDictionary<string, string> Attributes { get; }

    public IReadOnlyList<LegacyWcfElement> Children { get; }

    public string? Value { get; }

    public string? RawXml { get; }

    public string? SourceFilePath { get; }

    public int? LineNumber { get; }

    public bool IsKnownElement { get; }
}
