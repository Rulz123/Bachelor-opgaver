using Npgsql;

namespace CommentService;

public interface ICommentRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<Comment> CreateAsync(Comment comment, CancellationToken cancellationToken);
    Task<IReadOnlyList<Comment>> GetByArticleAsync(Guid articleId, CancellationToken cancellationToken);
}

public sealed class PostgresCommentRepository(IConfiguration configuration, ILogger<PostgresCommentRepository> logger) : ICommentRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("CommentDatabase")
        ?? throw new InvalidOperationException("ConnectionStrings:CommentDatabase is required.");

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            CREATE TABLE IF NOT EXISTS comments (
                id UUID PRIMARY KEY,
                article_id UUID NOT NULL,
                author TEXT NOT NULL,
                body TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL
            );
            """, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("Comment database initialized");
    }

    public async Task<Comment> CreateAsync(Comment comment, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("INSERT INTO comments (id, article_id, author, body, created_at) VALUES ($1, $2, $3, $4, $5)", connection);
        command.Parameters.AddWithValue(comment.Id);
        command.Parameters.AddWithValue(comment.ArticleId);
        command.Parameters.AddWithValue(comment.Author);
        command.Parameters.AddWithValue(comment.Body);
        command.Parameters.AddWithValue(comment.CreatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return comment;
    }

    public async Task<IReadOnlyList<Comment>> GetByArticleAsync(Guid articleId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT id, article_id, author, body, created_at FROM comments WHERE article_id = $1 ORDER BY created_at", connection);
        command.Parameters.AddWithValue(articleId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var comments = new List<Comment>();
        while (await reader.ReadAsync(cancellationToken))
        {
            comments.Add(new Comment(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.GetFieldValue<DateTimeOffset>(4)));
        }
        return comments;
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
