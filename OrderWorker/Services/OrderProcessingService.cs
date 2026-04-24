using OrderModels.DTOs;
using OrderModels.Enums;
using OrderModels.Models;

namespace OrderWorker.Services;

public interface IOrderProcessingService
{
    Task ProcessOrderAsync(OrderDto orderDto, CancellationToken cancellationToken = default);
}

public class OrderProcessingService : IOrderProcessingService
{
    private readonly ILogger<OrderProcessingService> _logger;

    public OrderProcessingService(ILogger<OrderProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task ProcessOrderAsync(OrderDto orderDto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting to process order {OrderId} for customer {CustomerId}", 
                orderDto.OrderId, orderDto.CustomerId);

            // Simulate processing time
            await Task.Delay(1000, cancellationToken);

            // Validate order
            if (orderDto.Items == null || !orderDto.Items.Any())
            {
                _logger.LogWarning("Order {OrderId} has no items", orderDto.OrderId);
                throw new InvalidOperationException("Order has no items");
            }

            // Calculate and validate total
            var calculatedTotal = orderDto.Items.Sum(item => item.Price * item.Quantity);
            if (Math.Abs(calculatedTotal - orderDto.TotalAmount) > 0.01m)
            {
                _logger.LogWarning("Order {OrderId} total mismatch. Expected: {Expected}, Got: {Got}",
                    orderDto.OrderId, orderDto.TotalAmount, calculatedTotal);
                throw new InvalidOperationException("Order total mismatch");
            }

            // Business logic: check inventory, process payment, etc.
            _logger.LogInformation("Validating inventory for order {OrderId}", orderDto.OrderId);
            ValidateInventory(orderDto);

            _logger.LogInformation("Processing payment for order {OrderId}", orderDto.OrderId);
            ProcessPayment(orderDto);

            _logger.LogInformation("Order {OrderId} processed successfully", orderDto.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order {OrderId}", orderDto.OrderId);
            throw;
        }
    }

    private void ValidateInventory(OrderDto orderDto)
    {
        // Simulate inventory check
        foreach (var item in orderDto.Items)
        {
            _logger.LogInformation("Checking inventory for product {ProductId}, quantity {Quantity}",
                item.ProductId, item.Quantity);
            
            // In a real system, this would check against a database
            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException($"Invalid quantity for product {item.ProductId}");
            }
        }
    }

    private void ProcessPayment(OrderDto orderDto)
    {
        // Simulate payment processing
        _logger.LogInformation("Processing payment of {Amount} for order {OrderId}",
            orderDto.TotalAmount, orderDto.OrderId);

        // In a real system, this would integrate with a payment gateway
        if (orderDto.TotalAmount < 0)
        {
            throw new InvalidOperationException("Invalid order amount");
        }
    }
}
