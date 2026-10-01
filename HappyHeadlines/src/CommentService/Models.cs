namespace CommentService;

public sealed record Comment(Guid Id, Guid ArticleId, string Author, string Body, DateTimeOffset CreatedAt);
public sealed record CreateCommentRequest(Guid ArticleId, string Author, string Body);
public sealed record ProfanityResult(bool IsProfane, IReadOnlyList<string> Matches);
public sealed record ValidationFailure(string Error, string Message);
