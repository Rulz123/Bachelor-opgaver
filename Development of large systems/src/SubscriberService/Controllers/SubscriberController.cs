using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using SubscriberService.Data;

namespace SubscriberService.Controllers;

[ApiController]
[Route("subscriber")]
public class SubscriberController : ControllerBase
{
    private readonly SubscriberDbContext _dbContext;
    private readonly IConnection _connection;
    private readonly JsonSerializerOptions _jsonOptions;

    public SubscriberController(
        SubscriberDbContext dbContext,
        IConnection connection,
        JsonSerializerOptions jsonOptions)
    {
        _dbContext = dbContext;
        _connection = connection;
        _jsonOptions = jsonOptions;
    }

    [HttpPost]
    public async Task<IActionResult> SubscribeAsync(
        Subscriber subscriber,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subscriber.Email))
        {
            return BadRequest("Email is required.");
        }

        _dbContext.Subscribers.Add(new Subscriber
        {
            Id = Guid.NewGuid(),
            Email = subscriber.Email,
            Name = subscriber.Name
        });
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(
           subscriber, _jsonOptions);

        var channelOptions = new CreateChannelOptions(
         publisherConfirmationsEnabled: true,
         publisherConfirmationTrackingEnabled: true);

        await using var channel = await _connection.CreateChannelAsync(channelOptions);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            Persistent = true,
            MessageId = subscriber.Id.ToString(),
            Headers = new Dictionary<string, object?>()
        };

        var currentActivity = Activity.Current;

        if (currentActivity?.Id is string traceParent)
        {
            properties.Headers["traceparent"] =
                Encoding.UTF8.GetBytes(traceParent);
        }

        if (currentActivity?.TraceStateString is string traceState)
        {
            properties.Headers["tracestate"] =
                Encoding.UTF8.GetBytes(traceState);
        }

        await channel.BasicPublishAsync(
            exchange: "subscriber.published",
            routingKey: string.Empty,
            mandatory: true,
            basicProperties: properties,
            body: body);
        return Ok(new
        {
            email = subscriber.Email,
            subscribed = true
        });
    }

    [HttpDelete]
    public async Task<IActionResult> UnsubscribeAsync(
        Subscriber subscriber,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subscriber.Email))
        {
            return BadRequest("Email is required to unsubscribe.");
        }

        _dbContext.Subscribers.Remove(subscriber);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            email = subscriber.Email,
            unsubscribed = true
        });
    }

    [HttpGet("newsletterSubscriberGetter")]
    public async Task<IActionResult> NewsletterSubscriberGetter(Subscriber subscriber,
        CancellationToken cancellationToken)
    {
        var requestedSubscriber = await _dbContext.Subscribers.FirstOrDefaultAsync(s => s.Email.Equals(subscriber.Email) 
        && s.Id.Equals(subscriber.Id), cancellationToken);
        return requestedSubscriber != null ? Ok(requestedSubscriber) : NotFound();       
    }
}