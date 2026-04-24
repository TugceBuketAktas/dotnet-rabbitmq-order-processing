using RabbitMQ.Client;
using OrderWorker.Consumers;
using OrderWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

// RabbitMQ Connection
var factory = new ConnectionFactory
{
    HostName = builder.Configuration["RabbitMQ:HostName"] ?? "localhost",
    Port = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672"),
    UserName = builder.Configuration["RabbitMQ:UserName"] ?? "guest",
    Password = builder.Configuration["RabbitMQ:Password"] ?? "guest",
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
    RequestedConnectionTimeout = TimeSpan.FromSeconds(10)
};

var connection = await CreateConnectionWithRetryAsync(factory);
builder.Services.AddSingleton(connection);

// Services
builder.Services.AddScoped<IOrderProcessingService, OrderProcessingService>();
builder.Services.AddHttpClient<IOrderStatusReporter, OrderStatusReporter>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["OrderApi:BaseUrl"] ?? "http://localhost:5000/");
});
builder.Services.AddHostedService<RabbitMQConsumer>();

var host = builder.Build();
host.Run();

static async Task<IConnection> CreateConnectionWithRetryAsync(ConnectionFactory factory)
{
    const int maxAttempts = 10;
    var delay = TimeSpan.FromSeconds(3);
    Exception? lastException = null;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            return await factory.CreateConnectionAsync();
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            lastException = ex;
            Console.WriteLine(
                $"RabbitMQ connection attempt {attempt}/{maxAttempts} failed in worker. Retrying in {delay.TotalSeconds:0} seconds.");
            await Task.Delay(delay);
        }
        catch (Exception ex)
        {
            lastException = ex;
            break;
        }
    }

    throw lastException ?? new InvalidOperationException("RabbitMQ connection could not be established");
}
