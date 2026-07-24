namespace Cirreum.IdentityProvider.Tests;

public class IdentityClaimTests {

	[Fact]
	public void Roles_stamps_the_reserved_roles_wire_name() {
		var claim = IdentityClaim.Roles("admin", "user");

		claim.Type.Should().Be(CustomClaimNames.Roles);
		claim.Values.Should().Equal("admin", "user");
	}

	[Fact]
	public void Name_stamps_the_reserved_name_as_a_single_value() {
		var claim = IdentityClaim.Name("Jane Smith");

		claim.Type.Should().Be(CustomClaimNames.Name);
		claim.Values.Should().ContainSingle().Which.Should().Be("Jane Smith");
	}

	[Theory]
	[InlineData("tenant")]
	[InlineData("customTenant")]
	[InlineData("customtenant")]
	[InlineData("CUSTOMTenant")]
	public void Of_normalizes_every_spelling_to_one_wire_name(string input) {
		IdentityClaim.Of(input, "acme").Type.Should().Be("customTenant");
	}

	[Fact]
	public void Of_throws_on_a_bare_prefix_with_no_suffix() {
		var act = () => IdentityClaim.Of("custom", "x");

		act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("name");
	}

	[Theory]
	[InlineData("correlationId")]
	[InlineData("CorrelationId")]        // the reserved set is case-insensitive
	[InlineData("customCorrelationId")]  // the prefixed spelling canonicalizes to the same reserved name
	[InlineData("CustomCorrelationId")]
	public void Of_throws_on_a_reserved_protocol_name(string name) {
		var act = () => IdentityClaim.Of(name, "x");

		act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("name");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Of_throws_on_a_blank_name(string name) {
		var act = () => IdentityClaim.Of(name, "x");

		act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("name");
	}

	[Fact]
	public void Roles_with_no_values_throws() {
		var act = () => IdentityClaim.Roles();

		act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
	}

	[Fact]
	public void Of_with_no_values_throws() {
		var act = () => IdentityClaim.Of("tenant");

		act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void A_blank_value_throws(string blank) {
		var act = () => IdentityClaim.Roles("admin", blank);

		act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
	}

	[Fact]
	public void Values_are_defensively_copied() {
		var roles = new List<string> { "admin" };
		var claim = IdentityClaim.Roles(roles);

		roles.Add("sneaky");

		claim.Values.Should().ContainSingle().Which.Should().Be("admin");
	}

}
