namespace Cirreum.IdentityProvider.Tests;

public class ProvisionResultTests {

	[Fact]
	public void Allow_carries_the_claims() {
		var claims = new[] { IdentityClaim.Roles("admin") };

		ProvisionResult.Allow(claims)
			.Should().BeOfType<ProvisionResult.Allowed>()
			.Which.Claims.Should().BeSameAs(claims);
	}

	[Fact]
	public void Allow_with_no_claims_is_a_valid_allowed_result() {
		ProvisionResult.Allow([])
			.Should().BeOfType<ProvisionResult.Allowed>()
			.Which.Claims.Should().BeEmpty();
	}

	[Fact]
	public void Deny_produces_a_denied_result() {
		ProvisionResult.Deny().Should().BeOfType<ProvisionResult.Denied>();
	}

	[Fact]
	public void An_identity_with_only_an_external_id_mints_no_claims() {
		// An IProvisionedIdentity that adds nothing beyond ExternalUserId uses the interface
		// default (Claims => []) and provisions to an allowed result with no claims.
		IProvisionedIdentity identity = new MinimalIdentity("ext-1");

		identity.Claims.Should().BeEmpty();
		ProvisionResult.Allow(identity.Claims)
			.Should().BeOfType<ProvisionResult.Allowed>()
			.Which.Claims.Should().BeEmpty();
	}

	private sealed record MinimalIdentity(string ExternalUserId) : IProvisionedIdentity;

}
