using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using LegacyWcf.Configuration.Internal;

namespace LegacyWcf.Configuration;

/// <summary>
/// Reads legacy WCF configuration from <c>app.config</c>, <c>web.config</c>, or external
/// <c>.config</c> files.
/// </summary>
/// <remarks>
/// The reader locates <c>&lt;configuration&gt;/&lt;system.serviceModel&gt;</c>,
/// preserves the raw XML tree, and builds additive typed models for supported WCF concepts.
/// Phase 2 Stage 3 supports typed services, service endpoints, service hosts, host
/// base addresses, and initial typed binding enumeration.
/// </remarks>
public static class LegacyWcfConfigurationReader
{
    /// <summary>
    /// Reads a configuration file and attempts to locate
    /// <c>&lt;configuration&gt;/&lt;system.serviceModel&gt;</c>.
    /// </summary>
    /// <param name="filePath">
    /// The path to an <c>app.config</c>, <c>web.config</c>, or external <c>.config</c> file.
    /// </param>
    /// <returns>
    /// A <see cref="LegacyWcfConfigurationReadResult"/> describing whether a usable
    /// WCF configuration was read. When reading fails,
    /// <see cref="LegacyWcfConfigurationReadResult.Configuration"/> is <see langword="null"/>
    /// and <see cref="LegacyWcfConfigurationReadResult.Diagnostics"/> explains the outcome.
    /// </returns>
    /// <remarks>
    /// Normal read failures, such as a missing file, malformed XML, missing
    /// <c>&lt;configuration&gt;</c> root, or missing <c>&lt;system.serviceModel&gt;</c>
    /// section, are reported through diagnostics rather than by returning a partially
    /// populated configuration.
    /// </remarks>
    public static LegacyWcfConfigurationReadResult Read(string filePath)
    {
        var sourceFilePath = GetSourceFilePath(filePath);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Failure(new LegacyWcfDiagnostic(
                LegacyWcfDiagnosticSeverity.Error,
                "A configuration file path must be provided.",
                "LWC0001",
                sourceFilePath));
        }

        if (!File.Exists(filePath))
        {
            return Failure(new LegacyWcfDiagnostic(
                LegacyWcfDiagnosticSeverity.Error,
                "The configuration file could not be found: " + sourceFilePath,
                "LWC0001",
                sourceFilePath));
        }

        XDocument document;

        try
        {
            document = LoadDocument(filePath);
        }
        catch (XmlException exception)
        {
            return Failure(new LegacyWcfDiagnostic(
                LegacyWcfDiagnosticSeverity.Error,
                "The configuration XML could not be loaded or parsed: " + exception.Message,
                "LWC0003",
                sourceFilePath,
                exception.LineNumber > 0 ? exception.LineNumber : (int?)null));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(new LegacyWcfDiagnostic(
                LegacyWcfDiagnosticSeverity.Error,
                "The configuration file could not be read: " + exception.Message,
                "LWC0002",
                sourceFilePath));
        }
        catch (IOException exception)
        {
            return Failure(new LegacyWcfDiagnostic(
                LegacyWcfDiagnosticSeverity.Error,
                "The configuration file could not be read: " + exception.Message,
                "LWC0002",
                sourceFilePath));
        }

        var configurationElement = document.Root;

        if (configurationElement is null || !IsNamed(configurationElement, "configuration"))
        {
            return Failure(new LegacyWcfDiagnostic(
                LegacyWcfDiagnosticSeverity.Error,
                "The <configuration> element was not found at the root of the configuration file.",
                "LWC0004",
                sourceFilePath,
                GetLineNumber(configurationElement)));
        }

        var systemServiceModelElement = configurationElement.Elements()
            .FirstOrDefault(element => IsNamed(element, "system.serviceModel"));

        if (systemServiceModelElement is null)
        {
            return Failure(new LegacyWcfDiagnostic(
                LegacyWcfDiagnosticSeverity.Error,
                "The <system.serviceModel> element was not found under <configuration>.",
                "LWC0005",
                sourceFilePath,
                GetLineNumber(configurationElement)));
        }

        var rawSystemServiceModel = LegacyWcfRawElementBuilder.Build(
            systemServiceModelElement,
            sourceFilePath,
            "configuration");

        var diagnostics = Array.Empty<LegacyWcfDiagnostic>();
        var services = LegacyWcfTypedModelBuilder.BuildServices(rawSystemServiceModel);
        var bindings = LegacyWcfTypedModelBuilder.BuildBindings(rawSystemServiceModel);
        var configuration = new LegacyWcfConfiguration(rawSystemServiceModel, diagnostics, services, bindings);

        return new LegacyWcfConfigurationReadResult(
            success: true,
            configuration: configuration,
            diagnostics: diagnostics);
    }

    private static XDocument LoadDocument(string filePath)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null
        };

        using (var stream = File.OpenRead(filePath))
        using (var reader = XmlReader.Create(stream, settings))
        {
            return XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        }
    }

    private static LegacyWcfConfigurationReadResult Failure(LegacyWcfDiagnostic diagnostic)
    {
        return new LegacyWcfConfigurationReadResult(
            success: false,
            configuration: null,
            diagnostics: new[] { diagnostic });
    }

    private static bool IsNamed(XElement element, string localName)
    {
        return string.Equals(element.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase);
    }

    private static int? GetLineNumber(XObject? node)
    {
        if (node is null)
        {
            return null;
        }

        var lineInfo = (IXmlLineInfo)node;
        return lineInfo.HasLineInfo() ? lineInfo.LineNumber : (int?)null;
    }

    private static string? GetSourceFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return filePath;
        }

        try
        {
            return Path.GetFullPath(filePath);
        }
        catch
        {
            return filePath;
        }
    }
}
