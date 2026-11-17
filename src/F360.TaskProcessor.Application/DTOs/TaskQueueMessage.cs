namespace F360.TaskProcessor.Application.DTOs;

/// <summary>
/// Mensagem leve trafegada na fila do RabbitMQ contendo a referência da tarefa.
/// </summary>
public record TaskQueueMessage(Guid TaskId, string TaskType, DateTime EnqueuedAtUtc);
