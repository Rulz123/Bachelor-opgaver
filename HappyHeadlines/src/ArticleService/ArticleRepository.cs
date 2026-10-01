using Npgsql;

namespace ArticleService;

public interface IArticleRepository
{
    Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken cancellationToken);
    Task<Article?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Article> CreateAsync(Article article, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Article article, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class PostgresArticleRepository(IConfiguration configuration, ILogger<PostgresArticleRepository> logger) : IArticleRepository
{
    private readonly IReadOnlyDictionary<string, string> _connectionStrings = ArticleRouter.DatabaseKeys
        .ToDictionary(key => key, key => configuration.GetConnectionString(key)
            ?? throw new InvalidOperationException($"ConnectionStrings:{key} is required."), StringComparer.OrdinalIgnoreCase);

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        foreach (var connectionString in _connectionStrings.Values)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("""
                CREATE TABLE IF NOT EXISTS articles (
                    id UUID PRIMARY KEY,
                    title TEXT NOT NULL,
                    body TEXT NOT NULL,
                    continent TEXT NULL,
                    is_global BOOLEAN NOT NULL,
                    created_at TIMESTAMPTZ NOT NULL,
                    updated_at TIMESTAMPTZ NOT NULL
                );
                """, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        logger.LogInformation("Article database initialized");
    }

    public async Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken cancellationToken)
    {
        var articles = new List<Article>();
        foreach (var connectionString in _connectionStrings.Values)
        {
            await using var connection = await OpenConnectionAsync(connectionString, cancellationToken);
            await using var command = new NpgsqlCommand("SELECT id, title, body, continent, is_global, created_at, updated_at FROM articles ORDER BY created_at DESC", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) articles.Add(ReadArticle(reader));
        }
        return articles;
    }

    public async Task<Article?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        foreach (var connectionString in _connectionStrings.Values)
        {
            await using var connection = await OpenConnectionAsync(connectionString, cancellationToken);
            await using var command = new NpgsqlCommand("SELECT id, title, body, continent, is_global, created_at, updated_at FROM articles WHERE id = $1", connection);
            command.Parameters.AddWithValue(id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken)) return ReadArticle(reader);
        }
        return null;
    }

    public async Task<Article> CreateAsync(Article article, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO articles (id, title, body, continent, is_global, created_at, updated_at) VALUES ($1, $2, $3, $4, $5, $6, $7)";
        await using var connection = await OpenAsync(ArticleRouter.GetDatabaseKey(article.IsGlobal, article.Continent), cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AddParameters(command, article);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return article;
    }

    public async Task<bool> UpdateAsync(Article article, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE articles SET title = $2, body = $3, continent = $4, is_global = $5, updated_at = $7 WHERE id = $1";
        foreach (var connectionString in _connectionStrings.Values)
        {
            await using var connection = await OpenConnectionAsync(connectionString, cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            AddParameters(command, article);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 1) return true;
        }
        return false;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        foreach (var connectionString in _connectionStrings.Values)
        {
            await using var connection = await OpenConnectionAsync(connectionString, cancellationToken);
            await using var command = new NpgsqlCommand("DELETE FROM articles WHERE id = $1", connection);
            command.Parameters.AddWithValue(id);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 1) return true;
        }
        return false;
    }

    private async Task<NpgsqlConnection> OpenAsync(string databaseKey, CancellationToken cancellationToken)
    {
        return await OpenConnectionAsync(_connectionStrings[databaseKey], cancellationToken);
    }

    private static async Task<NpgsqlConnection> OpenConnectionAsync(string connectionString, CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static Article ReadArticle(NpgsqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3),
        reader.GetBoolean(4), reader.GetFieldValue<DateTimeOffset>(5), reader.GetFieldValue<DateTimeOffset>(6));

    private static void AddParameters(NpgsqlCommand command, Article article)
    {
        command.Parameters.AddWithValue(article.Id);
        command.Parameters.AddWithValue(article.Title);
        command.Parameters.AddWithValue(article.Body);
        command.Parameters.AddWithValue((object?)article.Continent ?? DBNull.Value);
        command.Parameters.AddWithValue(article.IsGlobal);
        command.Parameters.AddWithValue(article.CreatedAt);
        command.Parameters.AddWithValue(article.UpdatedAt);
    }
}
