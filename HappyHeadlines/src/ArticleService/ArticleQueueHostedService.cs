using HappyHeadlines.Messaging;

namespace ArticleService;

public sealed class ArticleQueueHostedService(
    RabbitMqConnection connection,
    IArticleRepository repository,
    IArticleCache cache,
    ILogger<ArticleQueueHostedService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new ArticleQueueConsumer(connection, ArticleQueueNames.ArticleQueue, HandleAsync, logger);
        return consumer.StartAsync(stoppingToken);
    }

    private async Task HandleAsync(ArticlePublishedMessage message, CancellationToken cancellationToken)
    {
        var article = new Article(message.ArticleId, message.Title, message.Body, message.Continent, message.IsGlobal, message.PublishedAt, message.PublishedAt);
        try
        {
            await repository.CreateAsync(article, cancellationToken);
            cache.Upsert(article);
        }
        catch (Npgsql.PostgresException exception) when (exception.SqlState == "23505")
        {
            logger.LogInformation("duplicate_article_message_ignored message_id={MessageId}", message.MessageId);
            var existing = await repository.GetAsync(message.ArticleId, cancellationToken);
            if (existing is not null) cache.Upsert(existing);
        }
    }
}
