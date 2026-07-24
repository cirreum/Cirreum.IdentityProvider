# Cirreum.IdentityProvider 2.0.0 — Provisioned Identity Claims

## Why this release exists

A provisioner's job is to decide who gets in and what their token carries. Through 1.x, "what
the token carries" was a single list of **roles** — sufficient when the identity provider
handed over a full user profile for free, but a dead end under a pure IdP-as-a-Service backing
where the **application** owns the user's attributes and the token arrives nearly empty. The
app knew the user's name, tenant, and entitlements; it just had no way to project them into the
issued token.

2.0.0 opens that channel. A provisioner now supplies an app-defined set of identity claims to
mint, with roles as one unprivileged claim among them.

## What's new

**Mint any claim, not just roles.** A provisioner returns an `IProvisionedIdentity` — the
lightweight identity slice (external id + claims), distinct from the full `IApplicationUser` —
whose `Claims` are built with the `IdentityClaim` factories:

```csharp
public sealed record AppIdentity(
    string ExternalUserId,
    IReadOnlyList<string> Roles,
    string TenantId,
    string DisplayName
) : IProvisionedIdentity {
    public IReadOnlyList<IdentityClaim> Claims => [
        IdentityClaim.Roles(Roles),
        IdentityClaim.Name(DisplayName),
        IdentityClaim.Of("tenant", TenantId),
    ];
}
```

**Collision-proof by construction.** Every provisioned claim lives in a `custom*` wire namespace
(`customRoles`, `customName`, `customTenant`, …), so a minted claim can never collide with a
native or reserved identity-provider claim — on any provider, without enumerating each
provider's reserved names. The `IdentityClaim` constructor is private and its factories
guarantee the prefix; a reserved wire-protocol name (`correlationId`) throws at construction.
The client canonicalizes `custom*` back to the native claim, so `custom` never leaks past the
wire.

**The app's type shape is the intent.** There is no `RequireRoles` flag and no framework-shipped
identity type. A required `Roles` constructor parameter makes a roleless user a compile error;
omitting roles (letting `Claims` default to `[]`) is an equally deliberate choice for an
ownership or ABAC app. `ProvisionResult.Allow([])` is a valid admit-with-no-claims — an empty
result is no longer treated as a provisioner bug.

**Observability built in.** Enable the `Cirreum.Identity.Provisioning` OpenTelemetry source and
meter for a span per provisioning callback plus three metrics: `cirreum.identity.provision.duration` (surfaces
the identity-provider callback-deadline risk), `cirreum.identity.provision.count` by outcome, and
`cirreum.identity.provision.claims` — a running measure of how many claims you mint, so token growth is visible
rather than discovered later. No user identifiers or email are tagged.

## Compatibility

Breaking. `IProvisionedUser` becomes `IProvisionedIdentity`, and the provisioning contract no
longer models roles as a first-class list — see [`MIGRATION-v2.md`](MIGRATION-v2.md) for the
find/replace table and a step-by-step walkthrough. For most consumers the change is implementing
`IProvisionedIdentity` with a one-line `Claims` projection. The admit/deny flow, `ProvisionContext`,
the provisioner hook and bases, and registration are unchanged.

The provider adapters (`Cirreum.Identity.EntraExternalId`, `Cirreum.Identity.Oidc`) and their
Runtime Extensions consume this contract and release against `2.0.0`; applications update after
those land.

## See also

- [`MIGRATION-v2.md`](MIGRATION-v2.md) — the upgrade guide.
- [`CHANGELOG.md`](CHANGELOG.md) — the full change list.
