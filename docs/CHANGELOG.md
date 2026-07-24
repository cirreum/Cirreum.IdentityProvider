# Cirreum.IdentityProvider Changelog

All notable changes to **Cirreum.IdentityProvider** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Provisioning widens beyond roles: an application now supplies an arbitrary set of identity
claims to mint into the issued token, with roles folded in as one claim among them. Every
provisioned claim lives in a `custom*` wire namespace that cannot collide with a native
identity-provider claim. This is a breaking reshape of the provisioning contract — see
`MIGRATION-v2.md` (ADR-0033).

### Added

- `IdentityClaim` — one constituent of a provisioned identity (a `custom*` wire name plus
  one-or-more values), built via the factories `IdentityClaim.Roles(...)`,
  `IdentityClaim.Name(...)`, and `IdentityClaim.Of(name, ...)`. The constructor is private, so
  every claim is guaranteed to carry a `custom*` wire name; `Of` prefixes idempotently, and a
  reserved wire-protocol name (`correlationId`) throws at construction.
- `CustomClaimNames` — the `custom*` wire prefix, the reserved `customRoles` / `customName`
  wire names, and the reserved-protocol-name set.
- `IProvisionedIdentity` — the lightweight identity an application provisions (`ExternalUserId`
  + `Claims`), distinct from `IApplicationUser` (the full db-backed user). Replaces
  `IProvisionedUser`.
- `IdentityClaimExtensions.ToClaimMap` — projects an `IdentityClaim` set to a canonical
  name→values map, merging (union, distinct) any repeated claim type. Shared by the provider
  adapters.
- `ProvisioningTelemetry` / `ProvisioningTrace` — an OpenTelemetry surface for the
  provisioning callback (one `ActivitySource` + `Meter` named `Cirreum.Identity.Provisioning`):
  a span per callback plus duration, outcome-count, and minted-claim-count metrics. No user
  identifiers or email are tagged.

### Changed

- `ProvisionResult.Allowed` now carries `IReadOnlyList<IdentityClaim> Claims`, and the single
  factory is `ProvisionResult.Allow(IReadOnlyList<IdentityClaim>)`. `Allow([])` is a valid
  admit-with-no-claims outcome — an empty result is no longer treated as a provisioner error.
- `UserProvisionerBase`, `SelfServiceUserProvisionerBase`, and `InvitationUserProvisionerBase`
  read `IProvisionedIdentity.Claims`.

### Removed

- `IProvisionedUser` (renamed to `IProvisionedIdentity`) and its `Roles` member.
- `ProvisionResult.Allow(params string[])` and `ProvisionResult.Allow(IReadOnlyList<string>)`,
  and the `Allowed(IReadOnlyList<string> Roles)` shape.

### Migration

- The provisioning contract no longer models roles as a first-class list; implement
  `IProvisionedIdentity` (was `IProvisionedUser`) and project roles (and any other claims)
  through `Claims` via `IdentityClaim.Roles(...)`. Whether a user must carry roles is expressed
  by the shape of your own provisioned-identity type, not enforced by the framework. Step-by-step
  in `MIGRATION-v2.md`.

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
