using System.Diagnostics;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Configuration;

namespace HappyHeadlines.Messaging;

public sealed record ArticlePublishedMessage(Guid MessageId, Guid ArticleId, string Title, string Body, string Continent, bool IsGlobal, DateTimeOffset PublishedAt);
public sealed record ArticleTextValidationResult(bool IsProfane, IReadOnlyList<string> Matches);

public interface IPublishedArticlePublisher
{
    Task PublishAsync(ArticlePublishedMessage message, CancellationToken cancellationToken);
}

public static class ArticleQueueNames
{
    public const string Exchange = "article.published";
    public const string ArticleQueue = "article-service.published.v2";
    public const string NewsletterQueue = "newsletter-service.published.v2";
    public const string DeadLetterExchange = "article.dead.v2";
}

public static class ArticleQueueRetryPolicy
{
    public const int MaximumRetries = 2;

    public static bool ShouldRetry(int retryCount) => retryCount < MaximumRetries;

    public static string DeadLetterQueue(string queueName) => queueName + ".dead";

    public static int ReadRetryCount(IDictionary<string, object>? headers)
    {
        if (headers is null || !headers.TryGetValue("x-retry-count", out var value)) return 0;
        var text = value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> memory => Encoding.UTF8.GetString(memory.Span),
            _ => value.ToString() ?? string.Empty
        };
        return int.TryParse(text, out var count) ? count : 0;
    }

    public static IDictionary<string, object> CreateRetryHeaders(IDictionary<string, object>? headers, int retryCount, Activity? activity)
    {
        var result = headers is null ? new Dictionary<string, object>() : new Dictionary<string, object>(headers);
        result["x-retry-count"] = Encoding.UTF8.GetBytes((retryCount + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        RabbitTraceContext.AddHeaders(result, activity);
        return result;
    }
}

public static class RabbitTraceContext
{
    public static void AddHeaders(IDictionary<string, object> headers, Activity? activity)
    {
        if (activity?.Id is not null) headers["traceparent"] = activity.Id;
        if (!string.IsNullOrWhiteSpace(activity?.TraceStateString)) headers["tracestate"] = activity.TraceStateString!;
    }

    public static ActivityContext Extract(IDictionary<string, object>? headers)
    {
        if (headers is null || !headers.TryGetValue("traceparent", out var value)) return default;
        var traceparent = HeaderValue(value);
        var traceState = headers.TryGetValue("tracestate", out var state) ? HeaderValue(state) : null;
        return ActivityContext.TryParse(traceparent, traceState, out var context) ? context : default;
    }

    private static string HeaderValue(object value) => value switch
    {
        byte[] bytes => Encoding.UTF8.GetString(bytes),
        ReadOnlyMemory<byte> memory => Encoding.UTF8.GetString(memory.Span),
        _ => value.ToString() ?? string.Empty
    };
}

public sealed class RabbitMqConnection(IConfiguration configuration) : IAsyncDisposable
{
    private readonly ConnectionFactory _factory = new()
    {
        HostName = configuration["RABBITMQ_HOST"] ?? "article-queue",
        UserName = configuration["RABBITMQ_USER"] ?? throw new InvalidOperationException("RABBITMQ_USER is required."),
        Password = configuration["RABBITMQ_PASSWORD"] ?? throw new InvalidOperationException("RABBITMQ_PASSWORD is required."),
        DispatchConsumersAsync = true,
        AutomaticRecoveryEnabled = true
    };
    public IConnection? Connection { get; private set; }
    public Task<IConnection> GetAsync(CancellationToken cancellationToken)
    {
        if (Connection is { IsOpen: true }) return Task.FromResult(Connection);
        return ConnectWithRetryAsync(cancellationToken);
    }
    private async Task<IConnection> ConnectWithRetryAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            try { Connection = _factory.CreateConnection(); return Connection; }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException) when (!cancellationToken.IsCancellationRequested) { await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); }
        }
    }
    public ValueTask DisposeAsync() { Connection?.Dispose(); return ValueTask.CompletedTask; }
}

public sealed class ArticleQueuePublisher(RabbitMqConnection connection) : IPublishedArticlePublisher
{
    public async Task PublishAsync(ArticlePublishedMessage message, CancellationToken cancellationToken)
    {
        using var channel = (await connection.GetAsync(cancellationToken)).CreateModel();
        channel.ConfirmSelect();
        ArticleQueueTopology.Ensure(channel, ArticleQueueNames.ArticleQueue);
        ArticleQueueTopology.Ensure(channel, ArticleQueueNames.NewsletterQueue);
        var headers = new Dictionary<string, object>();
        RabbitTraceContext.AddHeaders(headers, Activity.Current);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true; properties.MessageId = message.MessageId.ToString(); properties.Headers = headers;
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        channel.BasicPublish(ArticleQueueNames.Exchange, string.Empty, mandatory: true, basicProperties: properties, body: body);
        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
    }
}

public static class ArticleQueueTopology
{
    public static void Ensure(IModel channel, string queueName)
    {
        channel.ExchangeDeclare(ArticleQueueNames.Exchange, ExchangeType.Fanout, durable: true);
        channel.ExchangeDeclare(ArticleQueueNames.DeadLetterExchange, ExchangeType.Direct, durable: true);
        var arguments = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = ArticleQueueNames.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = queueName
        };
        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments);
        channel.QueueBind(queueName, ArticleQueueNames.Exchange, string.Empty);
        var deadQueue = ArticleQueueRetryPolicy.DeadLetterQueue(queueName);
        channel.QueueDeclare(deadQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(deadQueue, ArticleQueueNames.DeadLetterExchange, queueName);
    }
}
