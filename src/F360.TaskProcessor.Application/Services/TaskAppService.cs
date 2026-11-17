using F360.TaskProcessor.Application.DTOs;
using F360.TaskProcessor.Application.Interfaces;
using F360.TaskProcessor.Domain.Entities;
using F360.TaskProcessor.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace F360.TaskProcessor.Application.Services;

/// <summary>
/// Caso de uso responsável por receber novas tarefas da API, persistir no banco NoSQL
/// com status inicial 'Pendente' e enfileirar no RabbitMQ para processamento assíncrono.
/// </summary>
public class TaskAppService : ITaskAppService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IMessageQueuePublisher _queuePublisher;
    private readonly ILogger<TaskAppService> _logger;

    public TaskAppService(
        ITaskRepository taskRepository,
        IMessageQueuePublisher queuePublisher,
        ILogger<TaskAppService> logger)
    {
        _taskRepository = taskRepository;
        _queuePublisher = queuePublisher;
        _logger = logger;
    }

    public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var rawJson = request.Dados.GetRawText();

        var jobTask = new JobTask(
            type: request.Tipo,
            dataJson: rawJson,
            maxAttempts: request.MaxTentativas);

        // 1. Persiste a tarefa no MongoDB com status "Pendente" (Requisito 1.c e 3.a)
        await _taskRepository.CreateAsync(jobTask, cancellationToken);

        // 2. Publica mensagem na fila do RabbitMQ para processamento assíncrono pelos Workers (Requisito 1.c e 4.a)
        var queueMessage = new TaskQueueMessage(jobTask.Id, jobTask.Type, DateTime.UtcNow);
        await _queuePublisher.PublishAsync(queueMessage, cancellationToken);

        _logger.LogInformation(
            "Tarefa {TaskId} do tipo '{TaskType}' criada com status '{Status}' e enviada para a fila.",
            jobTask.Id, jobTask.Type, jobTask.Status);

        return TaskResponse.FromEntity(jobTask);
    }

    public async Task<TaskResponse?> GetTaskByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken);
        return task is null ? null : TaskResponse.FromEntity(task);
    }

    public async Task<IReadOnlyList<TaskResponse>> GetAllTasksAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        return tasks.Select(TaskResponse.FromEntity).ToList();
    }
}
