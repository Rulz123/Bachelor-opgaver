using System.Net.Http.Json;
using HappyHeadlines.Messaging;
using HappyHeadlines.Observability;

namespace PublisherService;

public interface IArticleTextValidator
{
    Task<ArticleTextValidationResult> ValidateAsync(string text, CancellationToken cancellationToken);
}

public sealed class HttpArticleTextValidator(HttpClient client) : IArticleTextValidator
{
    public async Task<ArticleTextValidationResult> ValidateAsync(string text, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(750));
        using var response = await client.PostAsJsonAsync("/validate", new { text }, timeout.Token);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"ProfanityService returned {(int)response.StatusCode}.");
        return await response.Content.ReadFromJsonAsync<ArticleTextValidationResult>(cancellationToken: timeout.Token)
            ?? throw new HttpRequestException("ProfanityService returned an empty validation response.");
    }
}