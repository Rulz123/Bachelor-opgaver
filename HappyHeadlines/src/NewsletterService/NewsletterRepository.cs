using Npgsql;
namespace NewsletterService;
public interface INewsletterRepository { Task InitializeAsync(CancellationToken ct); Task HandleAsync(Guid messageId, Guid articleId, string title, CancellationToken ct); Task<int> CountAsync(Guid articleId, CancellationToken ct); }
public sealed class PostgresNewsletterRepository(IConfiguration config) : INewsletterRepository
{
    private readonly string _connectionString = config.GetConnectionString("NewsletterDatabase") ?? throw new InvalidOperationException("ConnectionStrings:NewsletterDatabase is required.");
    public async Task InitializeAsync(CancellationToken ct) { await using var c = await OpenAsync(ct); await using var cmd = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS newsletter_deliveries (message_id UUID PRIMARY KEY, article_id UUID NOT NULL, title TEXT NOT NULL, delivered_at TIMESTAMPTZ NOT NULL); DELETE FROM newsletter_deliveries older USING newsletter_deliveries newer WHERE older.article_id = newer.article_id AND older.ctid > newer.ctid; CREATE UNIQUE INDEX IF NOT EXISTS ux_newsletter_deliveries_article_id ON newsletter_deliveries(article_id);", c); await cmd.ExecuteNonQueryAsync(ct); }
    public async Task HandleAsync(Guid messageId, Guid articleId, string title, CancellationToken ct) { await using var c = await OpenAsync(ct); await using var cmd = new NpgsqlCommand("INSERT INTO newsletter_deliveries (message_id, article_id, title, delivered_at) VALUES ($1,$2,$3,$4) ON CONFLICT (article_id) DO NOTHING", c); cmd.Parameters.AddWithValue(messageId); cmd.Parameters.AddWithValue(articleId); cmd.Parameters.AddWithValue(title); cmd.Parameters.AddWithValue(DateTimeOffset.UtcNow); await cmd.ExecuteNonQueryAsync(ct); }
    public async Task<int> CountAsync(Guid articleId, CancellationToken ct) { await using var c = await OpenAsync(ct); await using var cmd = new NpgsqlCommand("SELECT count(*) FROM newsletter_deliveries WHERE article_id=$1", c); cmd.Parameters.AddWithValue(articleId); return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)); }
    private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct) { var c = new NpgsqlConnection(_connectionString); await c.OpenAsync(ct); return c; }
}
