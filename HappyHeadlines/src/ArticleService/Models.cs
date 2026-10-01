namespace ArticleService;

public sealed record Article(
    Guid Id,
    string Title,
    string Body,
    string? Continent,
    bool IsGlobal,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateArticleRequest(
    string Title,
    string Body,
    string? Continent,
    bool IsGlobal);

public sealed record UpdateArticleRequest(
    string Title,
    string Body,
    string? Continent,
    bool IsGlobal);
