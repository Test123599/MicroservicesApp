namespace Shared.Messages;

public record ProductCreated(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    DateTimeOffset CreatedAt);
