namespace Cirreum.IdentityProvider.Tests;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Unit tests for the identity registrar base: per-instance registration, the
/// Source/Route validation rules, and the collection-scoped duplicate-key guard
/// (state must live in the composition, not the process — two hosts in one process
/// are isolated).
/// </summary>
public class IdentityProviderRegistrarTests {

	private static readonly IConfiguration EmptyConfiguration =
		new ConfigurationBuilder().Build();

	[Fact]
	public void Register_RegistersEnabledInstances_SkipsDisabled() {
		var registrar = new TestIdentityRegistrar();
		var settings = CreateSettings(("alpha", true), ("beta", false), ("gamma", true));

		registrar.Register(settings, new ServiceCollection(), EmptyConfiguration);

		registrar.RegisteredKeys.Should().BeEquivalentTo(["alpha", "gamma"]);
	}

	[Fact]
	public void Register_NullSettings_IsNoOp() {
		var registrar = new TestIdentityRegistrar();

		registrar.Register(null!, new ServiceCollection(), EmptyConfiguration);

		registrar.RegisteredKeys.Should().BeEmpty();
	}

	[Fact]
	public void RegisterInstance_AutoPopulatesSourceFromKey() {
		var registrar = new TestIdentityRegistrar();
		var settings = CreateInstance();

		registrar.RegisterInstance("alpha", settings, new ServiceCollection(), EmptyConfiguration);

		settings.Source.Should().Be("alpha");
		registrar.ValidatedSources.Should().ContainSingle().Which.Should().Be("alpha");
	}

	[Fact]
	public void RegisterInstance_ExplicitMatchingSource_IsAllowed() {
		var registrar = new TestIdentityRegistrar();
		var settings = CreateInstance();
		settings.Source = "alpha";

		var act = () => registrar.RegisterInstance("alpha", settings, new ServiceCollection(), EmptyConfiguration);

		act.Should().NotThrow();
	}

	[Fact]
	public void RegisterInstance_MismatchedSource_Throws() {
		var registrar = new TestIdentityRegistrar();
		var settings = CreateInstance();
		settings.Source = "other";

		var act = () => registrar.RegisterInstance("alpha", settings, new ServiceCollection(), EmptyConfiguration);

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*Source*")
			.WithMessage("*alpha*");
	}

	[Fact]
	public void RegisterInstance_MissingRoute_Throws() {
		var registrar = new TestIdentityRegistrar();
		var settings = new TestInstanceSettings { Enabled = true };

		var act = () => registrar.RegisterInstance("alpha", settings, new ServiceCollection(), EmptyConfiguration);

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*Route*");
	}

	[Fact]
	public void RegisterInstance_DuplicateKeyInSameCollection_Throws() {
		var services = new ServiceCollection();
		var registrar = new TestIdentityRegistrar();
		registrar.RegisterInstance("alpha", CreateInstance(), services, EmptyConfiguration);

		// Even a different registrar object registering the same provider/key into the
		// SAME collection is a duplicate — the guard keys on provider name + instance key.
		var act = () => new TestIdentityRegistrar()
			.RegisterInstance("alpha", CreateInstance(), services, EmptyConfiguration);

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*alpha*")
			.WithMessage("*already been registered*");
	}

	[Fact]
	public void RegisterInstance_SameKeyInFreshCollection_DoesNotThrow() {
		// Two hosts composed in one process (the integration-test norm) must be fully
		// isolated: the duplicate guard is collection-scoped, not process-global.
		new TestIdentityRegistrar()
			.RegisterInstance("alpha", CreateInstance(), new ServiceCollection(), EmptyConfiguration);

		var act = () => new TestIdentityRegistrar()
			.RegisterInstance("alpha", CreateInstance(), new ServiceCollection(), EmptyConfiguration);

		act.Should().NotThrow();
	}

	[Fact]
	public void Map_MapsEnabledInstances_SkipsDisabled() {
		var registrar = new TestIdentityRegistrar();
		var settings = CreateSettings(("alpha", true), ("beta", false));

		registrar.Map(settings, Substitute.For<IEndpointRouteBuilder>());

		registrar.MappedKeys.Should().BeEquivalentTo(["alpha"]);
	}

	[Fact]
	public void Map_NullSettings_IsNoOp() {
		var registrar = new TestIdentityRegistrar();

		registrar.Map(null!, Substitute.For<IEndpointRouteBuilder>());

		registrar.MappedKeys.Should().BeEmpty();
	}

	private static TestInstanceSettings CreateInstance(bool enabled = true) => new() {
		Enabled = enabled,
		Route = "/test/provision",
	};

	private static TestProviderSettings CreateSettings(params (string Key, bool Enabled)[] instances) {
		var settings = new TestProviderSettings();
		foreach (var (key, enabled) in instances) {
			settings.Instances[key] = CreateInstance(enabled);
		}
		return settings;
	}

}
