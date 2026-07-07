using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Messaging;

namespace SmartCoffeeBuilder.API.Services;

public class AiDesignResultConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageBusService _messageBus;
    private readonly ILogger<AiDesignResultConsumer> _logger;

    public AiDesignResultConsumer(
        IServiceProvider serviceProvider,
        IMessageBusService messageBus,
        ILogger<AiDesignResultConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _messageBus = messageBus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Design Result Consumer started");

        await _messageBus.SubscribeAsync<AiDesignResultMessage>(
            QueueKeys.AiDesignResult,
            async (message) =>
            {
                _logger.LogInformation(
                    "Received AI design result for recommendation {RecommendationId}, job {JobId}",
                    message.RecommendationId,
                    message.JobId);

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IAiRecommendationService>();
                    await service.ProcessAiDesignResultAsync(message);

                    _logger.LogInformation(
                        "Successfully processed AI design result for recommendation {RecommendationId}",
                        message.RecommendationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to process AI design result for recommendation {RecommendationId}",
                        message.RecommendationId);
                    throw;
                }
            },
            stoppingToken);
    }
}