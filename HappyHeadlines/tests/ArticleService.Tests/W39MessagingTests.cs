using System.Diagnostics;
using HappyHeadlines.Messaging;
using Xunit;

namespace ArticleService.Tests;

public class W39MessagingTests
{
    [Fact]
    public void Rabbit_headers_preserve_W3C_trace_context()
    {
        using var activity = new Activity("publish").SetIdFormat(ActivityIdFormat.W3C).Start();
        var headers = new Dictionary<string, object>();
        RabbitTraceContext.AddHeaders(headers, activity);
        var parent = RabbitTraceContext.Extract(headers);
        Assert.Equal(activity.TraceId, parent.TraceId);
        Assert.Equal(activity.SpanId, parent.SpanId);
    }

    [Fact]
    public void Rabbit_byte_headers_preserve_trace_state()
    {
        using var activity = new Activity("publish").SetIdFormat(ActivityIdFormat.W3C).Start();
        var headers = new Dictionary<string, object>();
        RabbitTraceContext.AddHeaders(headers, activity);
        var wireHeaders = headers.ToDictionary(pair => pair.Key, pair => (object)System.Text.Encoding.UTF8.GetBytes(pair.Value.ToString()!));
        var parent = RabbitTraceContext.Extract(wireHeaders);
        Assert.Equal(activity.TraceId, parent.TraceId);
        Assert.Equal(activity.TraceStateString, parent.TraceState);
    }

    [Fact]
    public void Retry_policy_is_bounded_and_dead_letters_after_two_retries()
    {
        Assert.True(ArticleQueueRetryPolicy.ShouldRetry(0));
        Assert.True(ArticleQueueRetryPolicy.ShouldRetry(1));
        Assert.False(ArticleQueueRetryPolicy.ShouldRetry(2));
        Assert.Equal("newsletter-service.published.v2.dead", ArticleQueueRetryPolicy.DeadLetterQueue(ArticleQueueNames.NewsletterQueue));
    }

    [Fact]
    public void Retry_counter_decodes_wire_bytes_and_retry_keeps_trace_parent()
    {
        using var activity = new Activity("retry").SetIdFormat(ActivityIdFormat.W3C).Start();
        IDictionary<string, object> incoming = new Dictionary<string, object>
        {
            ["x-retry-count"] = System.Text.Encoding.UTF8.GetBytes("1"),
            ["traceparent"] = System.Text.Encoding.UTF8.GetBytes(activity.Id!)
        };

        Assert.Equal(1, ArticleQueueRetryPolicy.ReadRetryCount(incoming));
        var retryHeaders = ArticleQueueRetryPolicy.CreateRetryHeaders(incoming, 1, activity);
        Assert.Equal(2, ArticleQueueRetryPolicy.ReadRetryCount(retryHeaders));
        Assert.Equal(activity.TraceId, RabbitTraceContext.Extract(retryHeaders).TraceId);
    }

    [Fact]
    public void Published_message_has_stable_message_and_article_ids_for_redelivery()
    {
        var messageId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var first = new ArticlePublishedMessage(messageId, articleId, "Title", "Body", "Europe", false, DateTimeOffset.UtcNow);
        var redelivery = first with { };
        Assert.Equal(first.MessageId, redelivery.MessageId);
        Assert.Equal(first.ArticleId, redelivery.ArticleId);
    }

    [Fact]
    public void Queue_names_include_dead_letter_destination()
    {
        Assert.Equal("article.published", ArticleQueueNames.Exchange);
        Assert.Equal("article-service.published.v2", ArticleQueueNames.ArticleQueue);
        Assert.Equal("newsletter-service.published.v2", ArticleQueueNames.NewsletterQueue);
        Assert.Equal("article.dead.v2", ArticleQueueNames.DeadLetterExchange);
    }
}
