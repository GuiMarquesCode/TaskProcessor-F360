using F360.TaskProcessor.Domain.Entities;

namespace F360.TaskProcessor.Domain.Interfaces;

/// <summary>
/// Contrato de persistência para tarefas (Repository Pattern + Dependency Inversion Principle).
/// </summary>
public interface ITaskRepository
{
    /// <summary>
    /// Armazena uma nova tarefa no banco de dados NoSQL.
    /// </summary>
    Task CreateAsync(JobTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta uma tarefa pelo seu identificador único.
    /// </summary>
    Task<JobTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista todas as tarefas armazenadas, ordenadas da mais recente para a mais antiga.
    /// </summary>
    Task<IReadOnlyList<JobTask>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Controle de Concorrência (Requisito 2.c):
    /// Tenta adquirir atomicamente uma tarefa que esteja com status 'Pendente',
    /// alterando-a para 'EmProcessamento' e incrementando o contador de tentativas no banco.
    /// Se outro worker já tiver capturado a tarefa simultaneamente, retorna null.
    /// </summary>
    Task<JobTask?> TryAcquireForProcessingAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza o estado atual da tarefa no banco de dados.
    /// </summary>
    Task UpdateAsync(JobTask task, CancellationToken cancellationToken = default);
}
