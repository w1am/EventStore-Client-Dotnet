using System.Diagnostics;
using EventStore.Client.Diagnostics.Telemetry;
using EventStore.Client.Diagnostics.Tracing;

namespace EventStore.Client.Diagnostics;

static class EventStoreClientDiagnostics {
	static readonly ActivitySource _activitySource =
		new ActivitySource(EventStoreClientInstrumentation.ActivitySourceName);

	static readonly ActivityTagsCollection _defaultTags = [new(TelemetryAttributes.DatabaseSystem, "eventstoredb")];

	public static Activity? StartActivity(
		string operation, ActivityTagsCollection? tags, ActivityKind activityKind = default,
		ActivityContext? activityContext = null
	) {
		var activity = _activitySource.CreateActivity(
			operation,
			activityKind,
			parentContext: activityContext ?? default,
			new ActivityTagsCollection {
					{ TelemetryAttributes.DatabaseOperation, operation }
				}
				.WithTags(_defaultTags)
				.WithTags(tags),
			idFormat: ActivityIdFormat.W3C
		);

		return activity?.Start();
	}

	public static async ValueTask<T> TraceOperation<T>(
		Func<ValueTask<T>> tracedOperation, string operationName, ActivityTagsCollection? tags = null
	) {
		using var activity = StartActivity(operationName, tags, ActivityKind.Client);

		try {
			var res = await tracedOperation().ConfigureAwait(false);
			activity?.SetActivityStatus(ActivityStatus.Ok());
			return res;
		} catch (Exception ex) {
			activity?.SetActivityStatus(ActivityStatus.Error(ex));
			throw;
		}
	}
	
	public static void TraceSubscriptionEvent(
		string? subscriptionId, ResolvedEvent evnt, ChannelInfo channelInfo,
		EventStoreClientSettings settings, UserCredentials? userCredentials
	) {
		var restoredTracingContext =
			evnt.OriginalEvent.Metadata.RestoreTracingContext();

		if (restoredTracingContext != null)
			StartActivity(
				TracingConstants.Operations.Subscribe,
				new ActivityTagsCollection {
						{
							TelemetryAttributes.EventStoreStream,
							evnt.OriginalEvent.EventStreamId
						}, {
							TelemetryAttributes.EventStoreSubscriptionId,
							subscriptionId
						}, {
							TelemetryAttributes.EventStoreEventId,
							evnt.OriginalEvent.EventId.ToString()
						}, {
							TelemetryAttributes.EventStoreEventType,
							evnt.OriginalEvent.EventType
						}
					}.WithTagsFrom(channelInfo, settings)
					.WithTagsFrom(userCredentials),
				ActivityKind.Consumer,
				restoredTracingContext
			)?.Dispose();
	}
}
