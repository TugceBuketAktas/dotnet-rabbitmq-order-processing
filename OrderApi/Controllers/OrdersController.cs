using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using OrderApi.Repositories;
using OrderApi.Services;
using OrderModels.DTOs;
using OrderModels.Enums;

namespace OrderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderRepository repository,
        ILogger<OrdersController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(
        CreateOrderDto createOrderDto,
        [FromServices] IOrderSubmissionService orderSubmissionService,
        CancellationToken cancellationToken)
    {
        try
        {
            var orderDto = await orderSubmissionService.SubmitAsync(createOrderDto, cancellationToken);

            _logger.LogInformation("Order {OrderId} accepted and queued for outbox publishing", orderDto.OrderId);

            return CreatedAtAction(nameof(GetOrder), new { id = orderDto.OrderId }, orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _repository.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            return Ok(order.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAllOrders(CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _repository.GetAllOrdersAsync();
            return Ok(orders.Select(order => order.ToDto()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult> UpdateOrderStatus(int id, UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _repository.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = request.Status;
            if (request.Status == OrderStatus.Completed || request.Status == OrderStatus.Failed)
            {
                order.ProcessedAt = DateTime.UtcNow;
            }
            else
            {
                order.ProcessedAt = null;
            }

            await _repository.UpdateOrderAsync(order);

            _logger.LogInformation("Order {OrderId} status updated to {Status}", id, request.Status);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId} status", id);
            return StatusCode(500, "Internal server error");
        }
    }

    public class UpdateOrderStatusRequest
    {
        [Required]
        public OrderStatus Status { get; set; }
    }
}
