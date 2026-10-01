using HappyHeadlines.Messaging;
namespace NewsletterService;
public sealed class NewsletterQueueHostedService(RabbitMqConnection connection, INewsletterRepository repository, ILogger<NewsletterQueueHostedService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => new ArticleQueueConsumer(connection, ArticleQueueNames.NewsletterQueue, HandleAsync, logger).StartAsync(stoppingToken);
    private Task HandleAsync(ArticlePublishedMessage message, CancellationToken ct) => repository.HandleAsync(message.MessageId, message.ArticleId, message.Title, ct);
}
