namespace Shared.Messaging.Abstractions;

public sealed record PublishTopologyEntry(Type MessageType, PublishOptions Options);
