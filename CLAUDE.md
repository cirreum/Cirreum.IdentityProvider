# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is **Cirreum.IdentityProvider**, a .NET 10.0 class library that provides the Core-layer framework for the Cirreum Identity provider family. It defines the shared registrar, configuration types, and provisioning contracts that concrete identity-provider packages (webhook callbacks, Entra External ID, etc.) build upon.

## Build Commands

```bash
# Build the solution
dotnet build Cirreum.IdentityProvider.slnx

# Build specific projects
dotnet build src/Cirreum.IdentityProvider/Cirreum.IdentityProvider.csproj

# Run tests (when test projects are added)
dotnet test

# Create NuGet packages (local release builds use version 1.0.100-rc)
dotnet pack --configuration Release
```

## Architecture

### Two-phase registration

Identity provisioning differs from authorization in that it must register both DI services and HTTP endpoints. The base registrar therefore exposes two phases:

1. **Services phase** (before `builder.Build()`) — `Register(settings, services, configuration)` + derived `RegisterInstanceServices(...)`
2. **Endpoints phase** (after `builder.Build()`) — `MapInstances(settings, endpoints)` + derived `MapInstance(...)`

Provider-specific Runtime Extensions packages (`Cirreum.Runtime.Identity.*`) surface these via app-facing extension methods.

### Core Components

**ProvisioningRegistrar<TSettings, TInstanceSettings>** (`ProvisioningRegistrar.cs`)
- Abstract base for all identity provisioning registrars
- Handles duplicate-registration detection, `Source` auto-population from instance key (with footgun guard), common `Route` validation
- Two abstract methods — `RegisterInstanceServices` (services phase) + `MapInstance` (endpoints phase)

**Configuration types** (root, `namespace Cirreum.Identity`)
- `ProvisioningSettings<TInstanceSettings>` — provider settings container with `Dictionary<string, TInstanceSettings> Instances`
- `ProvisioningInstanceSettings` — base instance settings: `Source`, `Enabled`, `Route`

**Provisioning contracts** (`Provisioning/` folder, `namespace Cirreum.Identity.Provisioning`)
- `IUserProvisioner` — the app's hook into the pre-token callback
- `UserProvisionerBase<TUser>` — standard invitation-redemption base
- `ProvisionContext` — callback payload passed to provisioners
- `ProvisionResult` — `Allowed(roles)` / `Denied` discriminated outcome
- `IProvisionedUser` — constraint on the app's user entity
- `IPendingInvitation` — modeling guide for invitation entities

### Namespace convention

Only **two** namespaces across the Cirreum Identity family:

- `Cirreum.Identity` — config, settings, registrars, runtime extensions (the "framework plumbing" side)
- `Cirreum.Identity.Provisioning` — provisioning contracts (`IUserProvisioner`, `ProvisionContext`, etc.)

Consumer code reads:

```csharp
using Cirreum.Identity;              // settings, registration, Map*()
using Cirreum.Identity.Provisioning; // contracts to implement
```

## Configuration shape

Section path: `Cirreum:Identity:Providers:{ProviderName}:Instances:{Key}`.

The `{ProviderType}` segment ("Identity") is derived from `ProviderType.Identity` via `GetInstanceSectionPath`.

The instance key under `Instances:` serves as both the logical instance name and the `Source` value. `Source` is never set in configuration — it is auto-populated from the key during registration.

## Dependencies

- **Cirreum.Providers** — base provider abstractions (`IProviderRegistrar<,>`, `IProviderSettings<>`, `IProviderInstanceSettings`, `ProviderType`)
- **Microsoft.AspNetCore.App** — ASP.NET Core framework for `IEndpointRouteBuilder`

## Project Structure

```
src/Cirreum.IdentityProvider/    # Main library
├── ProvisioningRegistrar.cs                  # Base registrar (services + endpoints phases)
├── ProvisioningSettings.cs                   # Base settings container
├── ProvisioningInstanceSettings.cs           # Base instance settings (Source/Enabled/Route)
└── Provisioning/                             # Provisioning contracts
    ├── IUserProvisioner.cs
    ├── UserProvisionerBase.cs
    ├── ProvisionContext.cs
    ├── ProvisionResult.cs
    ├── IProvisionedUser.cs
    └── IPendingInvitation.cs
```

## Development Notes

- Uses .NET 10.0 with latest C# language version
- Nullable reference types enabled
- CI/CD aware build configuration (detects Azure DevOps, GitHub Actions)
- Currently contains only the main library project (no test projects yet)
- File-scoped namespaces throughout
- K&R braces, tabs for indentation (matches repo `.editorconfig`)
