namespace Shared.Messaging.Abstractions.Models;

public class PublishOptions
{
    public string Destination { get; set; } = string.Empty;

    public virtual void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Destination))
            errors.Add("Destination is required.");

        if (errors.Count > 0)
            throw new ArgumentException(
                $"Invalid {GetType().Name}: {string.Join(" ", errors)}");
    }
}