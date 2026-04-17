namespace Shared.Inbox.Abstractions.Models;

public readonly record struct InboxRegistrationResult
{
    public InboxRegistrationStatus Status { get; init; }
    public bool IsRegistered => Status == InboxRegistrationStatus.Registered;
    public bool IsDuplicate => Status == InboxRegistrationStatus.Duplicate;

    public static InboxRegistrationResult Registered() =>
        new() { Status = InboxRegistrationStatus.Registered };

    public static InboxRegistrationResult Duplicate() =>
        new() { Status = InboxRegistrationStatus.Duplicate };
}