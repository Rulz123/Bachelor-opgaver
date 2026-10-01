namespace PublisherService;
public sealed record PublishArticleRequest(Guid ArticleId, string Title, string Body, string Continent, bool IsGlobal);
