using System;
using System.Collections.Generic;

namespace LegacyWcf.Configuration;

/// <summary>
/// Represents the outcome of reading a legacy WCF configuration file.
/// </summary>
/// <remarks>
/// A result can fail because the file is missing, unreadable, malformed, does not contain
/// a <c>&lt;configuration&gt;</c> root, or does not contain a
/// <c>&lt;system.serviceModel&gt;</c> section.
/// </remarks>
public sealed class LegacyWcfConfigurationReadResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyWcfConfigurationReadResult"/> class.
    /// </summary>
    /// <param name="success">
    /// <see langword="true"/> when the read produced a usable
    /// <see cref="LegacyWcfConfiguration"/>; otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="configuration">
    /// The configuration produced by the read. This is <see langword="null"/> when
    /// <paramref name="success"/> is <see langword="false"/>.
    /// </param>
    /// <param name="diagnostics">
    /// Optional diagnostics explaining read errors, warnings, or informational outcomes.
    /// When <see langword="null"/>, an empty diagnostics collection is used.
    /// </param>
    public LegacyWcfConfigurationReadResult(
        bool success,
        LegacyWcfConfiguration? configuration,
        IReadOnlyList<LegacyWcfDiagnostic>? diagnostics = null)
    {
        Success = success;
        Configuration = configuration;
        Diagnostics = diagnostics ?? Array.Empty<LegacyWcfDiagnostic>();
    }

    /// <summary>
    /// Gets a value indicating whether the read produced a usable configuration.
    /// </summary>
    /// <remarks>
    /// When this value is <see langword="false"/>, <see cref="Configuration"/> is
    /// <see langword="null"/> and <see cref="Diagnostics"/> should be inspected for the reason.
    /// </remarks>
    public bool Success { get; }

    /// <summary>
    /// Gets the configuration produced by the read.
    /// </summary>
    /// <remarks>
    /// This value is <see langword="null"/> when the read fails, including when the file
    /// cannot be read, the XML is malformed, or the required
    /// <c>&lt;system.serviceModel&gt;</c> section is missing.
    /// </remarks>
    public LegacyWcfConfiguration? Configuration { get; }

    /// <summary>
    /// Gets diagnostics that describe read errors, warnings, or informational outcomes.
    /// </summary>
    /// <remarks>
    /// For failed reads, this collection explains why no usable
    /// <see cref="LegacyWcfConfiguration"/> was returned.
    /// </remarks>
    public IReadOnlyList<LegacyWcfDiagnostic> Diagnostics { get; }
}