using System.Text;
using System.Text.Json;
using F360.TaskProcessor.Application.DTOs;
using F360.TaskProcessor.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace F360.TaskProcessor.Infrastructure.Messaging;

/// <summary>
/// Publicador de mensagens no RabbitMQ utilizando o driver oficial RabbitMQ.Client.
/// Garante que a fila seja durável (durable: true) e que as mensagens sejam persistidas em disco (Persistent = true).
/// </summary>
public class RabbitMqPublisher : IMessageQueuePublisher, IAsyncDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqPublisher(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task PublishAsync(TaskQueueMessage message, CancellationToken cancellationToken = default)
    {
        var connection = await GetOrCreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var jsonPayload = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(jsonPayload);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = message.TaskId.ToString()
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _settings.QueueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Mensagem publicada na fila '{QueueName}' do RabbitMQ para a Tarefa {TaskId}.",
            _settings.QueueName, message.TaskId);
    }

    private async Task<IConnection> GetOrCreateConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
        _connectionLock.Dispose();
    }
}
