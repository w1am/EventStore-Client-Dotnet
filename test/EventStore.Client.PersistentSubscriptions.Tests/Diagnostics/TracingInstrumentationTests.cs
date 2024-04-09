using System.Diagnostics;
using EventStore.Client.Diagnostics.Telemetry;
using EventStore.Client.Diagnostics.Tracing;

namespace EventStore.Client.PersistentSubscriptions.Tests.Diagnostics;

[Trait("Category", "Diagnostics:Tracing")]
public class TracingInstrumentationTests(ITestOutputHelper output, DiagnosticsFixture fixture)
	: EventStoreTests<DiagnosticsFixture>(output, fixture) {
}
