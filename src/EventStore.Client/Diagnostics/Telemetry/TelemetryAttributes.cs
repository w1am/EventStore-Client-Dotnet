namespace EventStore.Client.Diagnostics.Telemetry;

// The attributes below match the specification of v1.24.0 of the Open Telemetry semantic conventions.
// Some attributes are ignored where not required or relevant.
// https://github.com/open-telemetry/semantic-conventions/blob/v1.24.0/docs/general/trace.md
// https://github.com/open-telemetry/semantic-conventions/blob/v1.24.0/docs/database/database-spans.md
// https://github.com/open-telemetry/semantic-conventions/blob/v1.24.0/docs/exceptions/exceptions-spans.md
static class TelemetryAttributes {
	public const string DatabaseUser      = "db.user";
	public const string DatabaseSystem    = "db.system";
	public const string DatabaseOperation = "db.operation";

	public const string ServerAddress = "server.address";
	public const string ServerPort    = "server.port";

	public const string ExceptionEventName  = "exception";
	public const string ExceptionType       = "exception.type";
	public const string ExceptionMessage    = "exception.message";
	public const string ExceptionStacktrace = "exception.stacktrace";

	public const string OtelStatusCode        = "otel.status_code";
	public const string OtelStatusDescription = "otel.status_description";

	// Custom attributes
	public const string EventStoreStream         = "db.eventstoredb.stream";
	public const string EventStoreSubscriptionId = "db.eventstoredb.subscription.id";
	public const string EventStoreEventId        = "db.eventstoredb.event.id";
	public const string EventStoreEventType      = "db.eventstoredb.event.type";
}
