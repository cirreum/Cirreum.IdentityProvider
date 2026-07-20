namespace Cirreum.IdentityProvider.Tests;

using Cirreum.Identity;
using Cirreum.Identity.Configuration;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Concrete test doubles for exercising
/// <see cref="IdentityProviderRegistrar{TSettings, TInstanceSettings}"/> behavior.
/// </summary>
internal sealed class TestInstanceSettings : IdentityProviderInstanceSettings {
}

internal sealed class TestProviderSettings : IdentityProviderSettings<TestInstanceSettings> {
}

internal sealed class TestIdentityRegistrar : IdentityProviderRegistrar<TestProviderSettings, TestInstanceSettings> {

	public override string ProviderName => "TestIdentity";

	public List<string> RegisteredKeys { get; } = [];

	public List<string> MappedKeys { get; } = [];

	public List<string> ValidatedSources { get; } = [];

	public override void ValidateSettings(TestInstanceSettings settings) {
		this.ValidatedSources.Add(settings.Source);
	}

	protected override void RegisterProvisioner(
		string key,
		TestInstanceSettings settings,
		IServiceCollection services,
		IConfiguration configuration) {
		this.RegisteredKeys.Add(key);
	}

	protected override void MapProvisioner(
		string key,
		TestInstanceSettings settings,
		IEndpointRouteBuilder endpoints) {
		this.MappedKeys.Add(key);
	}

}
