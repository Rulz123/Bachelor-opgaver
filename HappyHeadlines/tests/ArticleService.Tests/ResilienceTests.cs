using System.Net;
using System.Net.Http.Json;
using CommentService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArticleService.Tests;

public class ResilienceTests
{
    [Fact]
    public async Task Timeout_retries_with_bounded_attempts()
    {
        var handler = new CountingHandler(_ => throw new TaskCanceledException("timeout"));
        var client = CreateClient(handler, attempts: 3, timeoutMs: 10);
        var exception = await Assert.ThrowsAsync<ProfanityUnavailableException>(() => client.ValidateAsync("hello", CancellationToken.None));
        Assert.Contains("bounded retries", exception.Message);
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public async Task Retry_exhaustion_is_reported_as_unavailable()
    {
        var handler = new CountingHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = CreateClient(handler, attempts: 2, timeoutMs: 100);
        await Assert.ThrowsAsync<ProfanityUnavailableException>(() => client.ValidateAsync("hello", CancellationToken.None));
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task Circuit_opens_after_threshold()
    {
        var breaker = new CircuitBreaker(new CircuitBreakerOptions { FailureThreshold = 2, BreakDuration = TimeSpan.FromSeconds(1) });
        await Assert.ThrowsAsync<InvalidOperationException>(() => breaker.ExecuteAsync<int>(_ => throw new InvalidOperationException(), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => breaker.ExecuteAsync<int>(_ => throw new InvalidOperationException(), CancellationToken.None));
        Assert.Equal(CircuitState.Open, breaker.State);
        await Assert.ThrowsAsync<CircuitOpenException>(() => breaker.ExecuteAsync(_ => Task.FromResult(1), CancellationToken.None));
    }

    [Fact]
    public async Task Half_open_probe_recovers_successfully()
    {
        var breaker = new CircuitBreaker(new CircuitBreakerOptions { FailureThreshold = 1, BreakDuration = TimeSpan.FromMilliseconds(20) });
        await Assert.ThrowsAsync<InvalidOperationException>(() => breaker.ExecuteAsync<int>(_ => throw new InvalidOperationException(), CancellationToken.None));
        await Task.Delay(40);
        var value = await breaker.ExecuteAsync(_ => Task.FromResult(42), CancellationToken.None);
        Assert.Equal(42, value);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public async Task Successful_recovery_closes_circuit_and_allows_next_request()
    {
        var breaker = new CircuitBreaker(new CircuitBreakerOptions { FailureThreshold = 1, BreakDuration = TimeSpan.FromMilliseconds(15) });
        await Assert.ThrowsAsync<InvalidOperationException>(() => breaker.ExecuteAsync<int>(_ => throw new InvalidOperationException(), CancellationToken.None));
        await Task.Delay(30);
        await breaker.ExecuteAsync(_ => Task.FromResult(true), CancellationToken.None);
        var next = await breaker.ExecuteAsync(_ => Task.FromResult("accepted"), CancellationToken.None);
        Assert.Equal("accepted", next);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    private static HttpProfanityClient CreateClient(HttpMessageHandler handler, int attempts, int timeoutMs)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PROFANITY_MAX_ATTEMPTS"] = attempts.ToString(),
            ["PROFANITY_TIMEOUT_MS"] = timeoutMs.ToString()
        }).Build();
        return new HttpProfanityClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }, configuration, NullLogger<HttpProfanityClient>.Instance);
    }

    private sealed class CountingHandler(Func<HttpRequestMessage, HttpResponseMessage> response)
        : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(response(request));
        }
    }
}
