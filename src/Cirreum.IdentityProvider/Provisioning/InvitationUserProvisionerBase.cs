namespace Cirreum.Identity.Provisioning;

/// <summary>
/// Abstract <see cref="UserProvisionerBase{TUser}"/> specialization that onboards new users
/// via invitation redemption — anyone signing in without an existing account must present a
/// valid, unclaimed invitation matching their email address.
/// </summary>
/// <typeparam name="TUser">
/// The application's user entity. Must implement <see cref="IProvisionedUser"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// Invitation-based onboarding is the right fit when the application is closed to arbitrary
/// sign-ups — for example a back-office tool, an invited-partner network, or a staging
/// environment where access is granted explicitly.
/// </para>
/// <para>
/// Derived classes implement <see cref="UserProvisionerBase{TUser}.FindUserAsync"/> to look
/// up returning users and <see cref="RedeemInvitationAsync"/> to atomically claim an
/// invitation and create the user record.
/// </para>
/// <para>
/// If the identity provider does not supply an email address (some social identity providers
/// with email sharing disabled), the login is denied without attempting an invitation lookup
/// — invitations are matched on email.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class ClientBBorrowerProvisioner(AppDbContext db)
///     : InvitationUserProvisionerBase&lt;BorrowerUser&gt; {
///
///     protected override Task&lt;BorrowerUser?&gt; FindUserAsync(
///         string externalUserId,
///         CancellationToken ct) =>
///         db.Users.FirstOrDefaultAsync(u => u.ExternalUserId == externalUserId, ct);
///
///     protected override async Task&lt;BorrowerUser?&gt; RedeemInvitationAsync(
///         string email,
///         string externalUserId,
///         CancellationToken ct) {
///         var invitation = await db.Invitations
///             .FirstOrDefaultAsync(
///                 i => i.Email == email.ToLowerInvariant()
///                   &amp;&amp; i.ClaimedAt == null
///                   &amp;&amp; i.ExpiresAt > DateTimeOffset.UtcNow,
///                 ct);
///         if (invitation is null) {
///             return null;
///         }
///         invitation.ClaimedAt = DateTimeOffset.UtcNow;
///         invitation.ClaimedByExternalUserId = externalUserId;
///         var user = new BorrowerUser {
///             ExternalUserId = externalUserId,
///             Roles = invitation.Roles,
///         };
///         db.Users.Add(user);
///         await db.SaveChangesAsync(ct);
///         return user;
///     }
/// }
/// </code>
/// </example>
public abstract class InvitationUserProvisionerBase<TUser> : UserProvisionerBase<TUser>
	where TUser : IProvisionedUser {

	/// <inheritdoc />
	protected override async Task<ProvisionResult> ProvisionNewUserAsync(
		ProvisionContext context,
		CancellationToken cancellationToken) {

		if (string.IsNullOrWhiteSpace(context.Email)) {
			return ProvisionResult.Deny();
		}

		var newUser = await this.RedeemInvitationAsync(
			context.Email,
			context.ExternalUserId,
			cancellationToken);

		return newUser is not null
			? ProvisionResult.Allow(newUser.Roles)
			: ProvisionResult.Deny();
	}

	/// <summary>
	/// Atomically finds, validates, and claims a pending invitation by email address, and
	/// creates the application user record. This is Phase 1 of the two-phase onboarding flow.
	/// </summary>
	/// <param name="email">
	/// The email address from the identity provider's token issuance callback.
	/// Implementations should match case-insensitively.
	/// </param>
	/// <param name="externalUserId">
	/// The external user ID of the authenticating user. Store this on the new user record
	/// so <see cref="UserProvisionerBase{TUser}.FindUserAsync"/> can retrieve them on
	/// subsequent logins.
	/// </param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// The newly created user record if a valid invitation was found and successfully
	/// claimed, or <see langword="null"/> if no matching invitation exists, it has expired,
	/// or it has already been claimed. The base class reads
	/// <see cref="IProvisionedUser.Roles"/> from the returned instance to populate the
	/// issued token.
	/// </returns>
	/// <remarks>
	/// <para>
	/// Implementations should perform the find, expiry check, and claim in a single database
	/// transaction to prevent concurrent logins from redeeming the same invitation twice.
	/// At minimum, the implementation should:
	/// </para>
	/// <list type="bullet">
	///   <item><description>Query for an unclaimed, non-expired invitation matching <paramref name="email"/></description></item>
	///   <item><description>Return <see langword="null"/> immediately if none is found</description></item>
	///   <item><description>Mark the invitation as Claimed (e.g. set <c>ClaimedAt</c>, <c>ClaimedByExternalUserId</c>)</description></item>
	///   <item><description>Create a new user record with <paramref name="externalUserId"/> and the invitation's role</description></item>
	///   <item><description>Persist both changes atomically</description></item>
	/// </list>
	/// <para>
	/// Phase 2 (in-app onboarding) completes the redemption: collecting any remaining profile
	/// data, calling the identity provider's management API to assign provider-native roles,
	/// and marking the invitation as fully Redeemed.
	/// </para>
	/// </remarks>
	protected abstract Task<TUser?> RedeemInvitationAsync(
		string email,
		string externalUserId,
		CancellationToken cancellationToken);
}
