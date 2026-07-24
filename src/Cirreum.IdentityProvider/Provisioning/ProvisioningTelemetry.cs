namespace Cirreum.Identity.Provisioning;

using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// OpenTelemetry instrumentation for the user-provisioning callback. Both provider adapters
/// emit through this single source and meter, tagging by provider and instance, so a consumer
/// enables one name (<see cref="SourceName"/>) to observe all provisioning regardless of
/// which identity provider drove it.
/// </summary>
/// <remarks>
/// <para>
/// Wrap the provisioner invocation in the scope returned by
/// <see cref="StartProvision(string, string)"/> and call exactly one outcome method on it.
/// </para>
/// <para>
/// Everything here is a no-op cost when no listener is attached — <see cref="ActivitySource"/>
/// yields a <see langword="null"/> activity with no collector, and the metric instruments are
/// cheap no-ops without a <see cref="MeterListener"/>.
/// </para>
/// </remarks>
public static class ProvisioningTelemetry {

	/// <summary>The <see cref="ActivitySource"/> and <see cref="Meter"/> name — enable this to collect provisioning telemetry.</summary>
	public const string SourceName = "Cirreum.Identity.Provisioning";

	private static readonly string? Version = typeof(ProvisioningTelemetry).Assembly.GetName().Version?.ToString();

	internal static readonly ActivitySource ActivitySource = new(SourceName, Version);

	private static readonly Meter Meter = new(SourceName, Version);

	internal static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
		"cirreum.identity.provision.duration",
		unit: "ms",
		description: "Time spent in the user-provisioning callback.");

	internal static readonly Counter<long> Count = Meter.CreateCounter<long>(
		"cirreum.identity.provision.count",
		description: "User-provisioning callbacks, by outcome.");

	internal static readonly Histogram<int> Claims = Meter.CreateHistogram<int>(
		"cirreum.identity.provision.claims",
		description: "Number of custom claims minted per allowed provisioning — the observable form of the mint-light discipline.");

	/// <summary>
	/// Begins instrumenting a provisioning callback. Wrap the provisioner invocation in the
	/// returned scope and call one of its outcome methods
	/// (<see cref="ProvisioningTrace.Allowed(int)"/>, <see cref="ProvisioningTrace.Denied"/>,
	/// or <see cref="ProvisioningTrace.Failed"/>) before disposing it.
	/// </summary>
	/// <param name="provider">The identity-provider adapter (for example <c>entra</c> or <c>oidc</c>).</param>
	/// <param name="instance">The provisioning instance key (<c>ProvisionContext.Source</c>).</param>
	public static ProvisioningTrace StartProvision(string provider, string instance) =>
		new(provider, instance);

}
