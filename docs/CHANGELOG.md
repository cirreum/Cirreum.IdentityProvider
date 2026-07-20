# Cirreum.IdentityProvider Changelog

All notable changes to **Cirreum.IdentityProvider** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.8] - 2026-07-20

### Fixed

- The registrar's duplicate-instance-key guard was process-global static state: a
  second host composed in the same process (the integration-test norm) re-registering
  the same instance key threw "already been registered" even though its own
  composition was clean. Guard state now lives in the service collection, so hosts
  are fully isolated (ADR-0028 principle; rider on the ADR-0030/0031 wave).

## [1.0.7] - 2026-07-18

### Updated

- Updated NuGet packages.

## [1.0.6] - 2026-07-04

### Updated

- Updated NuGet packages.

## [1.0.5] - 2026-06-05

### Fixed

- Updated dependencies to their latest versions to pick up upstream fixes:
  - `Cirreum.Providers` `1.1.1` → `1.2.1`

## [1.0.4]

Baseline release. Prior history predates this changelog.
