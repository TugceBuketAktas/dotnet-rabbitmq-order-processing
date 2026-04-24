using System.Net.Http.Json;
using OrderModels.Enums;

namespace OrderWorker.Services;

public interface IOrderStatusReporter
{
    Task ReportStatusAsync(int orderId, OrderStatus status, CancellationToken cancellationToken = default);
}

public class OrderStatusReporter : IOrderStatusReporter
{
    private const int MaxAttempts = 5;

    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderStatusReporter> _logger;

    public OrderStatusReporter(HttpClient httpClient, ILogger<OrderStatusReporter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task ReportStatusAsync(int orderId, OrderStatus status, CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var response = await _httpClient.PatchAsJsonAsync(
                    $"api/orders/{orderId}/status",
                    new { status },
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                lastException = new HttpRequestException(
                    $"Status update failed for order {orderId}. HTTP {(int)response.StatusCode}. {responseBody}");

                _logger.LogWarning(
                    "Status update attempt {Attempt}/{MaxAttempts} failed for order {OrderId}. HTTP {StatusCode}",
                    attempt,
                    MaxAttempts,
                    orderId,
                    (int)response.StatusCode);
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                lastException = ex;
                _logger.LogWarning(
                    ex,
                    "Status update attempt {Attempt}/{MaxAttempts} failed for order {OrderId}",
                    attempt,
                    MaxAttempts,
                    orderId);
            }

            if (attempt < MaxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException($"Failed to report status for order {orderId}");
    }
}
