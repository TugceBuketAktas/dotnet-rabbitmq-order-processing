using System.Text.Json;
using RabbitMQ.Client;
using OrderModels.DTOs;
using OrderModels.Enums;
using OrderWorker.Services;

namespace OrderWorker.Consumers;

public class RabbitMQConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQConsumer> _logger;
    private IChannel? _channel;

    public RabbitMQConsumer(
        IConnection connection,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQConsumer> logger)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _channel = await _connection.CreateChannelAsync();

            const string exchangeName = "orders_exchange";
            const string queueName = "orders_queue";
            const string routingKey = "order.process";

            await _channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null);

            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await _channel.QueueBindAsync(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey,
                arguments: null);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ consumer started, listening to queue: {QueueName}", queueName);

            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await _channel.BasicGetAsync(queue: queueName, autoAck: false);

                if (result is not null)
                {
                    await HandleMessage(result, stoppingToken);
                }
                else
                {
                    await Task.Delay(100, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RabbitMQ consumer cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ Consumer error");
        }
    }

    private async Task HandleMessage(BasicGetResult result, CancellationToken cancellationToken)
    {
        OrderDto? orderDto = null;

        try
        {
            var message = System.Text.Encoding.UTF8.GetString(result.Body.ToArray());
            orderDto = JsonSerializer.Deserialize<OrderDto>(message);

            if (orderDto == null)
            {
                _logger.LogWarning("Failed to deserialize message");
                await _channel!.BasicNackAsync(deliveryTag: result.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            _logger.LogInformation("Processing order message: {OrderId}", orderDto.OrderId);

            using var scope = _serviceProvider.CreateScope();
            var processingService = scope.ServiceProvider.GetRequiredService<IOrderProcessingService>();
            var statusReporter = scope.ServiceProvider.GetRequiredService<IOrderStatusReporter>();

            await statusReporter.ReportStatusAsync(orderDto.OrderId, OrderStatus.Processing, cancellationToken);

            try
            {
                await processingService.ProcessOrderAsync(orderDto, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Order {OrderId} failed business validation", orderDto.OrderId);
                await statusReporter.ReportStatusAsync(orderDto.OrderId, OrderStatus.Failed, cancellationToken);
                await _channel!.BasicAckAsync(deliveryTag: result.DeliveryTag, multiple: false);
                return;
            }

            await statusReporter.ReportStatusAsync(orderDto.OrderId, OrderStatus.Completed, cancellationToken);

            await _channel!.BasicAckAsync(deliveryTag: result.DeliveryTag, multiple: false);
            _logger.LogInformation("Order {OrderId} processed and acknowledged", orderDto.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message");
            if (_channel != null)
            {
                await _channel.BasicNackAsync(deliveryTag: result.DeliveryTag, multiple: false, requeue: true);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
