namespace Cirreum.Identity.Provisioning;

/// <summary>
/// Provides context about the user and calling application during an external
/// identity provider's token issuance callback.
/// Passed to <see cref="IUserProvisioner"/> by provider-specific callback handlers
/// (for example Cirreum.Identity.EntraExternalId or Cirreum.Identity.Webhook).
/// </summary>
public sealed class ProvisionContext {

	/// <summary>
	/// Gets the identifier of the identity provider instance that triggered this provisioning call.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Matches the instance key configured under
	/// <c>Cirreum:Identity:Providers:{Webhook|EntraExternalId}:Instances:{key}</c> in
	/// <c>appsettings.json</c>. Each instance registers its <see cref="IUserProvisioner"/> in DI
	/// as a keyed service under this same key, so an application consuming multiple identity
	/// providers can register one provisioner per source and have the correct one resolved
	/// automatically.
	/// </para>
	/// <para>
	/// Most single-IdP applications can ignore this field — it is only meaningful when a host
	/// serves provisioning callbacks from more than one provider at once, or when auditing
	/// which provider a user came through.
	/// </para>
	/// </remarks>
	public required string Source { get; init; }

	/// <summary>
	/// Gets the external identity provider's unique identifier for the user being authenticated.
	/// </summary>
	/// <remarks>
	/// For Entra External ID this is the Entra object ID assigned to the user.
	/// For Descope this is the Descope user ID.
	/// Consuming applications typically store this as <c>ExternalUserId</c> on their own
	/// user record for future lookup.
	/// </remarks>
	public required string ExternalUserId { get; init; }

	/// <summary>
	/// Gets the correlation ID from the identity provider callback payload.
	/// Used for tracing the token issuance request end-to-end.
	/// </summary>
	public required string CorrelationId { get; init; }

	/// <summary>
	/// Gets the app ID of the client application that initiated the authentication request.
	/// This is the application the user is signing into, not the claims/provisioning endpoint app itself.
	/// </summary>
	public required string ClientAppId { get; init; }

	/// <summary>
	/// Gets the email address of the user being authenticated.
	/// </summary>
	/// <remarks>
	/// May be an empty string if the identity provider did not supply an email address
	/// (for example certain social identity providers with email sharing disabled).
	/// Provisioners performing email-based invitation lookup should handle this case.
	/// </remarks>
	public required string Email { get; init; }

}
