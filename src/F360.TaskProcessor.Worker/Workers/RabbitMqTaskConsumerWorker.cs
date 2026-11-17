using System.Text;
using System.Text.Json;
using F360.TaskProcessor.Application.DTOs;
using F360.TaskProcessor.Application.Services;
using F360.TaskProcessor.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace F360.TaskProcessor.Worker.Workers;

/// <summary>
/// Worker em segundo plano responsável por consumir a fila de tarefas do RabbitMQ (Requisito 2).
/// Utiliza PrefetchCount = 1 (Competing Consumers) e confirmação manual (BasicAck)
/// em conjunto com o controle de concorrência atômico no MongoDB.
/// </summary>
public class RabbitMqTaskConsumerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqTaskConsumerWorker> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqTaskConsumerWorker(
        IServiceProvider serviceProvider,
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqTaskConsumerWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando Worker de processamento de tarefas F360...");

        await InitializeRabbitMqWithRetryAsync(stoppingToken);

        if (_channel is null)
            return;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            await HandleMessageAsync(eventArgs, stoppingToken);
        };

        await _channel.BasicConsumeAsync(
            queue: _settings.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "Worker aguardando tarefas na fila '{QueueName}' (PrefetchCount = 1)...",
            _settings.QueueName);

        // Mantém o BackgroundService ativo até o encerramento da aplicação
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task InitializeRabbitMqWithRetryAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await _channel.QueueDeclareAsync(
                    queue: _settings.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                // Controle de Concorrência na Fila (Competing Consumers):
                // Garante que cada worker receba apenas 1 mensagem por vez antes de enviar o Ack.
                await _channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: 1,
                    global: false,
                    cancellationToken: stoppingToken);

                _logger.LogInformation(
                    "Conexão com RabbitMQ estabelecida com sucesso ({Host}:{Port}).",
                    _settings.HostName, _settings.Port);
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Aguardando disponibilidade do RabbitMQ em {Host}:{Port}... ({Message}) Nova tentativa em 3s.",
                    _settings.HostName, _settings.Port, ex.Message);

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs eventArgs, CancellationToken stoppingToken)
    {
        if (_channel is null)
            return;

        try
        {
            var bodyJson = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            var queueMessage = JsonSerializer.Deserialize<TaskQueueMessage>(bodyJson);

            if (queueMessage is null || queueMessage.TaskId == Guid.Empty)
            {
                _logger.LogWarning("Mensagem inválida recebida na fila: {Payload}. Descartando.", bodyJson);
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<ITaskProcessingOrchestrator>();

            await orchestrator.ProcessTaskAsync(queueMessage.TaskId, stoppingToken);

            // Confirma o processamento da entrega atual na fila (se falhou e ainda tinha tentativas,
            // o próprio Orchestrator já atualizou o status no banco e publicou a nova tentativa).
            await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar mensagem da fila RabbitMQ.");
            if (_channel.IsOpen)
            {
                // Em caso de falha crítica de infraestrutura (ex.: queda temporária do banco),
                // devolve a mensagem para a fila (requeue: true).
                await _channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Encerrando Worker de processamento de tarefas F360...");

        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
            _connection.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}
