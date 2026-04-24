namespace Cirreum.Identity.Provisioning;

/// <summary>
/// Abstract <see cref="UserProvisionerBase{TUser}"/> specialization that onboards new users
/// via self-service sign-up — anyone authenticating successfully with the identity provider
/// is allowed in, and a user record is created on first sign-in.
/// </summary>
/// <typeparam name="TUser">
/// The application's user entity. Must implement <see cref="IProvisionedUser"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// Self-service onboarding is the right fit when the application is open to any successful
/// authentication — for example a public consumer app, an open-registration B2C portal, or
/// a borrower app where the IdP already gates access.
/// </para>
/// <para>
/// Derived classes implement <see cref="UserProvisionerBase{TUser}.FindUserAsync"/> to look
/// up returning users and <see cref="CreateSelfServiceUserAsync"/> to create the user
/// record and choose the initial role(s).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class ClientABorrowerProvisioner(AppDbContext db)
///     : SelfServiceUserProvisionerBase&lt;BorrowerUser&gt; {
///
///     protected override Task&lt;BorrowerUser?&gt; FindUserAsync(
///         string externalUserId,
///         CancellationToken ct) =>
///         db.Users.FirstOrDefaultAsync(u => u.ExternalUserId == externalUserId, ct);
///
///     protected override async Task&lt;BorrowerUser?&gt; CreateSelfServiceUserAsync(
///         ProvisionContext context,
///         CancellationToken ct) {
///         var user = new BorrowerUser {
///             ExternalUserId = context.ExternalUserId,
///             Email = context.Email,
///             Roles = [BorrowerRoles.Default],
///         };
///         db.Users.Add(user);
///         await db.SaveChangesAsync(ct);
///         return user;
///     }
/// }
/// </code>
/// </example>
public abstract class SelfServiceUserProvisionerBase<TUser> : UserProvisionerBase<TUser>
	where TUser : IProvisionedUser {

	/// <inheritdoc />
	protected override async Task<ProvisionResult> ProvisionNewUserAsync(
		ProvisionContext context,
		CancellationToken cancellationToken) {

		var newUser = await this.CreateSelfServiceUserAsync(
			context,
			cancellationToken);

		return newUser is not null
			? ProvisionResult.Allow(newUser.Roles)
			: ProvisionResult.Deny();
	}

	/// <summary>
	/// Creates the application user record for a first-time sign-in and chooses the
	/// initial role(s) to embed in the issued token.
	/// </summary>
	/// <param name="context">The provisioning context from the identity-provider callback.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// The newly created user record, or <see langword="null"/> to deny the sign-in
	/// (for example if the application imposes a sign-up restriction — allow-listed domains,
	/// waitlist, etc.).
	/// </returns>
	protected abstract Task<TUser?> CreateSelfServiceUserAsync(
		ProvisionContext context,
		CancellationToken cancellationToken);
}
