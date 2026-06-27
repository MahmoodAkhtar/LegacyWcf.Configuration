# Changelog

All notable changes to LegacyWcf.Configuration will be documented in this file.

This project follows a simple changelog format:

- Added for new features
- Changed for changes in existing behaviour
- Fixed for bug fixes
- Deprecated for features that will be removed later
- Removed for removed features
- Breaking Changes for changes that require consumer action

## [Unreleased]

### Added

- Nothing yet.

### Changed

- Nothing yet.

### Fixed

- Nothing yet.

### Breaking Changes

- None.

## [0.2.0] - 2026-06-27

### Added

- Added typed WCF service models through `LegacyWcfService`, `LegacyWcfServices`, and `LegacyWcfConfiguration.Services`.
- Added typed WCF service endpoint models through `LegacyWcfServiceEndpoint` and `LegacyWcfServiceEndpoints`.
- Added typed service host support through `LegacyWcfHost` and `LegacyWcfService.Host`.
- Added typed host base address support through `LegacyWcfHost.BaseAddresses`.
- Added typed host timeout support through `LegacyWcfHostTimeouts`.
- Added initial typed binding support through `LegacyWcfBinding`, `LegacyWcfBindingCollection`, `LegacyWcfBindings`, and `LegacyWcfConfiguration.Bindings`.
- Added typed binding groups for `basicHttpBinding`, `wsHttpBinding`, `netTcpBinding`, and `customBinding`.
- Added initial typed behaviour support through `LegacyWcfBehavior`, `LegacyWcfBehaviorCollection`, `LegacyWcfBehaviors`, and `LegacyWcfConfiguration.Behaviors`.
- Added typed service behaviour and endpoint behaviour collections.
- Added support for British legacy/custom behaviour spelling aliases: `<behaviours>`, `<serviceBehaviours>`, `<endpointBehaviours>`, and `<behaviour>`.
- Added typed client endpoint support through `LegacyWcfClient`, `LegacyWcfClientEndpoint`, `LegacyWcfClientEndpoints`, and `LegacyWcfConfiguration.Client`.
- Added typed `serviceHostingEnvironment` support through `LegacyWcfServiceHostingEnvironment` and `LegacyWcfConfiguration.ServiceHostingEnvironment`.
- Added preservation of source attributes for typed bindings, behaviours, client endpoints, and `serviceHostingEnvironment` models.
- Added raw XML fallback from every Phase 2 typed model object through `RawElement`.
- Added tests covering Phase 2 typed services, endpoints, hosts, bindings, behaviours, client endpoints, and `serviceHostingEnvironment` support.

### Changed

- Expanded the library from a Phase 1 raw `<system.serviceModel>` reader into a Phase 2 typed WCF configuration model while keeping raw XML preservation as the source of truth.
- Updated documentation to describe the completed Phase 2 typed model and the next planned phase: retrieval APIs.

### Fixed

- Nothing yet.

### Breaking Changes

- None.

## [0.1.0] - 2026-06-25

### Added

- Added the initial `LegacyWcfConfigurationReader.Read(string filePath)` API.
- Added `LegacyWcfConfigurationReadResult` with `Success`, `Configuration`, and `Diagnostics`.
- Added `LegacyWcfConfiguration` with `RawSystemServiceModel`.
- Added `LegacyWcfElement` as the raw XML model for preserved WCF configuration elements.
- Added `LegacyWcfDiagnostic` and `LegacyWcfDiagnosticSeverity`.
- Added recursive raw XML preservation for `<system.serviceModel>` descendants.
- Preserved element names, paths, attributes, children, values, raw XML, source file paths, and line numbers where practical.
- Preserved unknown custom elements and attributes.
- Added diagnostics for missing file paths, missing files, unreadable files, malformed XML, missing `<configuration>`, and missing `<system.serviceModel>`.
- Added initial project documentation, usage guidance, architecture notes, configuration specification, roadmap, and AI handover context.
- Added NuGet package metadata for `LegacyWcf.Configuration` version `0.1.0`.
- Added Apache 2.0 package license metadata using the `Apache-2.0` SPDX expression.
- Added NuGet package README support using the root `README.md`.
- Added XML documentation file generation for package builds.
- Added symbol package generation using the `.snupkg` format.
- Added GitHub Actions publishing for version tags matching `v*.*.*`.
- Added NuGet Trusted Publishing support for GitHub Actions publishing without a long-lived NuGet API key.

### Changed

- Nothing yet.

### Fixed

- Nothing yet.

### Breaking Changes

- None.
