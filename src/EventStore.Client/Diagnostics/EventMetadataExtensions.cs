using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using EventStore.Client.Diagnostics.Tracing;

namespace EventStore.Client.Diagnostics;

static class EventMetadataExtensions {
	// TODO JC: TEMPORARY WORKAROUND CODE TO USE CUSTOM METADATA UNTIL DATABASE CHANGES ARE READY
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<byte> InjectTracingMetadata(
		this ReadOnlyMemory<byte> rawCustomMetadata
	) {
		if (Activity.Current == null) return rawCustomMetadata.Span;

		try {
			using var customMetadataJson = JsonDocument.Parse(rawCustomMetadata);
			var       tracingMetadata    = Activity.Current.GetTracingMetadata();

			using var stream = new MemoryStream();
			using var writer = new Utf8JsonWriter(stream);

			writer.WriteStartObject();

			foreach (var prop in customMetadataJson.RootElement.EnumerateObject())
				prop.WriteTo(writer);

			writer.WriteIfNotNull(TracingConstants.Metadata.TraceId, tracingMetadata.TraceId)
				.WriteIfNotNull(TracingConstants.Metadata.SpanId, tracingMetadata.SpanId);

			writer.WriteEndObject();
			writer.Flush();

			return stream.ToArray();
		} catch (Exception) {
			return rawCustomMetadata.Span;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ActivityContext? RestoreTracingContext(this ReadOnlyMemory<byte> rawCustomMetadata) {
		var (traceId, spanId) = rawCustomMetadata.ExtractTracingMetadata();

		if (traceId == null || spanId == null)
			return default;

		try {
			return new(
				ActivityTraceId.CreateFromString(traceId.ToCharArray()),
				ActivitySpanId.CreateFromString(spanId.ToCharArray()),
				ActivityTraceFlags.Recorded,
				isRemote: true
			);
		} catch (Exception) {
			return default;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static TracingMetadata ExtractTracingMetadata(this ReadOnlyMemory<byte> rawCustomMetadata) {
		try {
			using var customMetadataJson = JsonDocument.Parse(rawCustomMetadata);

			return new TracingMetadata(
				customMetadataJson.RootElement.GetProperty(TracingConstants.Metadata.TraceId).GetString(),
				customMetadataJson.RootElement.GetProperty(TracingConstants.Metadata.SpanId).GetString()
			);
		} catch (Exception) {
			return TracingMetadata.None();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static Utf8JsonWriter WriteIfNotNull(
		this Utf8JsonWriter jsonWriter, string key, string? value
	) {
		if (string.IsNullOrEmpty(value)) return jsonWriter;

		jsonWriter.WritePropertyName(key);
		jsonWriter.WriteStringValue(value);

		return jsonWriter;
	}
}
