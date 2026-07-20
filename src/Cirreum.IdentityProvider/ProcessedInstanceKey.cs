namespace Cirreum.Identity;

/// <summary>
/// Service-collection-scoped marker recording that a provisioning instance key has been
/// registered. <see cref="IdentityProviderRegistrar{TSettings, TInstanceSettings}"/>
/// checks for an existing marker to reject duplicate registrations of the same instance
/// key within one composition; the marker lives and dies with its service collection, so
/// multiple hosts in one process never cross-contaminate.
/// </summary>
/// <param name="Value">The fully-qualified instance registration key.</param>
internal sealed record ProcessedInstanceKey(string Value);
