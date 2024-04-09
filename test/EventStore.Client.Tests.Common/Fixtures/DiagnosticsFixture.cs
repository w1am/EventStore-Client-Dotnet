using System.Collections.Concurrent;
using System.Diagnostics;
using EventStore.Client.Diagnostics;
using EventStore.Client.Diagnostics.Telemetry;
using EventStore.Client.Diagnostics.Tracing;

namespace EventStore.Client.Tests;

public class DiagnosticsFixture : EventStoreFixture {
	readonly ConcurrentQueue<Activity> _activities = [];

	public DiagnosticsFixture() {
		var diagnosticActivityListener = new ActivityListener {
			ShouldListenTo = source => source.Name == EventStoreClientInstrumentation.ActivitySourceName,
			Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
				ActivitySamplingResult.AllDataAndRecorded,
			ActivityStopped = activity => _activities.Enqueue(activity)
		};

		OnSetup = () => {
			ActivitySource.AddActivityListener(diagnosticActivityListener);

			return Task.CompletedTask;
		};

		OnTearDown = () => {
			diagnosticActivityListener.Dispose();

			return Task.CompletedTask;
		};
	}

	public IEnumerable<Activity> GetActivitiesForOperation(string operation, string stream)
		=> _activities.Where(
			activity => (string?)activity.GetTagItem(TelemetryAttributes.DatabaseOperation) == operation
			         && (string?)activity.GetTagItem(TelemetryAttributes.EventStoreStream) == stream
		);
	
	public void AssertAppendActivityHasExpectedTags(Activity activity, string stream) {
		var expectedTags = new Dictionary<string, string?> {
			{ TelemetryAttributes.DatabaseSystem, "eventstoredb" },
			{ TelemetryAttributes.DatabaseOperation, TracingConstants.Operations.Append },
			{ TelemetryAttributes.EventStoreStream, stream },
			{ TelemetryAttributes.DatabaseUser, TestCredentials.Root.Username },
			{ TelemetryAttributes.OtelStatusCode, "OK" }
		};

		foreach (var tag in expectedTags)
			activity.Tags.ShouldContain(tag);
	}

	public void AssertErroneousAppendActivityHasExpectedTags(
		Activity activity, Exception actualException
	) {
		var expectedTags = new Dictionary<string, string?> {
			{ TelemetryAttributes.OtelStatusCode, "ERROR" },
			{ TelemetryAttributes.ExceptionType, actualException.GetType().Name }, {
				TelemetryAttributes.ExceptionMessage,
				$"{actualException.Message} {actualException.InnerException?.Message}"
			}
		};

		foreach (var tag in expectedTags)
			activity.Tags.ShouldContain(tag);

		activity.GetTagItem(TelemetryAttributes.ExceptionStacktrace).ShouldNotBeNull();
	}
	
	public void AssertSubscriptionActivityHasExpectedTags(
		Activity activity, string stream, string eventId, string? subscriptionId
	) {
		var expectedTags = new Dictionary<string, string?> {
			{ TelemetryAttributes.DatabaseSystem, "eventstoredb" },
			{ TelemetryAttributes.DatabaseOperation, TracingConstants.Operations.Subscribe },
			{ TelemetryAttributes.EventStoreStream, stream },
			{ TelemetryAttributes.EventStoreEventId, eventId },
			{ TelemetryAttributes.EventStoreEventType, EventStoreFixture.TestEventType },
			{ TelemetryAttributes.EventStoreSubscriptionId, subscriptionId },
			{ TelemetryAttributes.DatabaseUser, TestCredentials.Root.Username }
		};

		foreach (var tag in expectedTags)
			activity.Tags.ShouldContain(tag);
	}
}
