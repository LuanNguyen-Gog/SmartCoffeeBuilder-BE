using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SmartCoffeeBuilder.Service.Messaging;

/// <summary>
/// Internal messaging contract. Hides the underlying broker (RabbitMQ) so
/// domain services can publish/subscribe without coupling to AMQP types.
/// </summary>
public interface IMessageBusService
{
    /// <summary>
    /// Publish a JSON-serialized message to the configured request queue.
    /// </summary>
    Task PublishAsync<T>(string queueKey, T message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a long-running consumer for <paramref name="queueKey"/>. The handler
    /// runs once per delivered message. Messages that throw are nacked back to
    /// the queue so they can be re-delivered.
    /// </summary>
    Task SubscribeAsync<T>(
        string queueKey,
        Func<T, Task> handler,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// RabbitMQ-backed message bus used by the AI design pipeline.
///
/// Replaces the previous Google Pub/Sub wrapper. Wire format is identical
/// (UTF-8 JSON body) so the Python services can consume the same payloads.
///
/// Two queues are pre-declared at startup:
///   - <see cref="RabbitMqOptions.RequestQueue"/>  : BE -> AI design request
///   - <see cref="RabbitMqOptions.ResultQueue"/>   : AI -> BE design result
///
/// Both queues are bound to a single direct exchange
/// (<see cref="RabbitMqOptions.RequestExchange"/>) so producers and consumers
/// stay decoupled from queue names.
/// </summary>
public class RabbitMqService : IMessageBusService, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqService> _logger;
    private readonly IConnection _connection;
    private readonly IModel _publisherChannel;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
    {
        _logger = logger;
        _options = RabbitMqOptions.FromConfiguration(configuration);

        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            ClientProvidedName = "smartcafe-be",
        };

        _connection = factory.CreateConnection();
        _publisherChannel = _connection.CreateModel();

        // Declare topology once at startup. Idempotent on RabbitMQ side.
        DeclareTopology(_publisherChannel);

        _logger.LogInformation(
            "RabbitMQ connected host={Host}:{Port} vhost={VHost} exchange={Exchange}",
            _options.Host, _options.Port, _options.VirtualHost, _options.RequestExchange);
    }

    private void DeclareTopology(IModel channel)
    {
        channel.ExchangeDeclare(
            exchange: _options.RequestExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        // Request queue — BE -> AI (publish).
        channel.QueueDeclare(
            queue: _options.RequestQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);
        channel.QueueBind(
            queue: _options.RequestQueue,
            exchange: _options.RequestExchange,
            routingKey: _options.RequestRoutingKey);

        // Result queue — AI -> BE (consume).
        channel.QueueDeclare(
            queue: _options.ResultQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);
        channel.QueueBind(
            queue: _options.ResultQueue,
            exchange: _options.RequestExchange,
            routingKey: _options.ResultRoutingKey);
    }

    private string ResolveRoutingKey(string queueKey) => queueKey switch
    {
        QueueKeys.AiDesignRequest => _options.RequestRoutingKey,
        QueueKeys.AiDesignResult => _options.ResultRoutingKey,
        _ => queueKey,
    };

    private string ResolveQueueName(string queueKey) => queueKey switch
    {
        QueueKeys.AiDesignRequest => _options.RequestQueue,
        QueueKeys.AiDesignResult => _options.ResultQueue,
        _ => queueKey,
    };

    public Task PublishAsync<T>(string queueKey, T message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var props = _publisherChannel.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2; // persistent
        props.MessageId = Guid.NewGuid().ToString();
        props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _publisherChannel.BasicPublish(
            exchange: _options.RequestExchange,
            routingKey: ResolveRoutingKey(queueKey),
            mandatory: false,
            basicProperties: props,
            body: body);

        _logger.LogInformation(
            "Published to {Queue} (bytes={Bytes}, messageId={MessageId})",
            ResolveQueueName(queueKey), json.Length, props.MessageId);

        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T>(
        string queueKey,
        Func<T, Task> handler,
        CancellationToken cancellationToken = default)
    {
        var queue = ResolveQueueName(queueKey);
        var channel = _connection.CreateModel();
        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            _logger.LogInformation("Received from {Queue}: {Json}", queue, json);

            try
            {
                var payload = JsonSerializer.Deserialize<T>(json, JsonOptions);
                if (payload is null)
                {
                    _logger.LogWarning("Skipping null payload from {Queue}", queue);
                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                await handler(payload);
                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {Queue}; nack with requeue", queue);
                // requeue=true so transient errors are retried on the next poll.
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        var consumerTag = channel.BasicConsume(
            queue: queue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Subscribed to {Queue} (consumerTag={Tag})", queue, consumerTag);

        // Keep the channel alive until shutdown.
        cancellationToken.Register(() =>
        {
            try
            {
                channel.Close();
                channel.Dispose();
                _logger.LogInformation("Consumer for {Queue} stopped", queue);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while closing consumer channel for {Queue}", queue);
            }
        });

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try { _publisherChannel?.Close(); _publisherChannel?.Dispose(); }
        catch (Exception ex) { _logger.LogWarning(ex, "Error closing publisher channel"); }
        try { _connection?.Close(); _connection?.Dispose(); }
        catch (Exception ex) { _logger.LogWarning(ex, "Error closing connection"); }
        GC.SuppressFinalize(this);
    }
}

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";
    public string RequestExchange { get; init; } = "smartcafe.ai-design";
    public string RequestQueue { get; init; } = "ai-design-request";
    public string RequestRoutingKey { get; init; } = "ai-design.request";
    public string ResultQueue { get; init; } = "ai-design-result";
    public string ResultRoutingKey { get; init; } = "ai-design.result";

    public static RabbitMqOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("RabbitMq");
        return new RabbitMqOptions
        {
            Host = section["Host"] ?? "localhost",
            Port = int.TryParse(section["Port"], out var p) ? p : 5672,
            Username = section["Username"] ?? "guest",
            Password = section["Password"] ?? "guest",
            VirtualHost = section["VirtualHost"] ?? "/",
            RequestExchange = section["RequestExchange"] ?? "smartcafe.ai-design",
            RequestQueue = section["RequestQueue"] ?? "ai-design-request",
            RequestRoutingKey = section["RequestRoutingKey"] ?? "ai-design.request",
            ResultQueue = section["ResultQueue"] ?? "ai-design-result",
            ResultRoutingKey = section["ResultRoutingKey"] ?? "ai-design.result",
        };
    }
}

/// <summary>
/// Logical queue keys used by domain code. Resolved to AMQP names by
/// <see cref="RabbitMqService"/>.
/// </summary>
public static class QueueKeys
{
    public const string AiDesignRequest = "ai-design-request";
    public const string AiDesignResult = "ai-design-result";
}