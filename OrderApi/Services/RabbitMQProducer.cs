using System.Text.Json;
using RabbitMQ.Client;
using OrderModels.DTOs;

namespace OrderApi.Services;

public interface IRabbitMQProducer
{
    Task PublishOrderAsync(OrderDto order, CancellationToken cancellationToken = default);
}

public class RabbitMQProducer : IRabbitMQProducer, IAsyncDisposable
{
    private readonly ConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMQProducer> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    public RabbitMQProducer(ConnectionFactory connectionFactory, ILogger<RabbitMQProducer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task PublishOrderAsync(OrderDto order, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await GetConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            const string exchangeName = "orders_exchange";
            const string queueName = "orders_queue";
            const string routingKey = "order.process";

            await channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null);

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await channel.QueueBindAsync(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey,
                arguments: null);

            var json = JsonSerializer.Serialize(order);
            var body = System.Text.Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            await channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Order {OrderId} published to queue", order.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing order {OrderId}", order.OrderId);
            throw;
        }
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection != null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            _connection = await CreateConnectionWithRetryAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task<IConnection> CreateConnectionWithRetryAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 6;
        var delay = TimeSpan.FromSeconds(2);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await _connectionFactory.CreateConnectionAsync(cancellationToken);
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                lastException = ex;
                _logger.LogWarning(
                    ex,
                    "RabbitMQ connection attempt {Attempt}/{MaxAttempts} failed in producer",
                    attempt,
                    maxAttempts);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                break;
            }
        }

        throw lastException ?? new InvalidOperationException("RabbitMQ connection could not be established");
    }

    public async ValueTask DisposeAsync()
    {
        _connectionLock.Dispose();

        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }
}
