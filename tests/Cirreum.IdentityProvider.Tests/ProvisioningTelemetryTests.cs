namespace Cirreum.IdentityProvider.Tests;

using Cirreum.Identity.Provisioning;
using System.Diagnostics.Metrics;

public class ProvisioningTelemetryTests {

	[Fact]
	public void Duration_RecordsSecondsWithTheCirreumBuckets() {
		const string instance = "duration-probe";
		var observed = new List<(Instrument Instrument, double Value)>();
		using var listener = new MeterListener {
			InstrumentPublished = (instrument, l) => {
				if (instrument.Meter.Name == ProvisioningTelemetry.SourceName) {
					l.EnableMeasurementEvents(instrument);
				}
			}
		};
		listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => {
			foreach (var tag in tags) {
				if (tag.Key == "cirreum.identity.instance" && Equals(tag.Value, instance)) {
					lock (observed) {
						observed.Add((instrument, value));
					}
				}
			}
		});
		listener.Start();

		using (var trace = ProvisioningTelemetry.StartProvision("oidc", instance)) {
			trace.Denied();
		}

		var (histogram, value) = observed.Should().ContainSingle().Subject;
		histogram.Name.Should().Be("cirreum.identity.provisioning.duration");
		histogram.Unit.Should().Be("s");
		value.Should().BeLessThan(1, "a sub-second callback recorded in seconds, not milliseconds");
		// Must match CirreumTelemetry.Histograms.DurationAdvice in Cirreum.Kernel.
		((Histogram<double>)histogram).Advice!.HistogramBucketBoundaries.Should().Equal(
			0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10);
	}

}
