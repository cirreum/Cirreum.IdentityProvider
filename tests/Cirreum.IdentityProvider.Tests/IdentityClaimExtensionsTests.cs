namespace Cirreum.IdentityProvider.Tests;

public class IdentityClaimExtensionsTests {

	[Fact]
	public void ToClaimMap_projects_each_type_to_its_values() {
		var map = new[] {
			IdentityClaim.Roles("admin"),
			IdentityClaim.Name("Jane"),
		}.ToClaimMap();

		map.Should().ContainKey(CustomClaimNames.Roles).WhoseValue.Should().Equal("admin");
		map.Should().ContainKey(CustomClaimNames.Name).WhoseValue.Should().Equal("Jane");
	}

	[Fact]
	public void ToClaimMap_merges_duplicate_types_and_dedupes_values() {
		var map = new[] {
			IdentityClaim.Roles("admin"),
			IdentityClaim.Of("customRoles", "user", "admin"), // same type, overlapping value
		}.ToClaimMap();

		map.Should().ContainSingle();
		map[CustomClaimNames.Roles].Should().BeEquivalentTo(new[] { "admin", "user" });
	}

	[Fact]
	public void ToClaimMap_of_an_empty_set_is_empty() {
		Array.Empty<IdentityClaim>().ToClaimMap().Should().BeEmpty();
	}

}
