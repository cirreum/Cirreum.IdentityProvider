# Backlog

Deferred work for **Cirreum.IdentityProvider**. Items here are tracked but not yet ready
to ship — either because the cost outweighs the benefit in isolation, or
because they're waiting on a forcing function (a related change, a consumer
upgrade, a coordinated multi-repo rollout).

## How this file works

- Each item is a `###` heading so it can be linked to and parsed.
- Each item declares **`SemVer:`** (`Patch` | `Minor` | `Major` | `Unspecified`),
  **`Trigger:`** (the human-readable condition that will make it ready), and
  **`Noted:`** (the date the item was added).
- The Cirreum DevOps release scripts (`PatchRelease`, `MinorRelease`,
  `MajorRelease`) surface items at-or-below the requested bump level so the
  operator can decide whether to fold them in before tagging.
- Items that ship: move from this file to `docs/CHANGELOG.md` under
  `[Unreleased]`. Items that grow into design discussions: promote to an ADR.

## Queued

### State `ActivityKind` explicitly on the provisioning activity

- **SemVer:** Patch
- **Trigger:** The next release of this package for any other reason — it is a one-word change with no behavior difference, not worth a release of its own.
- **Noted:** 2026-07-27

`ProvisioningTrace` calls `ProvisioningTelemetry.ActivitySource.StartActivity("identity.provision")`
without a kind, taking the `ActivityKind.Internal` default.

`Internal` is the **correct** kind. The provisioning callback is an inbound call from the IdP, which
makes `Server` tempting — but that span belongs to the ASP.NET endpoint that received it, and
`ProvisioningTrace` wraps the admit/deny and claim-mint decision *inside* that request. It neither
receives work nor sends it, so it is never the span where work arrives and the host-dependent
`DomainContext.EntryPointActivityKind` would be wrong here. Nothing is mislabeled today.

The issue is that the choice is not recorded — and on this track the correct answer is genuinely
non-obvious, which makes stating it worth more here than elsewhere. `DomainContext` asks a track to
pass the kind explicitly *even when it is Internal*, precisely because the framework does vary it:
`Client` for outbound HTTP, `Producer` for broker publishes, `EntryPointActivityKind` for Conductor
dispatch. A silent default reads as "nobody considered it".

```csharp
// Before
this._activity = ProvisioningTelemetry.ActivitySource.StartActivity("identity.provision");

// After
this._activity = ProvisioningTelemetry.ActivitySource.StartActivity(
    "identity.provision", ActivityKind.Internal);
```

`Cirreum.Runtime.AuthenticationProvider` 2.0.0 made the same change to the authentication track's
transformation activity. `AuthorizationTelemetry` in `Cirreum.Contracts` carries the identical item.
