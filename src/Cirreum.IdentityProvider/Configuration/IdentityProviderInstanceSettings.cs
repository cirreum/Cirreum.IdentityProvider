namespace Cirreum.Identity.Configuration;

using Cirreum.Providers.Configuration;

/// <summary>
/// Abstract base class for identity provisioning instance settings — defines common
/// configuration properties shared by all identity provisioning provider instances.
/// </summary>
public abstract class IdentityProviderInstanceSettings
	: IProviderInstanceSettings {

	/// <summary>
	/// Gets the source name identifying this provisioning instance.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>This value is auto-populated from the instance key</strong> during provider
	/// registration (see <see cref="IdentityProviderRegistrar{TSettings, TInstanceSettings}.RegisterInstance"/>).
	/// The instance key under <c>Instances:</c> in <c>appsettings.json</c> serves double duty
	/// as both the logical instance name and the <c>Source</c> value stamped into
	/// <c>ProvisionContext</c> by the callback handler. The same key is also used as the
	/// keyed DI key under which <c>IUserProvisioner</c> is registered.
	/// </para>
	/// <para>
	/// Do <strong>not</strong> set <c>Source</c> in configuration. If a mismatched value is
	/// detected during registration, an <see cref="InvalidOperationException"/> is thrown.
	/// </para>
	/// <example>
	/// <code>
	/// // appsettings.json
	/// //   "Webhook": { "Instances": { "Descope": { ... } } }
	/// //                               ^^^^^^^ — this key becomes the source name
	/// </code>
	/// </example>
	/// </remarks>
	public string Source { get; set; } = "";

	/// <summary>
	/// Gets or sets a value indicating whether this provider instance is enabled.
	/// When <see langword="false"/>, the instance will be skipped during registration.
	/// </summary>
	public bool Enabled { get; set; }

	/// <summary>
	/// Gets or sets the HTTP route at which this provisioning instance's callback endpoint
	/// is exposed. Required for every enabled instance.
	/// </summary>
	/// <example>
	/// <c>/auth/descope/provision</c>, <c>/auth/entra/provision</c>
	/// </example>
	public string Route { get; set; } = "";
}
