# Cirreum Identity Provider

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.IdentityProvider.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.IdentityProvider/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.IdentityProvider.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.IdentityProvider/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.IdentityProvider?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.IdentityProvider/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.IdentityProvider?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.IdentityProvider/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Identity-provider framework for Cirreum — the Core-layer library shared by all Cirreum identity-provider integrations.**

## Overview

`Cirreum.IdentityProvider` is the Core-layer library for the Cirreum Identity provider family. It defines the shared registrar, configuration types, and provisioning contracts that concrete identity-provider packages (webhook callbacks, Entra External ID, etc.) build upon.

Apps do **not** reference this package directly — they install a Runtime Extensions package such as `Cirreum.Runtime.Identity.Webhook`, `Cirreum.Runtime.Identity.EntraExternalId`, or the umbrella `Cirreum.Runtime.Identity`. This package flows in transitively.

## Provider pattern

All identity-provisioning providers in the Cirreum family share a common shape:

- The IdP calls back into the app during sign-in with a payload describing the authenticating user.
- The app's `IUserProvisioner` decides whether the user is allowed in and what roles to embed in the issued token.
- A concrete provisioning provider (e.g. Webhook, Entra External ID) wires up the HTTP endpoint, validates the inbound request, builds a `ProvisionContext`, invokes the provisioner, and returns the appropriate response format for that IdP.

## Two-phase registration

Identity provisioning registrars differ from authorization registrars in that they must register both DI services and HTTP endpoints. The base `ProvisioningRegistrar<,>` therefore exposes two phases:

1. **Services phase** (before `builder.Build()`) — `Register(settings, services, configuration)` validates settings, guards against duplicate registration, auto-populates `Source` from the instance key, and delegates instance-specific DI registration to `RegisterInstanceServices`.
2. **Endpoints phase** (after `builder.Build()`) — `MapInstances(settings, endpoints)` walks enabled instances and delegates instance-specific endpoint mapping to `MapInstance`.

The provider-specific Runtime Extensions package (`Cirreum.Runtime.Identity.*`) surfaces these via app-facing extension methods like `AddWebhookProvisioning<T>()` and `MapWebhookProvisioning()`.

## Configuration shape

```json
{
  "Cirreum": {
    "Identity": {
      "Providers": {
        "Webhook": {
          "Instances": {
            "Descope": {
              "Enabled": true,
              "Route": "/auth/descope/provision",
              "...": "provider-specific settings"
            }
          }
        },
        "EntraExternalId": {
          "Instances": {
            "primary": {
              "Enabled": true,
              "Route": "/auth/entra/claims",
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

The instance key under `Instances:` serves double duty: it is both the logical instance name and the `Source` value stamped into `ProvisionContext` by the callback handler. The key is also used as the keyed DI key under which `IUserProvisioner` is registered, so multi-IdP apps can register one provisioner per source and have the correct one resolved automatically.

> **Do not set `Source` in configuration.** It is auto-populated from the instance key during registration. If a mismatched value is detected, registration fails with an `InvalidOperationException` rather than silently overwriting.

## Key types

### Configuration (`namespace Cirreum.Identity`)

| Type | Purpose |
|---|---|
| `ProvisioningRegistrar<TSettings, TInstanceSettings>` | Abstract base for all identity provisioning registrars. Two-phase `Register` + `MapInstances`. |
| `ProvisioningSettings<TInstanceSettings>` | Base settings container — `Dictionary<string, TInstanceSettings> Instances`. |
| `ProvisioningInstanceSettings` | Base instance settings — `Source`, `Enabled`, `Route`. |

### Provisioning contracts (`namespace Cirreum.Identity.Provisioning`)

| Type | Purpose |
|---|---|
| `IUserProvisioner` | The app's hook into the pre-token callback. Returns `ProvisionResult.Allow(...)` or `ProvisionResult.Deny()`. |
| `UserProvisionerBase<TUser>` | Abstract base implementing the standard invitation-redemption flow. |
| `ProvisionContext` | Callback payload: `Source`, `ExternalUserId`, `CorrelationId`, `ClientAppId`, `Email`. |
| `ProvisionResult` | Discriminated outcome — `Allowed(roles)` or `Denied`. |
| `IProvisionedUser` | Constraint on the app's user entity — exposes `ExternalUserId` + `Roles`. |
| `IPendingInvitation` | Modeling guide for invitation entities. |

## Implementing a provisioner

Consumer apps implement `IUserProvisioner` (directly, or via `UserProvisionerBase<TUser>`) and register it through the provider-specific Runtime Extensions package:

```csharp
using Cirreum.Identity;
using Cirreum.Identity.Provisioning;

public sealed class BorrowerProvisioner(AppDbContext db) : UserProvisionerBase<AppUser> {
    protected override Task<AppUser?> FindUserAsync(string externalUserId, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.ExternalUserId == externalUserId, ct);

    protected override async Task<AppUser?> RedeemInvitationAsync(
        string email, string externalUserId, CancellationToken ct) {
        // atomically find, validate, and claim invitation; create user record
        // ...
    }
}

// In Program.cs:
builder.AddWebhookProvisioning<BorrowerProvisioner>();
var app = builder.Build();
app.MapWebhookProvisioning();
```

## Implementing a new identity-provider integration

Identity-provider implementations are separate Infrastructure packages (e.g. `Cirreum.Identity.Webhook`, `Cirreum.Identity.EntraExternalId`) that inherit `ProvisioningRegistrar<TSettings, TInstanceSettings>`:

```csharp
public sealed class MyIdpProvisioningRegistrar
    : ProvisioningRegistrar<MyIdpProvisioningSettings, MyIdpInstanceSettings> {

    public override string ProviderName => "MyIdp";

    protected override void RegisterInstanceServices(
        string key, MyIdpInstanceSettings settings,
        IServiceCollection services, IConfiguration configuration) {
        // Register the app's IUserProvisioner as a keyed service under the instance key.
        // Register provider-specific collaborators (request validators, etc.).
    }

    protected override void MapInstance(
        string key, MyIdpInstanceSettings settings,
        IEndpointRouteBuilder endpoints) {
        // endpoints.MapPost(settings.Route, async (HttpContext ctx, ...) => { ... });
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
