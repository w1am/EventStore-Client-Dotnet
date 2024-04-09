using System.Diagnostics;
using System.Runtime.CompilerServices;
using EventStore.Client.Diagnostics.Telemetry;
using EventStore.Client.Diagnostics.Tracing;

namespace EventStore.Client.Diagnostics;

static class ActivityExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TracingMetadata GetTracingMetadata(this Activity activity)
		=> new(activity.TraceId.ToString(), activity.SpanId.ToString());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetActivityStatus(this Activity activity, ActivityStatus activityStatus) {
		var (activityStatusCode, description, exception) = activityStatus;

		var statusCode = activityStatusCode switch {
			ActivityStatusCode.Error => "ERROR",
			ActivityStatusCode.Ok    => "OK",
			_                        => "UNSET"
		};

		activity.SetStatus(activityStatus.StatusCode, description);
		activity.SetTag(TelemetryAttributes.OtelStatusCode, statusCode);
		activity.SetTag(TelemetryAttributes.OtelStatusDescription, description);

		if (exception != null)
			activity.SetException(exception);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void SetException(this Activity activity, Exception exception) {
		var tags = new ActivityTagsCollection {
			{ TelemetryAttributes.ExceptionType, exception.GetType().Name },
			{ TelemetryAttributes.ExceptionMessage, $"{exception.Message} {exception.InnerException?.Message}" },
			{ TelemetryAttributes.ExceptionStacktrace, exception.StackTrace }
		};

		foreach (var tag in tags) {
			activity.SetTag(tag.Key, tag.Value);
		}

		activity.AddEvent(new ActivityEvent(TelemetryAttributes.ExceptionEventName, DateTimeOffset.Now, tags));
	}
}
