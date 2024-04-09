namespace EventStore.Client.Diagnostics.Tracing;

static class TracingConstants {
	public static class Metadata {
		public const string TraceId      = "$traceId";
		public const string SpanId       = "$spanId";
	}

	public static class Operations {
		public const string Append    = "streams.append";
		public const string Subscribe = "streams.subscribe";
	}
}
