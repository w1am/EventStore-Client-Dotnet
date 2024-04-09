using System.Diagnostics;
using System.Runtime.CompilerServices;
using EventStore.Client.Diagnostics.Telemetry;

namespace EventStore.Client.Diagnostics;

static class ActivityTagsCollectionExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ActivityTagsCollection WithTagsFrom(
		this ActivityTagsCollection tags, ChannelInfo? channelInfo, EventStoreClientSettings settings
	) {
		ActivityTagsCollection? serverTags = null;
		if (settings.ConnectivitySettings.DnsGossipSeeds?.Length == 1) {
			// Ensure consistent server.address attribute when connecting to cluster via dns discovery
			var gossipSeed = settings.ConnectivitySettings.DnsGossipSeeds[0];
			serverTags = CreateServerAttributes(gossipSeed.Host, gossipSeed.Port);
		} else if (channelInfo != null) {
			// Otherwise use the current gRPC channel target
			var authorityParts = channelInfo.Channel.Target.Split(':');
			serverTags = CreateServerAttributes(authorityParts[0], int.Parse(authorityParts[1]));
		}

		return tags.WithTags(serverTags).WithTagsFrom(settings.DefaultCredentials);

		ActivityTagsCollection CreateServerAttributes(string? host, int? port) => new() {
			{ TelemetryAttributes.ServerAddress, host },
			{ TelemetryAttributes.ServerPort, port }
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ActivityTagsCollection WithTagsFrom(
		this ActivityTagsCollection tags, UserCredentials? userCredentials
	) {
		return tags.WithTag(TelemetryAttributes.DatabaseUser, userCredentials?.Username);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ActivityTagsCollection WithTags(this ActivityTagsCollection current, ActivityTagsCollection? tags)
		=> tags == null
			? current
			: tags.Aggregate(current, (newTags, tag) => newTags.WithTag(tag.Key, tag.Value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static ActivityTagsCollection WithTag(this ActivityTagsCollection tags, string key, object? value) {
		if (value != null)
			tags[key] = value;

		return tags;
	}
}
