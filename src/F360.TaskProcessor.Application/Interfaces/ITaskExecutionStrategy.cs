using F360.TaskProcessor.Domain.Entities;

namespace F360.TaskProcessor.Application.Interfaces;

/// <summary>
/// Contrato do Strategy Pattern (Abstração e Polimorfismo).
/// Cada tipo de tarefa ("EnviarEmail", "GerarRelatorio", etc.) implementa esta interface.
/// </summary>
public interface ITaskExecutionStrategy
{
    /// <summary>
    /// Nome identificador do tipo da tarefa (ex.: "EnviarEmail", "GerarRelatorio").
    /// </summary>
    string TaskType { get; }

    /// <summary>
    /// Executa a lógica específica da tarefa em segundo plano.
    /// </summary>
    Task ExecuteAsync(JobTask task, CancellationToken cancellationToken = default);
}
