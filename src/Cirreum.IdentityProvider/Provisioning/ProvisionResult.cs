namespace Cirreum.Identity.Provisioning;

/// <summary>
/// Represents the outcome of a user provisioning attempt during external identity
/// provider token issuance.
/// </summary>
/// <remarks>
/// Use the static factory methods to construct a result:
/// <list type="bullet">
///   <item><description><see cref="Allow(IReadOnlyList{IdentityClaim})"/> — grant access, embedding the given claims</description></item>
///   <item><description><see cref="Deny()"/> — block token issuance for this user</description></item>
/// </list>
/// </remarks>
public abstract record ProvisionResult {

	// Private constructor prevents subclassing outside this type.
	// Nested records have access to private members of the enclosing type.
	private ProvisionResult() { }

	/// <summary>
	/// The user is allowed; the given claims are embedded in the issued token. An empty set is
	/// valid — the user is admitted with no custom claims.
	/// </summary>
	public sealed record Allowed(IReadOnlyList<IdentityClaim> Claims) : ProvisionResult;

	/// <summary>
	/// The user is denied; token issuance will be blocked.
	/// </summary>
	public sealed record Denied : ProvisionResult;

	/// <summary>
	/// Grants the user access and embeds the given claims in the issued token.
	/// </summary>
	/// <param name="claims">
	/// The claims to mint. May be empty to admit the user with no custom claims.
	/// </param>
	public static ProvisionResult Allow(IReadOnlyList<IdentityClaim> claims) => new Allowed(claims);

	/// <summary>
	/// Blocks token issuance for this user. The login will be rejected by the identity provider.
	/// </summary>
	public static ProvisionResult Deny() => new Denied();

}
