using Npgsql;

namespace DraftService;

public interface IDraftRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Draft>> GetAllAsync(CancellationToken cancellationToken);
    Task<Draft?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Draft> CreateAsync(Draft draft, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Draft draft, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class PostgresDraftRepository(IConfiguration configuration, ILogger<PostgresDraftRepository> logger) : IDraftRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("DraftDatabase")
        ?? throw new InvalidOperationException("ConnectionStrings:DraftDatabase is required.");

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            CREATE TABLE IF NOT EXISTS drafts (
                id UUID PRIMARY KEY,
                title TEXT NOT NULL,
                body TEXT NOT NULL,
                author TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            """, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("draft_database_initialized");
    }

    public async Task<IReadOnlyList<Draft>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT id, title, body, author, created_at, updated_at FROM drafts ORDER BY created_at DESC", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var drafts = new List<Draft>();
        while (await reader.ReadAsync(cancellationToken)) drafts.Add(Read(reader));
        return drafts;
    }

    public async Task<Draft?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT id, title, body, author, created_at, updated_at FROM drafts WHERE id = $1", connection);
        command.Parameters.AddWithValue(id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<Draft> CreateAsync(Draft draft, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("INSERT INTO drafts (id, title, body, author, created_at, updated_at) VALUES ($1, $2, $3, $4, $5, $6)", connection);
        AddParameters(command, draft);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return draft;
    }

    public async Task<bool> UpdateAsync(Draft draft, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("UPDATE drafts SET title = $2, body = $3, author = $4, updated_at = $6 WHERE id = $1", connection);
        AddParameters(command, draft);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("DELETE FROM drafts WHERE id = $1", connection);
        command.Parameters.AddWithValue(id);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static Draft Read(NpgsqlDataReader reader) => new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetFieldValue<DateTimeOffset>(4), reader.GetFieldValue<DateTimeOffset>(5));

    private static void AddParameters(NpgsqlCommand command, Draft draft)
    {
        command.Parameters.AddWithValue(draft.Id);
        command.Parameters.AddWithValue(draft.Title);
        command.Parameters.AddWithValue(draft.Body);
        command.Parameters.AddWithValue(draft.Author);
        command.Parameters.AddWithValue(draft.CreatedAt);
        command.Parameters.AddWithValue(draft.UpdatedAt);
    }
}
