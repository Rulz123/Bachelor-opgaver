namespace CommentService;

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

public sealed class ProfanityUnavailableException(string message, Exception? innerException = null) : Exception(message, innerException);
public sealed class CircuitOpenException(string message) : Exception(message);

public sealed class CircuitBreakerOptions
{
    public int FailureThreshold { get; init; } = 3;
    public TimeSpan BreakDuration { get; init; } = TimeSpan.FromSeconds(5);
}

public sealed class CircuitBreaker(CircuitBreakerOptions options)
{
    private readonly object _gate = new();
    private CircuitState _state = CircuitState.Closed;
    private int _failures;
    private DateTimeOffset _openedAt;
    private bool _halfOpenProbe;

    public CircuitState State { get { lock (_gate) return _state; } }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_state == CircuitState.Open)
            {
                if (DateTimeOffset.UtcNow - _openedAt < options.BreakDuration) throw new CircuitOpenException("ProfanityService circuit is open.");
                _state = CircuitState.HalfOpen;
                _halfOpenProbe = false;
            }
            if (_state == CircuitState.HalfOpen && _halfOpenProbe) throw new CircuitOpenException("ProfanityService circuit is half-open and probing.");
            if (_state == CircuitState.HalfOpen) _halfOpenProbe = true;
        }

        try
        {
            var result = await operation(cancellationToken);
            lock (_gate)
            {
                _state = CircuitState.Closed;
                _failures = 0;
                _halfOpenProbe = false;
            }
            return result;
        }
        catch
        {
            lock (_gate)
            {
                _halfOpenProbe = false;
                _failures++;
                if (_state == CircuitState.HalfOpen || _failures >= options.FailureThreshold)
                {
                    _state = CircuitState.Open;
                    _openedAt = DateTimeOffset.UtcNow;
                }
            }
            throw;
        }
    }
}

public interface IProfanityClient
{
    Task<ProfanityResult> ValidateAsync(string text, CancellationToken cancellationToken);
}

public sealed class HttpProfanityClient(HttpClient client, IConfiguration configuration, ILogger<HttpProfanityClient> logger) : IProfanityClient
{
    private readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(configuration.GetValue("PROFANITY_TIMEOUT_MS", 250));
    private readonly int _maxAttempts = Math.Max(1, configuration.GetValue("PROFANITY_MAX_ATTEMPTS", 3));

    public async Task<ProfanityResult> ValidateAsync(string text, CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_timeout);
            try
            {
                using var response = await client.PostAsJsonAsync("/validate", new { text }, timeout.Token);
                if (!response.IsSuccessStatusCode) throw new HttpRequestException($"ProfanityService returned {(int)response.StatusCode}.");
                return await response.Content.ReadFromJsonAsync<ProfanityResult>(cancellationToken: timeout.Token)
                    ?? throw new HttpRequestException("ProfanityService returned an empty response.");
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                lastException = exception;
                logger.LogWarning(exception, "ProfanityService attempt {Attempt}/{MaxAttempts} failed", attempt, _maxAttempts);
                if (attempt < _maxAttempts) await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken);
            }
        }
        throw new ProfanityUnavailableException("ProfanityService validation failed after bounded retries.", lastException);
    }
}
