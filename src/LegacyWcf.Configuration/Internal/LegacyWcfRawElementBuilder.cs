using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace LegacyWcf.Configuration.Internal;

internal static class LegacyWcfRawElementBuilder
{
    private static readonly HashSet<string> KnownElementNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "system.serviceModel",
        "services",
        "service",
        "endpoint",
        "host",
        "baseAddresses",
        "add",
        "timeouts",
        "bindings",
        "binding",
        "basicHttpBinding",
        "wsHttpBinding",
        "netTcpBinding",
        "customBinding",
        "behaviors",
        "behaviours",
        "serviceBehaviors",
        "serviceBehaviours",
        "endpointBehaviors",
        "endpointBehaviours",
        "behavior",
        "behaviour",
        "client",
        "serviceHostingEnvironment",
        "serviceMetadata",
        "serviceDebug",
        "serviceThrottling",
        "security",
        "readerQuotas",
        "transport",
        "reliableSession"
    };

    public static LegacyWcfElement Build(XElement element, string sourceFilePath, string parentPath)
    {
        if (element is null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        var name = GetName(element.Name);
        var path = string.IsNullOrWhiteSpace(parentPath)
            ? name
            : parentPath + "/" + name;

        var attributes = element.Attributes()
            .ToDictionary(attribute => GetName(attribute.Name), attribute => attribute.Value, StringComparer.OrdinalIgnoreCase);

        var children = element.Elements()
            .Select(child => Build(child, sourceFilePath, path))
            .ToList();

        var lineInfo = (IXmlLineInfo)element;
        var lineNumber = lineInfo.HasLineInfo() ? lineInfo.LineNumber : (int?)null;

        return new LegacyWcfElement(
            name: name,
            path: path,
            attributes: attributes,
            children: children,
            value: GetElementValue(element),
            rawXml: element.ToString(SaveOptions.DisableFormatting),
            sourceFilePath: sourceFilePath,
            lineNumber: lineNumber,
            isKnownElement: KnownElementNames.Contains(element.Name.LocalName));
    }

    private static string GetElementValue(XElement element)
    {
        if (element.HasElements)
        {
            var directText = string.Concat(element.Nodes().OfType<XText>().Select(text => text.Value));
            return string.IsNullOrWhiteSpace(directText) ? null : directText;
        }

        return string.IsNullOrWhiteSpace(element.Value) ? null : element.Value;
    }

    private static string GetName(XName name)
    {
        return string.IsNullOrWhiteSpace(name.NamespaceName)
            ? name.LocalName
            : "{" + name.NamespaceName + "}" + name.LocalName;
    }
}
