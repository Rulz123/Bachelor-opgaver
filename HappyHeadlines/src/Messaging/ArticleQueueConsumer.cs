using System.Diagnostics;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HappyHeadlines.Messaging;

public sealed class ArticleQueueConsumer(
    RabbitMqConnection connection,
    string queueName,
    Func<ArticlePublishedMessage, CancellationToken, Task> handler,
    ILogger logger) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new(HappyHeadlines.Observability.ObservabilityExtensions.ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = (await connection.GetAsync(stoppingToken)).CreateModel();
        ArticleQueueTopology.Ensure(channel, queueName);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) =>
        {
            Activity? activity = null;
            try
            {
                var message = JsonSerializer.Deserialize<ArticlePublishedMessage>(eventArgs.Body.Span) ?? throw new InvalidOperationException("Invalid article message.");
                var parent = RabbitTraceContext.Extract(eventArgs.BasicProperties.Headers);
                activity = parent == default
                    ? ActivitySource.StartActivity($"consume {queueName}", ActivityKind.Consumer)
                    : ActivitySource.StartActivity($"consume {queueName}", ActivityKind.Consumer, parent);
                await handler(message, stoppingToken);
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception exception)
            {
                activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                logger.LogError(exception, "queue_message_failed queue={Queue} delivery_tag={DeliveryTag}", queueName, eventArgs.DeliveryTag);
                var retryCount = ArticleQueueRetryPolicy.ReadRetryCount(eventArgs.BasicProperties.Headers);
                if (ArticleQueueRetryPolicy.ShouldRetry(retryCount))
                {
                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.MessageId = eventArgs.BasicProperties.MessageId;
                    properties.Headers = ArticleQueueRetryPolicy.CreateRetryHeaders(eventArgs.BasicProperties.Headers, retryCount, activity);
                    channel.BasicPublish(string.Empty, queueName, false, properties, eventArgs.Body);
                    channel.BasicAck(eventArgs.DeliveryTag, false);
                }
                else channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
            finally
            {
                activity?.Dispose();
            }
            return;
        };
        channel.BasicConsume(queueName, autoAck: false, consumer);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

}
