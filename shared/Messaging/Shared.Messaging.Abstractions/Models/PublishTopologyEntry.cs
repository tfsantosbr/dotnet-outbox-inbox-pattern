namespace Shared.Messaging.Abstractions.Models;

public sealed record PublishTopologyEntry(Type MessageType, PublishOptions Options);