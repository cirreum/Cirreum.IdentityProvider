namespace Cirreum.Identity;

using Cirreum.Identity.Configuration;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Abstract base class for implementing identity registrars that handle the
/// registration and configuration of provisioning callback endpoints within the dependency
/// injection container and the HTTP endpoint route builder.
/// </summary>
/// <typeparam name="TSettings">The type of provider settings that contains multiple instances.</typeparam>
/// <typeparam name="TInstanceSettings">The type of individual instance settings for this provider.</typeparam>
/// <remarks>
/// <para>
/// Identity provisioning providers differ from authorization providers in that they must
/// register both DI services (at <see cref="IServiceCollection"/> time, before
/// <c>builder.Build()</c>) and HTTP endpoints (at <see cref="IEndpointRouteBuilder"/> time,
/// after <c>builder.Build()</c>). This base class therefore exposes two registration phases:
/// <see cref="Register"/> for the services phase and <see cref="Map"/> for the
/// endpoints phase.
/// </para>
/// <para>
/// The instance key under <c>Instances:</c> in <c>appsettings.json</c> serves double duty as
/// both the logical instance name and the value of <see cref="IdentityProviderInstanceSettings.Source"/>
/// stamped into <c>ProvisionContext</c> by the callback handler. The key is also used as the
/// keyed DI key under which <c>IUserProvisioner</c> is registered, so multi-IdP apps can
/// register one provisioner per source and have the correct one resolved automatically.
/// </para>
/// </remarks>
public abstract class IdentityProviderRegistrar<TSettings, TInstanceSettings>
	: IProviderRegistrar<TSettings, TInstanceSettings>
	where TInstanceSettings : IdentityProviderInstanceSettings
	where TSettings : IdentityProviderSettings<TInstanceSettings> {

	private static readonly Dictionary<string, string> ProcessedInstances = [];

	/// <inheritdoc/>
	public ProviderType ProviderType => ProviderType.Identity;

	/// <inheritdoc/>
	public abstract string ProviderName { get; }

	/// <summary>
	/// Validates provider-instance settings. Override to implement provider-specific validation
	/// beyond the common <see cref="IdentityProviderInstanceSettings.Route"/> check performed by
	/// the base <see cref="RegisterInstance"/>.
	/// </summary>
	/// <param name="settings">The instance settings to validate.</param>
	public virtual void ValidateSettings(
		TInstanceSettings settings) {
	}

	/// <summary>
	/// Registers DI services for all enabled instances in <paramref name="providerSettings"/>.
	/// Called during the services phase (before <c>builder.Build()</c>) by the provider-specific
	/// <c>Add*Provisioning&lt;T&gt;()</c> extension in the Runtime Extensions layer.
	/// </summary>
	/// <param name="providerSettings">Populated provider settings (bound from configuration).</param>
	/// <param name="services">The DI service collection.</param>
	/// <param name="configuration">The root configuration object.</param>
	public virtual void Register(
		TSettings providerSettings,
		IServiceCollection services,
		IConfiguration configuration) {

		if (providerSettings is null || providerSettings.Instances.Count == 0) {
			return;
		}

		foreach (var (key, settings) in providerSettings.Instances) {
			if (!settings.Enabled) {
				continue;
			}

			this.RegisterInstance(key, settings, services, configuration);
		}
	}

	/// <summary>
	/// Registers DI services for a single provider instance. Performs duplicate-registration
	/// detection, auto-populates <see cref="IdentityProviderInstanceSettings.Source"/> from the
	/// instance key (with a footgun guard), validates that <see cref="IdentityProviderInstanceSettings.Route"/>
	/// is set, and delegates instance-specific service registration to
	/// <see cref="RegisterProvisioner"/>.
	/// </summary>
	/// <param name="key">The configuration instance key (becomes the Source name).</param>
	/// <param name="settings">The instance settings.</param>
	/// <param name="services">The DI service collection.</param>
	/// <param name="configuration">The root configuration object.</param>
	public virtual void RegisterInstance(
		string key,
		TInstanceSettings settings,
		IServiceCollection services,
		IConfiguration configuration) {

		// Guard against duplicate registration of the same instance key across multiple calls
		var providerRegistrationKey = $"Cirreum.{this.ProviderType}.{this.ProviderName}::{key}";
		if (!ProcessedInstances.TryAdd(providerRegistrationKey, $"{settings.GetHashCode()}")) {
			throw new InvalidOperationException(
				$"A provisioning instance with the key '{key}' for provider '{this.ProviderName}' has already been registered.");
		}

		// Must have settings
		if (settings is null) {
			throw new InvalidOperationException($"Missing required settings for provisioning instance '{key}'");
		}

		// The Source name is always derived from the instance key. If a user explicitly set
		// Source in configuration to a different value, fail loudly rather than silently
		// overwriting it — that would be a surprising footgun.
		if (!string.IsNullOrWhiteSpace(settings.Source) && settings.Source != key) {
			throw new InvalidOperationException(
				$"Provisioning instance '{key}' has Source='{settings.Source}' configured, but the " +
				$"source name is auto-derived from the instance key. Remove the 'Source' value " +
				$"from configuration — the instance key IS the source name.");
		}
		settings.Source = key;

		// Every provisioning instance must expose an HTTP route for the callback endpoint
		if (string.IsNullOrWhiteSpace(settings.Route)) {
			throw new InvalidOperationException(
				$"Provisioning provider '{this.ProviderName}' instance '{key}' requires a Route.");
		}

		// Provider-specific validation
		this.ValidateSettings(settings);

		// Delegate instance-specific DI registration to derived class
		this.RegisterProvisioner(key, settings, services, configuration);
	}

	/// <summary>
	/// Maps HTTP endpoints for all enabled instances in <paramref name="providerSettings"/>.
	/// Called during the endpoints phase (after <c>builder.Build()</c>) by the provider-specific
	/// <c>Map*Provisioning()</c> extension in the Runtime Extensions layer.
	/// </summary>
	/// <param name="providerSettings">Populated provider settings (bound from configuration).</param>
	/// <param name="endpoints">The endpoint route builder.</param>
	public virtual void Map(
		TSettings providerSettings,
		IEndpointRouteBuilder endpoints) {

		if (providerSettings is null || providerSettings.Instances.Count == 0) {
			return;
		}

		foreach (var (key, settings) in providerSettings.Instances) {
			if (!settings.Enabled) {
				continue;
			}

			this.MapProvisioner(key, settings, endpoints);
		}
	}

	/// <summary>
	/// Gets the configuration section path for a specific instance.
	/// </summary>
	/// <param name="instanceKey">The instance key.</param>
	/// <returns>The configuration section path, e.g. <c>Cirreum:Identity:Providers:Webhook:Instances:Descope</c>.</returns>
	protected string GetInstanceSectionPath(
		string instanceKey) =>
		$"Cirreum:{this.ProviderType}:Providers:{this.ProviderName}:Instances:{instanceKey}";

	/// <summary>
	/// Registers DI services for a single enabled instance. Derived classes implement this
	/// to register their <c>IUserProvisioner</c> (typically as a keyed scoped service),
	/// bind settings, and register any provider-specific collaborators.
	/// </summary>
	/// <param name="key">The configuration instance key.</param>
	/// <param name="settings">The instance settings.</param>
	/// <param name="services">The DI service collection.</param>
	/// <param name="configuration">The root configuration object.</param>
	protected abstract void RegisterProvisioner(
		string key,
		TInstanceSettings settings,
		IServiceCollection services,
		IConfiguration configuration);

	/// <summary>
	/// Maps the HTTP endpoint for a single enabled instance. Derived classes implement this
	/// to call <c>EndpointRouteBuilderExtensions.MapPost</c> (or equivalent) with the
	/// provider's callback handler.
	/// </summary>
	/// <param name="key">The configuration instance key.</param>
	/// <param name="settings">The instance settings.</param>
	/// <param name="endpoints">The endpoint route builder.</param>
	protected abstract void MapProvisioner(
		string key,
		TInstanceSettings settings,
		IEndpointRouteBuilder endpoints);

}
