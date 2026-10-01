using Npgsql;

namespace ProfanityService;

public interface IProfanityRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<ProfanityResult> ValidateAsync(string text, CancellationToken cancellationToken);
}

public sealed class PostgresProfanityRepository(IConfiguration configuration, ILogger<PostgresProfanityRepository> logger) : IProfanityRepository
{
    private readonly IConfiguration _configuration = configuration;
    private readonly string _connectionString = configuration.GetConnectionString("ProfanityDatabase")
        ?? throw new InvalidOperationException("ConnectionStrings:ProfanityDatabase is required.");

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            CREATE TABLE IF NOT EXISTS blocked_words (
                word TEXT PRIMARY KEY
            );
            """, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        var blockedWords = (_configuration["BLOCKED_WORDS"] ?? "spam,scam")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var word in blockedWords)
        {
            await using var seed = new NpgsqlCommand("INSERT INTO blocked_words (word) VALUES ($1) ON CONFLICT DO NOTHING", connection);
            seed.Parameters.AddWithValue(word);
            await seed.ExecuteNonQueryAsync(cancellationToken);
        }
        logger.LogInformation("Profanity database initialized");
    }

    public async Task<ProfanityResult> ValidateAsync(string text, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT word FROM blocked_words", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var matches = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var word = reader.GetString(0);
            if (text.Contains(word, StringComparison.OrdinalIgnoreCase)) matches.Add(word);
        }
        return new ProfanityResult(matches.Count > 0, matches);
    }
}
