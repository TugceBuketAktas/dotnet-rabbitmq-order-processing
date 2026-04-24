using System.Text.Json;
using OrderApi.Data;
using OrderModels.DTOs;
using OrderModels.Enums;
using OrderModels.Models;

namespace OrderApi.Services;

public interface IOrderSubmissionService
{
    Task<OrderDto> SubmitAsync(CreateOrderDto createOrderDto, CancellationToken cancellationToken = default);
}

public class OrderSubmissionService : IOrderSubmissionService
{
    private const string OrderCreatedMessageType = "order.created";

    private readonly OrderDbContext _dbContext;
    private readonly IOrderNumberGenerator _orderNumberGenerator;
    private readonly ILogger<OrderSubmissionService> _logger;

    public OrderSubmissionService(
        OrderDbContext dbContext,
        IOrderNumberGenerator orderNumberGenerator,
        ILogger<OrderSubmissionService> logger)
    {
        _dbContext = dbContext;
        _orderNumberGenerator = orderNumberGenerator;
        _logger = logger;
    }

    public async Task<OrderDto> SubmitAsync(CreateOrderDto createOrderDto, CancellationToken cancellationToken = default)
    {
        var orderId = _orderNumberGenerator.Next();
        var items = createOrderDto.Items.Select(item => new OrderItem
        {
            OrderId = orderId,
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            Price = item.Price
        }).ToList();

        var order = new Order
        {
            OrderId = orderId,
            CustomerId = createOrderDto.CustomerId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = items,
            TotalAmount = items.Sum(item => item.Price * item.Quantity)
        };

        var orderDto = order.ToDto();
        var outboxMessage = new OutboxMessage
        {
            MessageType = OrderCreatedMessageType,
            Payload = JsonSerializer.Serialize(orderDto)
        };

        _dbContext.Orders.Add(order);
        _dbContext.OutboxMessages.Add(outboxMessage);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order {OrderId} stored and added to outbox message {OutboxMessageId}",
            order.OrderId,
            outboxMessage.OutboxMessageId);

        return orderDto;
    }
}
