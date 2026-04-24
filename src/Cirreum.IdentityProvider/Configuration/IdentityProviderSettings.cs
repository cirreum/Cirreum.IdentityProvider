namespace Cirreum.Identity.Configuration;

using Cirreum.Providers.Configuration;

/// <summary>
/// Abstract base class for identity provisioning provider settings that contains a collection
/// of provider instances with their individual configurations.
/// </summary>
/// <typeparam name="TInstanceSettings">The type of individual instance settings for this provider.</typeparam>
public abstract class IdentityProviderSettings<TInstanceSettings>
	: IProviderSettings<TInstanceSettings>
	where TInstanceSettings : IdentityProviderInstanceSettings {

	/// <summary>
	/// Gets or sets the collection of provider instance settings keyed by instance name.
	/// Each instance represents a separately configured provisioning endpoint — for example,
	/// multiple webhook providers (Descope, Auth0) or multiple Entra External ID tenants.
	/// </summary>
	public Dictionary<string, TInstanceSettings> Instances { get; set; } = [];
}
