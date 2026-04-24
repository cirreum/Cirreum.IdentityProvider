namespace Cirreum.Identity.Provisioning;

/// <summary>
/// Abstract base for <see cref="IUserProvisioner"/> implementations that perform the
/// standard "returning user or onboard" orchestration.
/// </summary>
/// <typeparam name="TUser">
/// The application's user entity. Must implement <see cref="IProvisionedUser"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// This base encodes the universal two-step flow every provisioner performs:
/// </para>
/// <list type="number">
///   <item><description>Look up an existing user by external user ID — if found, allow with stored roles.</description></item>
///   <item><description>Otherwise, delegate to <see cref="ProvisionNewUserAsync"/> for onboarding.</description></item>
/// </list>
/// <para>
/// The onboarding branch is left fully abstract on this class so the implementation can
/// choose any model. Use <see cref="InvitationUserProvisionerBase{TUser}"/> for
/// invitation-based onboarding or <see cref="SelfServiceUserProvisionerBase{TUser}"/>
/// for open sign-up. Derive from this class directly only if your app needs custom
/// orchestration (for example a hybrid invitation / self-service flow).
/// </para>
/// <para>
/// Register the concrete implementation against an instance key via the Runtime Extensions
/// layer — <c>builder.AddIdentity().AddProvisioner&lt;MyProvisioner&gt;("instance_key")</c>.
/// Registration is scoped so the provisioner can access request-scoped services
/// (database contexts, loggers, etc.).
/// </para>
/// </remarks>
public abstract class UserProvisionerBase<TUser> : IUserProvisioner
	where TUser : IProvisionedUser {

	/// <inheritdoc />
	/// <remarks>
	/// Executes the standard two-step flow: returning-user lookup → onboard new user.
	/// Override only if the application requires a fundamentally different orchestration.
	/// </remarks>
	public virtual async Task<ProvisionResult> ProvisionAsync(
		ProvisionContext context,
		CancellationToken cancellationToken = default) {

		var user = await this.FindUserAsync(
			context.ExternalUserId,
			cancellationToken);

		if (user is not null) {
			return ProvisionResult.Allow(user.Roles);
		}

		return await this.ProvisionNewUserAsync(context, cancellationToken);
	}

	/// <summary>
	/// Looks up an existing user by their external identity provider user ID.
	/// </summary>
	/// <param name="externalUserId">
	/// The external user ID from the token issuance callback. Typically stored as
	/// <c>ExternalUserId</c> on the application's user record.
	/// </param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// The matching user, or <see langword="null"/> if no record exists for this
	/// external user ID.
	/// </returns>
	protected abstract Task<TUser?> FindUserAsync(
		string externalUserId,
		CancellationToken cancellationToken);

	/// <summary>
	/// Onboards a new user when <see cref="FindUserAsync"/> returns <see langword="null"/>.
	/// Derived classes implement this to run the appropriate onboarding flow for their
	/// instance — invitation redemption, self-service sign-up, or a custom strategy.
	/// </summary>
	/// <param name="context">The provisioning context from the identity-provider callback.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// <see cref="ProvisionResult.Allow(string[])"/> to grant access with the chosen roles,
	/// or <see cref="ProvisionResult.Deny()"/> to block token issuance.
	/// </returns>
	protected abstract Task<ProvisionResult> ProvisionNewUserAsync(
		ProvisionContext context,
		CancellationToken cancellationToken);
}
