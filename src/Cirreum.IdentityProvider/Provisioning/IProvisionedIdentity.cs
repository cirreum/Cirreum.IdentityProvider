namespace Cirreum.Identity.Provisioning;

/// <summary>
/// The lightweight identity an application provisions for a user during token issuance — the
/// external identifier plus the claims to mint. Distinct from <c>IApplicationUser</c> (the full,
/// db-backed user the authorization pipeline reads); this is only the slice the provisioner needs.
/// </summary>
/// <remarks>
/// Implement this on whatever type your application exposes for provisioning — often a lightweight
/// projection of your user entity. The base provisioner reads <see cref="ExternalUserId"/> for
/// lookup and <see cref="Claims"/> for the issued token; all other fields on your type are
/// invisible to this library. The shape of your type is the intent: make a value a required
/// constructor parameter to require it, or leave <see cref="Claims"/> at its default to mint nothing.
/// </remarks>
/// <example>
/// <code>
/// // Roles plus an app-owned display name and tenant:
/// public sealed record AppIdentity(
///     string ExternalUserId,
///     IReadOnlyList&lt;string&gt; Roles,
///     string TenantId,
///     string DisplayName
/// ) : IProvisionedIdentity {
///     public IReadOnlyList&lt;IdentityClaim&gt; Claims =&gt; [
///         IdentityClaim.Roles(Roles),
///         IdentityClaim.Name(DisplayName),
///         IdentityClaim.Of("tenant", TenantId)
///     ];
/// }
///
/// // An ownership / ABAC app that mints nothing — Claims defaults to [].
/// public sealed record GuestIdentity(string ExternalUserId) : IProvisionedIdentity;
/// </code>
/// </example>
public interface IProvisionedIdentity {

	/// <summary>
	/// Gets the external identity provider's unique identifier for this user, as stored
	/// in the application database.
	/// </summary>
	/// <remarks>
	/// This must match the value of <see cref="ProvisionContext.ExternalUserId"/> from the
	/// token issuance callback. Consuming apps typically store this as <c>ExternalUserId</c>
	/// on their user record.
	/// </remarks>
	string ExternalUserId { get; }

	/// <summary>
	/// Gets the claims to mint into the issued token for this identity. Defaults to none.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Build each claim with the <see cref="IdentityClaim"/> factories —
	/// <see cref="IdentityClaim.Roles(string[])"/>, <see cref="IdentityClaim.Name(string)"/>,
	/// or <see cref="IdentityClaim.Of(string, string[])"/>.
	/// </para>
	/// <para>
	/// Keep the set small and authorization-relevant. Profile data the identity provider already
	/// holds (display name, email, …) is better selected natively at the provider than minted here.
	/// </para>
	/// </remarks>
	IReadOnlyList<IdentityClaim> Claims => [];

}
