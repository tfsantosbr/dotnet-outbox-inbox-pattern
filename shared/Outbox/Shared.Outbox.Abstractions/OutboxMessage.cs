using System.Text.Json;

namespace Shared.Outbox.Abstractions;

public class OutboxMessage
{
    private OutboxMessage(
        Guid id,
        string type,
        string destination,
        string content,
        DateTime occurredOn,
        string? headers
    )
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredOn = occurredOn;
        Headers = headers;
        Destination = destination;
    }

    private OutboxMessage() { }

    public Guid Id { get; init; }
    public string? Headers { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Destination { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime OccurredOn { get; init; }
    public DateTime? ProcessedOn { get; private set; }
    public DateTime? ErrorHandledOn { get; private set; }
    public string? Error { get; private set; }

    public string GetTypeName()
    {
        ReadOnlySpan<char> span = Type;

        span = span[..span.IndexOf(',')].Trim();

        return span[(span.LastIndexOf('.') + 1)..].ToString();
    }

    public void MarkAsProcessedWithSuccess()
    {
        ProcessedOn = DateTime.UtcNow;
        Error = null;
        ErrorHandledOn = null;
    }

    public void MarkAsProcessedWithError(string error)
    {
        ProcessedOn = DateTime.UtcNow;
        ErrorHandledOn = DateTime.UtcNow;
        Error = error;
    }

    public static OutboxMessage Create<TContent>(
        string destination,
        Guid id,
        TContent content,
        DateTime occurredOn,
        IDictionary<string, string>? headers = null
    )
    {
        string? headersJson = null;

        if (headers is not null)
        {
            headersJson = JsonSerializer.Serialize(headers);
        }

        var contentType =
            content!.GetType().FullName + ", " + content.GetType().Assembly.GetName().Name;
        var contentJson = JsonSerializer.Serialize(content);

        return new OutboxMessage(
            id,
            contentType,
            destination,
            contentJson,
            occurredOn,
            headersJson
        );
    }

    public Dictionary<string, string>? GetHeaders()
    {
        return string.IsNullOrEmpty(Headers)
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, string>>(Headers);
    }
}
