namespace Cirreum.Identity.Provisioning;

using System.Diagnostics;

/// <summary>
/// A low-allocation scope over a single provisioning callback. On the chosen outcome it records
/// the duration and outcome-count metrics (and, for an allowed result, the minted-claim count),
/// and — when a tracing listener is active — annotates the span. Call exactly one outcome method,
/// then dispose; a <c>using</c> statement handles both.
/// </summary>
/// <remarks>
/// Obtain one via <see cref="ProvisioningTelemetry.StartProvision(string, string)"/>. Only
/// provider, instance, and outcome are tagged — no user identifiers or email, so a provisioning
/// span or metric never carries PII.
/// </remarks>
public readonly struct ProvisioningTrace : IDisposable {

	private const string ProviderTag = "cirreum.identity.provider";
	private const string InstanceTag = "cirreum.identity.instance";
	private const string OutcomeTag = "cirreum.identity.outcome";
	private const string ClaimCountTag = "cirreum.identity.claim_count";

	private readonly Activity? _activity;
	private readonly long _startTimestamp;
	private readonly string _provider;
	private readonly string _instance;

	internal ProvisioningTrace(string provider, string instance) {
		this._provider = provider;
		this._instance = instance;
		this._startTimestamp = Stopwatch.GetTimestamp();
		this._activity = ProvisioningTelemetry.ActivitySource.StartActivity("identity.provision");
		if (this._activity is not null) {
			this._activity.SetTag(ProviderTag, provider);
			this._activity.SetTag(InstanceTag, instance);
		}
	}

	/// <summary>Records an allowed outcome and the number of claims minted.</summary>
	/// <param name="claimCount">The count of custom claims embedded in the issued token.</param>
	public void Allowed(int claimCount) => this.Record("allowed", ActivityStatusCode.Ok, claimCount);

	/// <summary>Records a denied outcome — a valid access decision, not an error.</summary>
	public void Denied() => this.Record("denied", ActivityStatusCode.Ok, claimCount: null);

	/// <summary>Records a failed outcome — the provisioner threw or the callback could not complete.</summary>
	public void Failed() => this.Record("error", ActivityStatusCode.Error, claimCount: null);

	private void Record(string outcome, ActivityStatusCode status, int? claimCount) {
		var elapsedMs = Stopwatch.GetElapsedTime(this._startTimestamp).TotalMilliseconds;

		var tags = new TagList {
			{ ProviderTag, this._provider },
			{ InstanceTag, this._instance },
			{ OutcomeTag, outcome },
		};

		ProvisioningTelemetry.Duration.Record(elapsedMs, tags);
		ProvisioningTelemetry.Count.Add(1, tags);

		if (claimCount is int count) {
			ProvisioningTelemetry.Claims.Record(count, tags);
			this._activity?.SetTag(ClaimCountTag, count);
		}

		if (this._activity is not null) {
			this._activity.SetTag(OutcomeTag, outcome);
			this._activity.SetStatus(status);
		}
	}

	/// <summary>Ends the tracing span, if one was started.</summary>
	public void Dispose() => this._activity?.Dispose();

}
