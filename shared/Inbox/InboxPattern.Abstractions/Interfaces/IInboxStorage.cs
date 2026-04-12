using InboxPattern.Abstractions.Models;

namespace InboxPattern.Abstractions.Interfaces;

/// <summary>
/// Low-level inbox store. Callers must open an EF Core transaction before calling
/// <see cref="TryRegisterAsync"/> to ensure atomicity with the business operation.
/// </summary>
public interface IInboxStorage
{
    Task<InboxRegistrationResult> TryRegisterAsync(
        InboxMessage message,
        CancellationToken cancellationToken = default);
}
