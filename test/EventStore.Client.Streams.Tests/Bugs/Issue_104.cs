namespace EventStore.Client.Streams.Tests.Bugs;

public class Issue_104 : IClassFixture<EventStoreFixture> {
	public Issue_104(ITestOutputHelper output, EventStoreFixture fixture) =>
		Fixture = fixture.With(x => x.CaptureTestRun(output));

	EventStoreFixture Fixture { get; }

	[Fact]
	public async Task subscription_does_not_send_checkpoint_reached_after_disposal() {
		var streamName                   = Fixture.GetStreamName();
		var ignoredStreamName            = $"ignore_{streamName}";
		var subscriptionDisposed         = new TaskCompletionSource<bool>();
		var eventAppeared                = new TaskCompletionSource<bool>();
		var checkpointReachAfterDisposed = new TaskCompletionSource<bool>();

		await Fixture.Streams.AppendToStreamAsync(streamName, StreamRevision.None, Fixture.CreateTestEvents());

		var subscription = await Fixture.Streams.SubscribeToAllAsync(
			FromAll.Start,
			(_, _, _) => {
				eventAppeared.TrySetResult(true);
				return Task.CompletedTask;
			},
			false,
			(_, _, _) => subscriptionDisposed.TrySetResult(true),
			new(
				StreamFilter.Prefix(streamName),
				1,
				(_, _, _) => {
					if (!subscriptionDisposed.Task.IsCompleted)
						return Task.CompletedTask;

					checkpointReachAfterDisposed.TrySetResult(true);
					return Task.CompletedTask;
				}
			),
			new("admin", "changeit")
		);

		await eventAppeared.Task;

		subscription.Dispose();
		await subscriptionDisposed.Task;

		await Fixture.Streams.AppendToStreamAsync(
			ignoredStreamName,
			StreamRevision.None,
			Fixture.CreateTestEvents(50)
		);

		var delay  = Task.Delay(300);
		var result = await Task.WhenAny(delay, checkpointReachAfterDisposed.Task);
		Assert.Equal(delay, result); // iow 300ms have passed without seeing checkpointReachAfterDisposed
	}
}