namespace DraftService;

public sealed record Draft(Guid Id, string Title, string Body, string Author, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record CreateDraftRequest(string Title, string Body, string Author);
public sealed record UpdateDraftRequest(string Title, string Body, string Author);
