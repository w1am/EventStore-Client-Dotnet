using EventStore.Client.Diagnostics.Tracing;

namespace EventStore.Client.Streams.Tests.Diagnostics;

[Trait("Category", "Diagnostics:Tracing")]
public class StreamsTracingInstrumentationTests(ITestOutputHelper output, DiagnosticsFixture fixture)
	: EventStoreTests<DiagnosticsFixture>(output, fixture) {
	[Fact]
	public async Task AppendIsInstrumentedWithTracingAsExpected() {
		var stream = Fixture.GetStreamName();

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEvents()
		);

		var activity = Fixture
			.GetActivitiesForOperation(TracingConstants.Operations.Append, stream)
			.SingleOrDefault()
			.ShouldNotBeNull();

		Fixture.AssertAppendActivityHasExpectedTags(activity, stream);
	}

	[Fact]
	public async Task AppendTraceIsTaggedWithErrorStatusOnException() {
		var stream = Fixture.GetStreamName();

		var actualException = await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			Fixture.CreateTestEventsThatThrowsException()
		).ShouldThrowAsync<Exception>();

		var activity = Fixture
			.GetActivitiesForOperation(TracingConstants.Operations.Append, stream)
			.SingleOrDefault()
			.ShouldNotBeNull();

		Fixture.AssertErroneousAppendActivityHasExpectedTags(activity, actualException);
	}

	[Fact]
	public async Task CatchupSubscriptionIsInstrumentedWithTracingAndRestoresRemoteAppendContextAsExpected() {
		var stream = Fixture.GetStreamName();
		var events = Fixture.CreateTestEventsWithWorkaroundCustomMetadata(2).ToArray();

		await Fixture.Streams.AppendToStreamAsync(
			stream,
			StreamState.NoStream,
			events
		);

		string? subscriptionId = null;
		await Subscribe().WithTimeout();

		var appendActivity = Fixture
			.GetActivitiesForOperation(TracingConstants.Operations.Append, stream)
			.SingleOrDefault()
			.ShouldNotBeNull();

		var subscribeActivities = Fixture
			.GetActivitiesForOperation(TracingConstants.Operations.Subscribe, stream)
			.ToArray();

		subscriptionId.ShouldNotBeNull();
		subscribeActivities.Length.ShouldBe(events.Length);

		for (var i = 0; i < subscribeActivities.Length; i++) {
			subscribeActivities[i].TraceId.ShouldBe(appendActivity.Context.TraceId);
			subscribeActivities[i].ParentSpanId.ShouldBe(appendActivity.Context.SpanId);
			subscribeActivities[i].HasRemoteParent.ShouldBeTrue();

			Fixture.AssertSubscriptionActivityHasExpectedTags(
				subscribeActivities[i],
				stream,
				events[i].EventId.ToString(),
				subscriptionId
			);
		}

		return;

		async Task Subscribe() {
			await using var subscription = Fixture.Streams.SubscribeToStream(stream, FromStream.Start);
			await using var enumerator   = subscription.Messages.GetAsyncEnumerator();

			int eventsAppeared = 0;
			while (await enumerator.MoveNextAsync()) {
				if (enumerator.Current is StreamMessage.SubscriptionConfirmation(var sid))
					subscriptionId = sid;

				if (enumerator.Current is not StreamMessage.Event(_))
					continue;

				eventsAppeared++;
				if (eventsAppeared >= events.Length)
					return;
			}
		}
	}
}
