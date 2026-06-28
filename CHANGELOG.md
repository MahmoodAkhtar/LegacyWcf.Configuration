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

## [0.4.0] - 2026-06-28

### Added

- Added Phase 4 permissive validation diagnostics for successful reads.
- Added informational diagnostic `LWC1001` for unknown or unsupported WCF configuration elements preserved in the raw XML model.
- Added warning diagnostic `LWC1002` for duplicate non-blank service names.
- Added warning diagnostic `LWC1003` for duplicate non-blank binding names within the same binding type.
- Added warning diagnostic `LWC1004` for duplicate non-blank service behaviour names.
- Added warning diagnostic `LWC1005` for duplicate non-blank endpoint behaviour names.
- Added warning diagnostic `LWC1006` for duplicate direct `serviceHostingEnvironment` elements.
- Added warning diagnostic `LWC1007` for service or client endpoints that reference a missing binding configuration.
- Added warning diagnostic `LWC1008` for service or client endpoints that reference a missing endpoint behaviour configuration.
- Added warning diagnostic `LWC1009` for services that reference a missing service behaviour configuration.
- Added internal `LegacyWcfConfigurationValidator` post-processing over the preserved raw XML tree and typed model.
- Added tests covering duplicate services, duplicate bindings, duplicate behaviours, unresolved binding references, unresolved behaviour references, duplicate `serviceHostingEnvironment`, and unknown raw element diagnostics.

### Changed

- Expanded diagnostics from read-failure diagnostics into permissive validation diagnostics for successful reads.
- Successful reads can now include warning or informational diagnostics in both `LegacyWcfConfigurationReadResult.Diagnostics` and `LegacyWcfConfiguration.Diagnostics`.
- Kept Phase 4 additive: raw XML preservation, typed model enumeration, lookup helper signatures, and duplicate lookup behaviour remain unchanged.
- Updated documentation to describe Phase 4 validation diagnostics as implemented and `v0.4.0` as the current release context.

### Fixed

- Nothing yet.

### Breaking Changes

- None.

## [0.3.0] - 2026-06-27

### Added

- Added Phase 3 retrieval APIs for typed WCF services through `LegacyWcfServices.Find(string name)` and `LegacyWcfServices.GetRequired(string name)`.
- Added Phase 3 retrieval APIs for service endpoints through `LegacyWcfServiceEndpoints.FindByName(string name)`, `LegacyWcfServiceEndpoints.GetRequiredByName(string name)`, `LegacyWcfServiceEndpoints.FindByContract(string contract)`, and `LegacyWcfServiceEndpoints.GetRequiredByContract(string contract)`.
- Added Phase 3 retrieval APIs for typed binding collections through `LegacyWcfBindingCollection.Find(string? name)` and `LegacyWcfBindingCollection.GetRequired(string? name)`.
- Added Phase 3 top-level binding lookup through `LegacyWcfBindings.Find(string? bindingType, string? name)` and `LegacyWcfBindings.GetRequired(string? bindingType, string? name)`.
- Added Phase 3 retrieval APIs for typed behaviour collections through `LegacyWcfBehaviorCollection.Find(string? name)` and `LegacyWcfBehaviorCollection.GetRequired(string? name)`.
- Added Phase 3 retrieval APIs for client endpoints through `LegacyWcfClientEndpoints.FindByName(string name)`, `LegacyWcfClientEndpoints.GetRequiredByName(string name)`, `LegacyWcfClientEndpoints.FindByContract(string contract)`, and `LegacyWcfClientEndpoints.GetRequiredByContract(string contract)`.
- Added case-insensitive matching for WCF names and identifiers in Phase 3 lookup helpers.
- Added required lookup failure behaviour using clear `InvalidOperationException` messages when `GetRequired...` helpers cannot find a matching item.
- Added support for `null` lookup names in binding and behaviour collection lookups so unnamed bindings and behaviours can be retrieved.
- Added tests covering Phase 3 service, service endpoint, binding, behaviour, and client endpoint lookup helpers, including missing lookup values and required-lookup exception messages.

### Changed

- Expanded the library from a Phase 2 typed WCF configuration model into a queryable typed model with targeted retrieval helpers.
- Kept Phase 3 as an additive API convenience phase: XML parsing, raw XML preservation, diagnostics, and CoreWCF package boundaries remain unchanged.
- Left duplicate detection, missing binding reference diagnostics, missing behaviour reference diagnostics, and broader validation concerns for Phase 4.
- Updated documentation to describe Phase 3 retrieval APIs as implemented and Phase 4 validation and diagnostics as the next planned phase.

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
