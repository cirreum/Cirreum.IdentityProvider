namespace Cirreum.IdentityProvider.Tests;

public class UserProvisionerBaseTests {

	private sealed record TestIdentity(string ExternalUserId, IReadOnlyList<IdentityClaim> Claims) : IProvisionedIdentity;

	private sealed class TestProvisioner(TestIdentity? existing, TestIdentity? onboarded)
		: UserProvisionerBase<TestIdentity> {

		public bool OnboardCalled { get; private set; }

		protected override Task<TestIdentity?> FindUserAsync(string externalUserId, CancellationToken cancellationToken) =>
			Task.FromResult(existing);

		protected override Task<ProvisionResult> ProvisionNewUserAsync(ProvisionContext context, CancellationToken cancellationToken) {
			this.OnboardCalled = true;
			return Task.FromResult(onboarded is not null
				? ProvisionResult.Allow(onboarded.Claims)
				: ProvisionResult.Deny());
		}
	}

	private static ProvisionContext Context() => new() {
		Source = "test",
		ExternalUserId = "ext-1",
		CorrelationId = "corr",
		ClientAppId = "app",
		Email = "",
	};

	[Fact]
	public async Task Returning_identity_is_allowed_with_its_claims() {
		var claims = new[] { IdentityClaim.Roles("admin") };
		var provisioner = new TestProvisioner(new TestIdentity("ext-1", claims), onboarded: null);

		var result = await provisioner.ProvisionAsync(Context());

		result.Should().BeOfType<ProvisionResult.Allowed>().Which.Claims.Should().BeSameAs(claims);
		provisioner.OnboardCalled.Should().BeFalse();
	}

	[Fact]
	public async Task Unknown_identity_delegates_to_onboarding() {
		var provisioner = new TestProvisioner(
			existing: null,
			onboarded: new TestIdentity("ext-1", [IdentityClaim.Roles("user")]));

		var result = await provisioner.ProvisionAsync(Context());

		provisioner.OnboardCalled.Should().BeTrue();
		result.Should().BeOfType<ProvisionResult.Allowed>();
	}

}
