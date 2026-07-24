# Migrating to Cirreum.IdentityProvider 2.0.0 (from 1.x)

## Why v2

Through 1.x, a provisioner could inject exactly one thing into the issued token: **roles**.
That was enough when the identity provider federated a full user profile for free, but breaks
under a pure IdP-as-a-Service backing (Descope, generic OIDC, Entra External ID) where the
**application** is the authority for the user's attributes and the token may carry little more
than a subject id. The app holds the user's name, tenant, entitlements, and flags in its own
store, with no channel to project them into the token.

v2 widens that channel. A provisioner returns an app-defined set of **identity claims** to mint,
with roles folded in as one claim among them. Every provisioned claim lives in a `custom*` wire
namespace (`customRoles`, `customName`, `customTenant`, …) that cannot collide with a native or
reserved identity-provider claim, on any provider.

## Breaking Changes — Find / Replace Table

| 1.x | 2.0.0 |
|---|---|
| `IProvisionedUser` (interface) | `IProvisionedIdentity` |
| `IProvisionedUser.Roles` (property) | `IProvisionedIdentity.Claims` (returns `IReadOnlyList<IdentityClaim>`, defaults to `[]`) |
| `class AppUser : IProvisionedUser { … IReadOnlyList<string> Roles … }` | `record AppIdentity(…) : IProvisionedIdentity { public IReadOnlyList<IdentityClaim> Claims => [IdentityClaim.Roles(Roles)]; }` |
| `ProvisionResult.Allow(roles)` / `ProvisionResult.Allow("Admin")` | `ProvisionResult.Allow([IdentityClaim.Roles(roles)])` |
| `ProvisionResult.Allowed { Roles }` (pattern match) | `ProvisionResult.Allowed { Claims }` |
| `ProvisionResult.Allow(...)` with no roles was a bug | `ProvisionResult.Allow([])` is a valid admit-with-no-claims |

## New Capabilities

- **`IdentityClaim`** — build claims with `IdentityClaim.Roles(...)`, `IdentityClaim.Name(...)`,
  or `IdentityClaim.Of("tenant", value)` (arbitrary claims; the `custom` wire prefix is applied
  for you, idempotently). A reserved wire-protocol name (`correlationId`) throws at construction.
- **`Claims` as intent** — a required `Roles` constructor parameter on your provisioned-identity
  type makes a roleless user a compile error; omitting roles (letting `Claims` default to `[]`)
  is an equally deliberate choice for ownership / ABAC apps. No framework flag decides it.
- **`ProvisioningTelemetry`** — enable the `Cirreum.Identity.Provisioning` OpenTelemetry source
  and meter to observe provisioning: a span per callback plus duration, outcome, and
  minted-claim-count metrics.

## Migration Walkthrough

1. **Implement `IProvisionedIdentity`** (was `IProvisionedUser`) on the type you return from your
   provisioner — often a lightweight projection of your user entity, kept distinct from the full
   `IApplicationUser`. Keep whatever fields you already store (including a `Roles` field), and add
   a `Claims` projection:

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

2. **If you implement `IUserProvisioner` directly** (not via a base class), replace
   `ProvisionResult.Allow(roles)` with `ProvisionResult.Allow([IdentityClaim.Roles(roles)])`,
   and pattern-match `Allowed { Claims }` rather than `Allowed { Roles }`.

3. **If you derive from `UserProvisionerBase` / `SelfServiceUserProvisionerBase` /
   `InvitationUserProvisionerBase`,** there is nothing to change beyond step 1 — the bases now
   read `Claims` from the identity you return.

4. **Mint more than roles** by adding `IdentityClaim` entries to `Claims`. Keep the set small
   and authorization-relevant; profile data the identity provider already holds is better
   selected natively at the provider than minted here.

5. **Coordinate the provider-side change.** The claims travel under `custom*` wire names. The
   provider adapters (`Cirreum.Identity.EntraExternalId`, `Cirreum.Identity.Oidc`) and their
   Runtime Extensions carry the wire and client changes, and release against this version;
   update your identity-provider configuration when those land (see their release notes).

## What Didn't Change

- The admit / deny decision (`ProvisionResult.Allow` / `Deny`) and the sole-authority model —
  authenticating against the identity provider still does not grant access; your provisioner
  decides.
- `ProvisionContext` and the token-issuance callback payload.
- The provisioner hook and bases (`IUserProvisioner`, `UserProvisionerBase`,
  `SelfServiceUserProvisionerBase`, `InvitationUserProvisionerBase`) and their method shapes
  (`FindUserAsync`, `ProvisionNewUserAsync`, `RedeemInvitationAsync`, `CreateSelfServiceUserAsync`).
- Registration and configuration (`Cirreum:Identity:Providers:…:Instances:…`).

## Downstream Package Impact

The provider adapters and their Runtime Extensions consume this contract and release against
`2.0.0`:

- `Cirreum.Identity.EntraExternalId`, `Cirreum.Identity.Oidc` (Infrastructure)
- `Cirreum.Runtime.IdentityProvider`, `Cirreum.Runtime.Identity(.EntraExternalId / .Oidc)`
  (Runtime / Runtime Extensions)

Applications update after those packages are published. Client-side, the `custom*` claims are
canonicalized to their native forms (`customRoles → roles`, `customName → name`) by the WASM
runtime's claims extender, so `UserProfile` and role-based authorization read them unchanged.
