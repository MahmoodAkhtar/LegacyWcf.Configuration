# Changelog

All notable changes to LegacyWcf.Configuration will be documented in this file.

This project should follow a simple changelog format:

- `Added` for new features
- `Changed` for changes in existing behaviour
- `Fixed` for bug fixes
- `Deprecated` for features that will be removed later
- `Removed` for removed features
- `Breaking Changes` for changes that require consumer action

## [Unreleased]

### Added

- Initial project documentation.
- Initial design direction for a full-fidelity legacy WCF `<system.serviceModel>` reader.
- Initial documentation for raw and typed model design.
- Initial documentation for permissive diagnostics.
- Initial documentation for future CoreWCF adapter package boundary.

### Changed

- Nothing yet.

### Fixed

- Nothing yet.

### Breaking Changes

- None.

## [0.1.0] - Unreleased

Planned first release.

### Planned

- Read `app.config`, `web.config`, and external `.config` files.
- Locate `<system.serviceModel>`.
- Preserve the full XML tree under `<system.serviceModel>`.
- Expose a raw model using `LegacyWcfElement`.
- Expose initial typed models for services, endpoints, host, base addresses, bindings, behaviours, client endpoints, and `serviceHostingEnvironment`.
- Provide permissive diagnostics.
- Target `netstandard2.0` and `net8.0`.
