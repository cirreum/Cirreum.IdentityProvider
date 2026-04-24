# Cirreum Identity Provider

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.IdentityProvider.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.IdentityProvider/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.IdentityProvider.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.IdentityProvider/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.IdentityProvider?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.IdentityProvider/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.IdentityProvider?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.IdentityProvider/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Identity-provider framework for Cirreum — the Core-layer library shared by all Cirreum identity-provider integrations.**

## Overview

`Cirreum.IdentityProvider` is the Core-layer library for the Cirreum Identity provider family. It defines the shared registrar, configuration types, and user-provisioning contracts that concrete identity-provider packages (OIDC webhook callbacks, Entra External ID claims issuance, etc.) build upon.

Apps do **not** reference this package directly — they install a Runtime Extensions package such as `Cirreum.Runtime.Identity.Oidc`, `Cirreum.Runtime.Identity.EntraExternalId`, or the umbrella `Cirreum.Runtime.Identity`. This package flows in transitively.

## Provider pattern

All Cirreum Identity providers share a common shape:

- The IdP calls back into the app during sign-in with a payload describing the authenticating user.
- The app's `IUserProvisioner` decides whether the user is allowed in and what roles to embed in the issued token.
- A concrete provisioning provider (e.g. OIDC webhook, Entra External ID) wires up the HTTP endpoint, validates the inbound request, builds a `ProvisionContext`, resolves the keyed provisioner, and returns the appropriate response format for that IdP.

## Two-phase registration

Identity providers must register both DI services and HTTP endpoints. The base `IdentityProviderRegistrar<,>` therefore exposes two phases:

1. **Services phase** (before `builder.Build()`) — `Register(providerSettings, services, configuration)` validates settings, guards against duplicate registration, auto-populates `Source` from the instance key, and delegates instance-specific DI registration to `RegisterProvisioner`.
2. **Endpoints phase** (after `builder.Build()`) — `Map(providerSettings, endpoints)` walks enabled instances and delegates per-instance endpoint mapping to `MapProvisioner`.

The provider-specific Runtime Extensions package (`Cirreum.Runtime.Identity.*`) surfaces these via app-facing extension methods — ultimately composing into `builder.AddIdentity()` / `app.MapIdentity()` at the umbrella level.

## Configuration shape

```json
{
  "Cirreum": {
    "Identity": {
      "Providers": {
        "Oidc": {
          "Instances": {
            "clientA_descope": {
              "Enabled": true,
              "Route": "/auth/clientA/provision",
              "...": "provider-specific settings"
            },
            "clientB_descope": {
              "Enabled": true,
              "Route": "/auth/clientB/provision",
              "...": "provider-specific settings"
            }
          }
        },
        "EntraExternalId": {
          "Instances": {
            "primary": {
              "Enabled": true,
              "Route": "/auth/entra/provision",
              "...": "provider-specific settings"
            }
          }
        }
      }
    }
  }
}
```

### Instance key = Source name

The instance key under `Instances:` serves double duty: it is both the logical instance name and the `Source` value stamped into `ProvisionContext` by the callback handler. The key is also used as the keyed DI key under which `IUserProvisioner` is registered, so multi-instance / multi-IdP apps can register one provisioner per source and have the correct one resolved automatically.

> **Do not set `Source` in configuration.** It is auto-populated from the instance key during registration. If a mismatched value is detected, registration fails with an `InvalidOperationException` rather than silently overwriting.

## Key types

### Registrar (`namespace Cirreum.Identity`)

| Type | Purpose |
|---|---|
| `IdentityProviderRegistrar<TSettings, TInstanceSettings>` | Abstract base for all identity provider registrars. Two-phase `Register` + `Map`. Concrete Infrastructure packages implement `RegisterProvisioner` and `MapProvisioner`. |

### Configuration (`namespace Cirreum.Identity.Configuration`)

| Type | Purpose |
|---|---|
| `IdentityProviderSettings<TInstanceSettings>` | Base settings container — `Dictionary<string, TInstanceSettings> Instances`. |
| `IdentityProviderInstanceSettings` | Base instance settings — `Source`, `Enabled`, `Route`. |

### Provisioning contracts (`namespace Cirreum.Identity.Provisioning`)

| Type | Purpose |
|---|---|
| `IUserProvisioner` | The app's hook into the pre-token callback. Returns `ProvisionResult.Allow(...)` or `ProvisionResult.Deny()`. |
| `UserProvisionerBase<TUser>` | Abstract generic base — orchestrates returning-user lookup, delegates onboarding to `ProvisionNewUserAsync`. Use directly for custom / hybrid onboarding. |
| `InvitationUserProvisionerBase<TUser>` | Specialization for invitation-based onboarding. Abstract `RedeemInvitationAsync`. |
| `SelfServiceUserProvisionerBase<TUser>` | Specialization for self-service onboarding. Abstract `CreateSelfServiceUserAsync`. |
| `ProvisionContext` | Callback payload: `Source`, `ExternalUserId`, `CorrelationId`, `ClientAppId`, `Email`. |
| `ProvisionResult` | Discriminated outcome — `Allowed(roles)` or `Denied`. |
| `IProvisionedUser` | Constraint on the app's user entity — exposes `ExternalUserId` + `Roles`. |
| `IPendingInvitation` | Modeling guide for invitation entities (used by invitation-based provisioners). |

## Implementing a provisioner

Consumer apps implement `IUserProvisioner` via the base that matches the onboarding model for a given instance. One concrete class per configured instance.

### Invitation-based

```csharp
using Cirreum.Identity.Provisioning;

public sealed class ClientBBorrowerProvisioner(AppDbContext db)
    : InvitationUserProvisionerBase<BorrowerUser> {

    protected override Task<BorrowerUser?> FindUserAsync(
        string externalUserId, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.ExternalUserId == externalUserId, ct);

    protected override async Task<BorrowerUser?> RedeemInvitationAsync(
        string email, string externalUserId, CancellationToken ct) {
        // atomically find, validate, and claim the invitation; create user record
        // (implementation-specific — see IPendingInvitation for the expected shape)
    }
}
```

### Self-service

```csharp
using Cirreum.Identity.Provisioning;

public sealed class ClientABorrowerProvisioner(AppDbContext db)
    : SelfServiceUserProvisionerBase<BorrowerUser> {

    protected override Task<BorrowerUser?> FindUserAsync(
        string externalUserId, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.ExternalUserId == externalUserId, ct);

    protected override async Task<BorrowerUser?> CreateSelfServiceUserAsync(
        ProvisionContext context, CancellationToken ct) {
        var user = new BorrowerUser {
            ExternalUserId = context.ExternalUserId,
            Email = context.Email,
            Roles = [BorrowerRoles.Default],
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }
}
```

### Custom / hybrid orchestration

For applications that don't fit either preset (e.g. try invitation first, fall back to self-service), derive directly from `UserProvisionerBase<TUser>` and implement both `FindUserAsync` and `ProvisionNewUserAsync`:

```csharp
using Cirreum.Identity.Provisioning;

public sealed class HybridProvisioner(AppDbContext db, IInvitationService invitations)
    : UserProvisionerBase<AppUser> {

    protected override Task<AppUser?> FindUserAsync(
        string externalUserId, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.ExternalUserId == externalUserId, ct);

    protected override async Task<ProvisionResult> ProvisionNewUserAsync(
        ProvisionContext context, CancellationToken ct) {
        // Try invitation first
        var invited = await invitations.TryClaimAsync(context.Email, context.ExternalUserId, ct);
        if (invited is not null) {
            return ProvisionResult.Allow(invited.Roles);
        }

        // Fall through to self-service with default role
        var user = await this.CreateDefaultUserAsync(context, ct);
        return user is not null
            ? ProvisionResult.Allow(user.Roles)
            : ProvisionResult.Deny();
    }

    // ...
}
```

### Registering provisioners against instances

Via the Runtime Extensions layer (`Cirreum.Runtime.Identity` or per-protocol packages):

```csharp
// Program.cs
builder.AddIdentity()
    .AddProvisioner<ClientABorrowerProvisioner>("clientA_descope")
    .AddProvisioner<ClientBBorrowerProvisioner>("clientB_descope")
    .AddProvisioner<WorkforceProvisioner>("entraExternalId_primary");

var app = builder.Build();
app.MapIdentity();
```

Each `AddProvisioner<T>(instanceKey)` registers `T` as a keyed `IUserProvisioner` scoped service under `instanceKey`. At callback time the infra handler resolves the keyed provisioner whose key matches `ProvisionContext.Source`. Startup validation confirms every enabled instance in configuration has a registered provisioner.

## Implementing a new identity-provider integration

Identity-provider implementations are separate Infrastructure packages (e.g. `Cirreum.Identity.Oidc`, `Cirreum.Identity.EntraExternalId`) that inherit `IdentityProviderRegistrar<TSettings, TInstanceSettings>`:

```csharp
using Cirreum.Identity;
using Cirreum.Identity.Configuration;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class MyIdpIdentityProviderRegistrar
    : IdentityProviderRegistrar<MyIdpSettings, MyIdpInstanceSettings> {

    public override string ProviderName => "MyIdp";

    protected override void RegisterProvisioner(
        string key,
        MyIdpInstanceSettings settings,
        IServiceCollection services,
        IConfiguration configuration) {
        // Bind settings, register provider-specific collaborators
        // (request validators, shared-secret verifiers, etc.).
        // The app's IUserProvisioner is registered separately as a keyed
        // service via AddProvisioner<T>(key) in the Runtime Extensions layer.
    }

    protected override void MapProvisioner(
        string key,
        MyIdpInstanceSettings settings,
        IEndpointRouteBuilder endpoints) {
        endpoints.MapPost(settings.Route, async (HttpContext ctx /*, ...*/) => {
            // Parse inbound request, authenticate it, build ProvisionContext,
            // resolve the keyed IUserProvisioner by Source, invoke ProvisionAsync,
            // translate the ProvisionResult into the IdP's expected response format.
        });
    }
}
```

## Contribution Guidelines

1. **Be conservative with new abstractions** — the API surface must remain stable and meaningful.
2. **Limit dependency expansion** — only add foundational, version-stable dependencies.
3. **Favor additive, non-breaking changes** — breaking changes ripple through the entire ecosystem.
4. **Include thorough unit tests** — all primitives and patterns should be independently testable.
5. **Document architectural decisions** — context and reasoning should be clear for future maintainers.
6. **Follow .NET conventions** — use established patterns from `Microsoft.Extensions.*` libraries.

## Versioning

Follows [Semantic Versioning](https://semver.org/). Given its foundational role, major bumps are rare and carefully considered.

## License

MIT — see [LICENSE](LICENSE).

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*
