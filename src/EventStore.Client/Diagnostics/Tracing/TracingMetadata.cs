namespace EventStore.Client.Diagnostics.Tracing;

record TracingMetadata(string? TraceId, string? SpanId) {
	public static TracingMetadata None() => new(null, null);
}
