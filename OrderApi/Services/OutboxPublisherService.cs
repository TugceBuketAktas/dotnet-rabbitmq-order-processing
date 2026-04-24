using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderModels.DTOs;

namespace OrderApi.Services;

public class OutboxPublisherService : BackgroundService
{
    private const string OrderCreatedMessageType = "order.created";

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<OutboxPublisherService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected outbox publisher error");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task PublishPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var producer = scope.ServiceProvider.GetRequiredService<IRabbitMQProducer>();

        var pendingMessages = await dbContext.OutboxMessages
            .Where(message => message.PublishedAt == null)
            .OrderBy(message => message.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            message.AttemptCount++;
            message.LastAttemptAt = DateTime.UtcNow;

            try
            {
                if (!string.Equals(message.MessageType, OrderCreatedMessageType, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Unsupported message type: {message.MessageType}");
                }

                var orderDto = JsonSerializer.Deserialize<OrderDto>(message.Payload);
                if (orderDto == null)
                {
                    throw new InvalidOperationException("Outbox payload could not be deserialized");
                }

                await producer.PublishOrderAsync(orderDto, cancellationToken);

                message.PublishedAt = DateTime.UtcNow;
                message.LastError = null;

                _logger.LogInformation(
                    "Outbox message {OutboxMessageId} published for order {OrderId}",
                    message.OutboxMessageId,
                    orderDto.OrderId);
            }
            catch (Exception ex)
            {
                message.LastError = ex.Message;
                _logger.LogWarning(
                    ex,
                    "Outbox message {OutboxMessageId} publish attempt {AttemptCount} failed",
                    message.OutboxMessageId,
                    message.AttemptCount);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
